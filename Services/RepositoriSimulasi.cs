using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using SunCost.Models;

namespace SunCost.Services;

/// <summary>Menyimpan skenario simulasi pada satu berkas SQLite di folder data aplikasi pengguna.</summary>
public class RepositoriSimulasi
{
    // Skema penuh sesuai docs/ERD.md. Urutan: tabel yang dirujuk FK dibuat lebih dulu.
    // Kolom tambahan pada skenario sengaja boleh NULL (berbeda dari DDL di ERD): SQLite tidak bisa
    // menambah kolom NOT NULL ke tabel yang sudah berisi, dan baris lama tidak punya nilainya.
    // BE-2 yang mengisi kolom-kolom itu.
    private const string Skema = """
        CREATE TABLE IF NOT EXISTS lokasi (
            id          TEXT PRIMARY KEY,
            nama        TEXT NOT NULL,
            latitude    REAL NOT NULL CHECK (latitude BETWEEN -90 AND 90),
            longitude   REAL NOT NULL CHECK (longitude BETWEEN -180 AND 180),
            luas_atap   REAL NOT NULL CHECK (luas_atap > 0),
            orientasi   REAL NOT NULL CHECK (orientasi >= 0 AND orientasi < 360),
            kemiringan  REAL NOT NULL CHECK (kemiringan BETWEEN 0 AND 90)
        );

        CREATE TABLE IF NOT EXISTS panel_surya (
            id              TEXT PRIMARY KEY,
            merk            TEXT NOT NULL,
            kapasitas_wp    REAL NOT NULL CHECK (kapasitas_wp > 0),
            efisiensi       REAL NOT NULL,
            koefisien_suhu  REAL NOT NULL,
            luas_per_panel  REAL NOT NULL CHECK (luas_per_panel > 0),
            harga_per_unit  REAL NOT NULL,
            jenis           TEXT
        );

        CREATE TABLE IF NOT EXISTS tarif_listrik_pln (
            id              TEXT PRIMARY KEY,
            golongan        TEXT NOT NULL,
            tarif_per_kwh   REAL NOT NULL CHECK (tarif_per_kwh > 0),
            tanggal_berlaku TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS data_radiasi (
            id                     TEXT PRIMARY KEY,
            lokasi_id              TEXT NOT NULL REFERENCES lokasi(id) ON DELETE CASCADE,
            peak_sun_hours_tahunan REAL NOT NULL,
            suhu_rata_rata         REAL NOT NULL,
            sumber_data            TEXT NOT NULL,
            tanggal_ambil          TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS radiasi_bulanan (
            data_radiasi_id TEXT NOT NULL REFERENCES data_radiasi(id) ON DELETE CASCADE,
            bulan           INTEGER NOT NULL CHECK (bulan BETWEEN 1 AND 12),
            peak_sun_hours  REAL NOT NULL,
            PRIMARY KEY (data_radiasi_id, bulan)
        );

        CREATE TABLE IF NOT EXISTS skenario (
            id                  TEXT PRIMARY KEY,
            nama                TEXT NOT NULL,
            tanggal             TEXT NOT NULL,
            lokasi_id           TEXT REFERENCES lokasi(id),
            panel_id            TEXT REFERENCES panel_surya(id),
            tarif_id            TEXT REFERENCES tarif_listrik_pln(id),
            data_radiasi_id     TEXT REFERENCES data_radiasi(id),
            biaya_per_wp        REAL,
            degradasi_tahunan   REAL,
            faktor_emisi_co2    REAL,
            jumlah_panel        INTEGER,
            kapasitas_wp        REAL,
            produksi_tahunan    REAL NOT NULL,
            penghematan_tahunan REAL NOT NULL,
            payback_period      REAL NOT NULL,
            emisi_dihindari     REAL NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_skenario_tanggal ON skenario(tanggal DESC);
        """;

    // Kolom yang tidak ada pada database versi lama. Nama dan tipe ditulis tetap di sini
    // (bukan input pengguna), jadi aman dipakai langsung dalam ALTER TABLE.
    private static readonly (string Nama, string Tipe)[] KolomTambahanSkenario =
    {
        ("lokasi_id", "TEXT REFERENCES lokasi(id)"),
        ("panel_id", "TEXT REFERENCES panel_surya(id)"),
        ("tarif_id", "TEXT REFERENCES tarif_listrik_pln(id)"),
        ("data_radiasi_id", "TEXT REFERENCES data_radiasi(id)"),
        ("biaya_per_wp", "REAL"),
        ("degradasi_tahunan", "REAL"),
        ("faktor_emisi_co2", "REAL"),
        ("jumlah_panel", "INTEGER"),
        ("kapasitas_wp", "REAL"),
    };

    private string connectionString;

    public RepositoriSimulasi(string? berkasDb = null)
    {
        berkasDb ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SunCost", "suncost.db");
        Directory.CreateDirectory(Path.GetDirectoryName(berkasDb)!);
        connectionString = new SqliteConnectionStringBuilder { DataSource = berkasDb }.ToString();

        using var db = BukaKoneksi();
        using (var cmd = db.CreateCommand())
        {
            cmd.CommandText = Skema;
            cmd.ExecuteNonQuery();
        }
        TambahKolomYangBelumAda(db);
        TambahKolomJenisPanel(db);
        SisipkanDataAwal(db);
    }

    /// <summary>
    /// Membuka koneksi dengan foreign key menyala. SQLite mematikannya secara bawaan dan
    /// pengaturannya berlaku per koneksi, jadi harus dijalankan pada setiap koneksi yang dibuka.
    /// </summary>
    internal SqliteConnection BukaKoneksi()
    {
        var db = new SqliteConnection(connectionString);
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();
        return db;
    }

    /// <summary>Menambah kolom skenario yang belum ada. Aman dijalankan berulang dan tidak menyentuh data lama.</summary>
    private static void TambahKolomYangBelumAda(SqliteConnection db)
    {
        var ada = new HashSet<string>();
        using (var cmd = db.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info(skenario);";
            using var baca = cmd.ExecuteReader();
            while (baca.Read()) ada.Add(baca.GetString(1));
        }

        foreach (var (nama, tipe) in KolomTambahanSkenario)
        {
            if (ada.Contains(nama)) continue;
            using var alter = db.CreateCommand();
            alter.CommandText = $"ALTER TABLE skenario ADD COLUMN {nama} {tipe};";
            alter.ExecuteNonQuery();
        }
    }

        // Data master awal. Id tetap + INSERT OR IGNORE: aman dijalankan setiap aplikasi dibuka dan tidak
    // menimpa baris yang sudah ada. Spesifikasi panel adalah nilai tipikal kelasnya, bukan datasheet
    // satu produk; harga per unit hanya perkiraan.
    private static readonly (string Id, string Merk, double Wp, double Efisiensi, double KoefSuhu,
                             double LuasM2, double Harga, string Jenis)[] PanelAwal =
    {
        ("panel-mono-420", "Monokristalin 420 Wp", 420, 21.5, 0.35, 1.95, 1_800_000, "monokristalin"),
        ("panel-mono-550", "Monokristalin 550 Wp", 550, 21.3, 0.40, 2.60, 2_750_000, "monokristalin"),
        ("panel-thin-110", "Thin film a-Si 110 Wp", 110, 8.0, 0.21, 1.40, 1_100_000, "thin_film"),
    };

    // Tarif non-subsidi triwulan IV-2026.
    private static readonly (string Id, string Golongan, double TarifPerKwh)[] TarifAwal =
    {
        ("tarif-r1-900", "R-1/TR 900 VA (RTM)", 1352.00),
        ("tarif-r1-1300", "R-1/TR 1.300 VA", 1444.70),
        ("tarif-r1-2200", "R-1/TR 2.200 VA", 1444.70),
        ("tarif-r2", "R-2/TR 3.500-5.500 VA", 1699.53),
    };

    private const string TanggalTarifAwal = "2026-10-01";

    /// <summary>Menambah kolom jenis pada panel_surya untuk database yang dibuat sebelum kolom ini ada.</summary>
    private static void TambahKolomJenisPanel(SqliteConnection db)
    {
        using var cek = db.CreateCommand();
        cek.CommandText = "SELECT COUNT(*) FROM pragma_table_info('panel_surya') WHERE name = 'jenis';";
        if (Convert.ToInt64(cek.ExecuteScalar()) > 0) return;

        using var alter = db.CreateCommand();
        alter.CommandText = "ALTER TABLE panel_surya ADD COLUMN jenis TEXT;";
        alter.ExecuteNonQuery();
    }

    private static void SisipkanDataAwal(SqliteConnection db)
    {
        using var tx = db.BeginTransaction();

        foreach (var p in PanelAwal)
        {
            using var cmd = db.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT OR IGNORE INTO panel_surya
                    (id, merk, kapasitas_wp, efisiensi, koefisien_suhu, luas_per_panel, harga_per_unit, jenis)
                VALUES ($id, $merk, $wp, $ef, $koef, $luas, $harga, $jenis);
                """;
            cmd.Parameters.AddWithValue("$id", p.Id);
            cmd.Parameters.AddWithValue("$merk", p.Merk);
            cmd.Parameters.AddWithValue("$wp", p.Wp);
            cmd.Parameters.AddWithValue("$ef", p.Efisiensi);
            cmd.Parameters.AddWithValue("$koef", p.KoefSuhu);
            cmd.Parameters.AddWithValue("$luas", p.LuasM2);
            cmd.Parameters.AddWithValue("$harga", p.Harga);
            cmd.Parameters.AddWithValue("$jenis", p.Jenis);
            cmd.ExecuteNonQuery();
        }

        foreach (var t in TarifAwal)
        {
            using var cmd = db.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT OR IGNORE INTO tarif_listrik_pln (id, golongan, tarif_per_kwh, tanggal_berlaku)
                VALUES ($id, $golongan, $tarif, $tanggal);
                """;
            cmd.Parameters.AddWithValue("$id", t.Id);
            cmd.Parameters.AddWithValue("$golongan", t.Golongan);
            cmd.Parameters.AddWithValue("$tarif", t.TarifPerKwh);
            cmd.Parameters.AddWithValue("$tanggal", TanggalTarifAwal);
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }
    
    public bool simpan(SimulasiSkenario s)
    {
        using var db = BukaKoneksi();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO skenario
                (id, nama, tanggal, produksi_tahunan, penghematan_tahunan, payback_period, emisi_dihindari)
            VALUES ($id, $nama, $tanggal, $produksi, $hemat, $payback, $emisi);
            """;
        cmd.Parameters.AddWithValue("$id", s.Id);
        cmd.Parameters.AddWithValue("$nama", s.Nama);
        cmd.Parameters.AddWithValue("$tanggal", s.Tanggal.ToString("o", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$produksi", s.ProduksiTahunan);
        cmd.Parameters.AddWithValue("$hemat", s.PenghematanTahunan);
        cmd.Parameters.AddWithValue("$payback", s.PaybackPeriod);
        cmd.Parameters.AddWithValue("$emisi", s.EmisiDihindari);
        return cmd.ExecuteNonQuery() == 1;
    }

    public List<SimulasiSkenario> ambilSemua()
    {
        var daftar = new List<SimulasiSkenario>();

        using var db = BukaKoneksi();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            SELECT id, nama, tanggal, produksi_tahunan, penghematan_tahunan, payback_period, emisi_dihindari
            FROM skenario ORDER BY tanggal DESC;
            """;
        using var baca = cmd.ExecuteReader();
        while (baca.Read())
        {
            daftar.Add(new SimulasiSkenario(
                baca.GetString(0),
                baca.GetString(1),
                DateTime.Parse(baca.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                baca.GetDouble(3),
                baca.GetDouble(4),
                baca.GetDouble(5),
                baca.GetDouble(6)));
        }
        return daftar;
    }

    // ponytail: memindai seluruh riwayat; BE-2 menggantinya dengan query WHERE id = $id.
    public SimulasiSkenario? ambilById(string id) => ambilSemua().Find(s => s.Id == id);

    public bool hapus(string id)
    {
        using var db = BukaKoneksi();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM skenario WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteNonQuery() > 0;
    }
}