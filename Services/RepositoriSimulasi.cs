using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using SunCost.Models;

namespace SunCost.Services;

/// <summary>Menyimpan skenario simulasi pada satu berkas SQLite di folder data aplikasi pengguna.</summary>
public class RepositoriSimulasi
{
    // Skema penuh sesuai docs/ERD.md. Urutan: tabel yang dirujuk FK dibuat lebih dulu.
    // Kolom tambahan pada skenario sengaja boleh NULL (berbeda dari DDL di ERD): SQLite tidak bisa
    // menambah kolom NOT NULL ke tabel yang sudah berisi, dan baris lama tidak punya nilainya.
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
            tagihan_bulanan_kwh REAL,
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
        ("tagihan_bulanan_kwh", "REAL"),
    };

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

    /// <summary>Perintah berparameter. Teks SQL selalu konstanta; nilai selalu lewat parameter.</summary>
    private static SqliteCommand Perintah(SqliteConnection db, SqliteTransaction? tx, string sql,
                                          params (string Nama, object? Nilai)[] parameter)
    {
        var cmd = db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        foreach (var (nama, nilai) in parameter)
            cmd.Parameters.AddWithValue(nama, nilai ?? DBNull.Value);
        return cmd;
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
            using var cmd = Perintah(db, tx, """
                INSERT OR IGNORE INTO panel_surya
                    (id, merk, kapasitas_wp, efisiensi, koefisien_suhu, luas_per_panel, harga_per_unit, jenis)
                VALUES ($id, $merk, $wp, $ef, $koef, $luas, $harga, $jenis);
                """,
                ("$id", p.Id), ("$merk", p.Merk), ("$wp", p.Wp), ("$ef", p.Efisiensi),
                ("$koef", p.KoefSuhu), ("$luas", p.LuasM2), ("$harga", p.Harga), ("$jenis", p.Jenis));
            cmd.ExecuteNonQuery();
        }

        foreach (var t in TarifAwal)
        {
            using var cmd = Perintah(db, tx, """
                INSERT OR IGNORE INTO tarif_listrik_pln (id, golongan, tarif_per_kwh, tanggal_berlaku)
                VALUES ($id, $golongan, $tarif, $tanggal);
                """,
                ("$id", t.Id), ("$golongan", t.Golongan), ("$tarif", t.TarifPerKwh), ("$tanggal", TanggalTarifAwal));
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    // ---- Data master -------------------------------------------------------------------------

    // simpan() membuat baris panel/tarif baru ber-id "panel-"/"tarif-" + Guid 32 karakter untuk input
    // yang belum ada (termasuk "Tarif manual"). Baris itu milik skenario, bukan pilihan dropdown.
    private const string BukanBarisSkenario = "length(id) <> length($awalan) + 32";

    /// <summary>Panel untuk dropdown Simulasi Baru: data awal BE-5 dan panel yang ditambahkan manual ke tabel.</summary>
    public List<PanelSurya> ambilSemuaPanel()
    {
        using var db = BukaKoneksi();
        using var cmd = Perintah(db, null, $"""
            SELECT merk, kapasitas_wp, efisiensi, koefisien_suhu, luas_per_panel, harga_per_unit, jenis
            FROM panel_surya WHERE {BukanBarisSkenario}
            ORDER BY COALESCE(jenis, 'monokristalin') = 'thin_film', kapasitas_wp;
            """, ("$awalan", "panel-"));
        using var baca = cmd.ExecuteReader();
        var daftar = new List<PanelSurya>();
        while (baca.Read())
        {
            string merk = baca.GetString(0);
            double wp = baca.GetDouble(1), ef = baca.GetDouble(2), koef = baca.GetDouble(3),
                   luas = baca.GetDouble(4), harga = baca.GetDouble(5);
            daftar.Add(!baca.IsDBNull(6) && baca.GetString(6) == "thin_film"
                ? new PanelThinFilm(merk, wp, ef, koef, luas, harga)
                : new PanelMonokristal(merk, wp, ef, koef, luas, harga));
        }
        return daftar;
    }

    /// <summary>Data awal tanpa membuka database, untuk dropdown kalau database tidak bisa dibaca.</summary>
    public static List<PanelSurya> PanelBawaan() => PanelAwal
        .Select(p => p.Jenis == "thin_film"
            ? (PanelSurya)new PanelThinFilm(p.Merk, p.Wp, p.Efisiensi, p.KoefSuhu, p.LuasM2, p.Harga)
            : new PanelMonokristal(p.Merk, p.Wp, p.Efisiensi, p.KoefSuhu, p.LuasM2, p.Harga))
        .ToList();

    /// <inheritdoc cref="PanelBawaan"/>
    public static List<TarifListrikPLN> TarifBawaan() => TarifAwal
        .Select(t => new TarifListrikPLN(t.Golongan, t.TarifPerKwh, Tanggal(TanggalTarifAwal)))
        .ToList();

    /// <summary>Golongan tarif PLN untuk dropdown Simulasi Baru, urut dari tarif termurah.</summary>
    public List<TarifListrikPLN> ambilSemuaTarif()
    {
        using var db = BukaKoneksi();
        using var cmd = Perintah(db, null, $"""
            SELECT golongan, tarif_per_kwh, tanggal_berlaku
            FROM tarif_listrik_pln WHERE {BukanBarisSkenario}
            ORDER BY tarif_per_kwh, golongan;
            """, ("$awalan", "tarif-"));
        using var baca = cmd.ExecuteReader();
        var daftar = new List<TarifListrikPLN>();
        while (baca.Read())
            daftar.Add(new TarifListrikPLN(baca.GetString(0), baca.GetDouble(1), Tanggal(baca.GetString(2))));
        return daftar;
    }

    // ---- Simpan -------------------------------------------------------------------------------

    /// <summary>
    /// Menyimpan skenario beserta inputnya (lokasi, panel, tarif, data radiasi) dalam satu transaksi.
    /// Menyimpan ulang skenario yang sama mengganti isinya tanpa meninggalkan baris input yatim.
    /// Skenario tanpa input (baris lama) hanya menyimpan hasilnya.
    /// </summary>
    public bool simpan(SimulasiSkenario s)
    {
        using var db = BukaKoneksi();
        using var tx = db.BeginTransaction();

        HapusDi(db, tx, s.Id);

        string? lokasiId = null, panelId = null, tarifId = null, radiasiId = null;
        if (s.Lokasi is { } lokasi && s.Panel is { } panel && s.Tarif is { } tarif && s.DataRadiasi is { } radiasi)
        {
            lokasiId = Guid.NewGuid().ToString("N");
            using (var cmd = Perintah(db, tx, """
                INSERT INTO lokasi (id, nama, latitude, longitude, luas_atap, orientasi, kemiringan)
                VALUES ($id, $nama, $lat, $lon, $luas, $ori, $miring);
                """,
                ("$id", lokasiId), ("$nama", lokasi.Nama), ("$lat", lokasi.Latitude), ("$lon", lokasi.Longitude),
                ("$luas", lokasi.LuasAtap), ("$ori", lokasi.Orientasi), ("$miring", lokasi.Kemiringan)))
                cmd.ExecuteNonQuery();

            panelId = CariAtauBuatPanel(db, tx, panel);
            tarifId = CariAtauBuatTarif(db, tx, tarif);
            radiasiId = SimpanRadiasi(db, tx, lokasiId, radiasi);
        }

        var analisis = s.Analisis;
        int baris;
        using (var cmd = Perintah(db, tx, """
            INSERT INTO skenario
                (id, nama, tanggal, lokasi_id, panel_id, tarif_id, data_radiasi_id,
                 biaya_per_wp, degradasi_tahunan, faktor_emisi_co2, jumlah_panel, kapasitas_wp, tagihan_bulanan_kwh,
                 produksi_tahunan, penghematan_tahunan, payback_period, emisi_dihindari)
            VALUES
                ($id, $nama, $tanggal, $lokasi, $panel, $tarif, $radiasi,
                 $biayaWp, $degradasi, $faktor, $jumlahPanel, $kapasitasWp, $tagihan,
                 $produksi, $hemat, $payback, $emisi);
            """,
            ("$id", s.Id),
            ("$nama", s.Nama),
            ("$tanggal", s.Tanggal.ToString("o", CultureInfo.InvariantCulture)),
            ("$lokasi", lokasiId), ("$panel", panelId), ("$tarif", tarifId), ("$radiasi", radiasiId),
            ("$biayaWp", analisis?.BiayaPerWp),
            ("$degradasi", analisis?.DegradasiTahunan),
            ("$faktor", analisis is null ? (double?)null : DampakLingkungan.FaktorEmisiBawaan),
            ("$jumlahPanel", s.JumlahPanel),
            ("$kapasitasWp", s.KapasitasKwp * 1000),   // ERD menyimpan Wp
            ("$tagihan", s.TagihanBulananKwh),
            ("$produksi", s.ProduksiTahunan),
            ("$hemat", s.PenghematanTahunan),
            ("$payback", s.PaybackPeriod),
            ("$emisi", s.EmisiDihindari)))
            baris = cmd.ExecuteNonQuery();

        tx.Commit();
        return baris == 1;
    }

    private static string CariAtauBuatPanel(SqliteConnection db, SqliteTransaction tx, PanelSurya p)
    {
        string jenis = p is PanelThinFilm ? "thin_film" : "monokristalin";
        var nilai = new (string Nama, object? Nilai)[]
        {
            ("$merk", p.Merk), ("$wp", p.KapasitasWp), ("$ef", p.Efisiensi), ("$koef", p.KoefisienSuhu),
            ("$luas", p.LuasPerPanel), ("$harga", p.HargaPerUnit), ("$jenis", jenis),
        };

        using (var cari = Perintah(db, tx, """
            SELECT id FROM panel_surya
            WHERE merk = $merk AND kapasitas_wp = $wp AND efisiensi = $ef AND koefisien_suhu = $koef
              AND luas_per_panel = $luas AND harga_per_unit = $harga
              AND COALESCE(jenis, 'monokristalin') = $jenis
            LIMIT 1;
            """, nilai))
        {
            if (cari.ExecuteScalar() is string ada) return ada;
        }

        string id = "panel-" + Guid.NewGuid().ToString("N");
        using var tambah = Perintah(db, tx, """
            INSERT INTO panel_surya (id, merk, kapasitas_wp, efisiensi, koefisien_suhu, luas_per_panel, harga_per_unit, jenis)
            VALUES ($id, $merk, $wp, $ef, $koef, $luas, $harga, $jenis);
            """, nilai.Append(("$id", (object?)id)).ToArray());
        tambah.ExecuteNonQuery();
        return id;
    }

    private static string CariAtauBuatTarif(SqliteConnection db, SqliteTransaction tx, TarifListrikPLN t)
    {
        using (var cari = Perintah(db, tx,
            "SELECT id FROM tarif_listrik_pln WHERE golongan = $golongan AND tarif_per_kwh = $tarif LIMIT 1;",
            ("$golongan", t.Golongan), ("$tarif", t.getTarifPerKWh())))
        {
            if (cari.ExecuteScalar() is string ada) return ada;
        }

        string id = "tarif-" + Guid.NewGuid().ToString("N");
        using var tambah = Perintah(db, tx, """
            INSERT INTO tarif_listrik_pln (id, golongan, tarif_per_kwh, tanggal_berlaku)
            VALUES ($id, $golongan, $tarif, $tanggal);
            """,
            ("$id", id), ("$golongan", t.Golongan), ("$tarif", t.getTarifPerKWh()),
            ("$tanggal", t.TanggalBerlaku.ToString("o", CultureInfo.InvariantCulture)));
        tambah.ExecuteNonQuery();
        return id;
    }

    private static string SimpanRadiasi(SqliteConnection db, SqliteTransaction tx, string lokasiId, DataRadiasi r)
    {
        string id = Guid.NewGuid().ToString("N");
        using (var cmd = Perintah(db, tx, """
            INSERT INTO data_radiasi (id, lokasi_id, peak_sun_hours_tahunan, suhu_rata_rata, sumber_data, tanggal_ambil)
            VALUES ($id, $lokasi, $psh, $suhu, $sumber, $tanggal);
            """,
            ("$id", id), ("$lokasi", lokasiId), ("$psh", r.getRataRataTahunan()), ("$suhu", r.SuhuRataRata),
            ("$sumber", r.SumberData), ("$tanggal", r.TanggalAmbil.ToString("o", CultureInfo.InvariantCulture))))
            cmd.ExecuteNonQuery();

        for (int bulan = 1; bulan <= 12; bulan++)
        {
            using var cmd = Perintah(db, tx,
                "INSERT INTO radiasi_bulanan (data_radiasi_id, bulan, peak_sun_hours) VALUES ($id, $bulan, $psh);",
                ("$id", id), ("$bulan", bulan), ("$psh", r.getPeakSunHours(bulan)));
            cmd.ExecuteNonQuery();
        }
        return id;
    }

    /// <summary>
    /// Data radiasi API terbaru untuk koordinat ini yang diambil sejak <paramref name="sejak"/>, atau null.
    /// Data cadangan tidak pernah dipakai ulang supaya lokasi itu dicoba lagi ke API pada simulasi berikutnya.
    /// </summary>
    public DataRadiasi? cariDataRadiasi(double latitude, double longitude, DateTime sejak)
    {
        using var db = BukaKoneksi();
        string id, sumber;
        double suhu;
        DateTime diambil;
        // ponytail: ORDER BY teks tanggal; benar selama semua baris ditulis dengan zona waktu yang sama.
        using (var cmd = Perintah(db, null, """
            SELECT d.id, d.suhu_rata_rata, d.sumber_data, d.tanggal_ambil
            FROM data_radiasi d JOIN lokasi l ON l.id = d.lokasi_id
            WHERE abs(l.latitude - $lat) < 1e-6 AND abs(l.longitude - $lon) < 1e-6
              AND d.sumber_data NOT LIKE 'Cadangan%'
            ORDER BY d.tanggal_ambil DESC
            LIMIT 1;
            """, ("$lat", latitude), ("$lon", longitude)))
        using (var baca = cmd.ExecuteReader())
        {
            if (!baca.Read()) return null;
            id = baca.GetString(0);
            suhu = baca.GetDouble(1);
            sumber = baca.GetString(2);
            diambil = Tanggal(baca.GetString(3));
        }
        if (diambil < sejak) return null;

        var bulanan = new double[12];
        int terisi = 0;
        using (var cmd = Perintah(db, null,
            "SELECT bulan, peak_sun_hours FROM radiasi_bulanan WHERE data_radiasi_id = $id;", ("$id", id)))
        using (var baca = cmd.ExecuteReader())
        {
            while (baca.Read())
            {
                bulanan[baca.GetInt32(0) - 1] = baca.GetDouble(1);
                terisi++;
            }
        }
        return terisi == 12 ? new DataRadiasi(bulanan.ToList(), suhu, sumber, diambil) : null;
    }

    // ---- Muat ---------------------------------------------------------------------------------

    // Satu query dengan LEFT JOIN: baris lama (FK kosong) tetap muncul dengan kolom input bernilai NULL.
    // Urutan kolom dipakai oleh Rakit(); ubah keduanya bersamaan.
    private const string QueryMuat = """
        SELECT s.id, s.nama, s.tanggal, s.produksi_tahunan, s.penghematan_tahunan, s.payback_period, s.emisi_dihindari,
               s.jumlah_panel, s.kapasitas_wp, s.biaya_per_wp, s.degradasi_tahunan, s.tagihan_bulanan_kwh,
               l.nama, l.latitude, l.longitude, l.luas_atap, l.orientasi, l.kemiringan,
               p.merk, p.kapasitas_wp, p.efisiensi, p.koefisien_suhu, p.luas_per_panel, p.harga_per_unit, p.jenis,
               t.golongan, t.tarif_per_kwh, t.tanggal_berlaku,
               d.id, d.suhu_rata_rata, d.sumber_data, d.tanggal_ambil
        FROM skenario s
        LEFT JOIN lokasi l ON l.id = s.lokasi_id
        LEFT JOIN panel_surya p ON p.id = s.panel_id
        LEFT JOIN tarif_listrik_pln t ON t.id = s.tarif_id
        LEFT JOIN data_radiasi d ON d.id = s.data_radiasi_id
        WHERE ($id IS NULL OR s.id = $id)
        ORDER BY s.tanggal DESC;
        """;

    private List<SimulasiSkenario> Muat(string? id)
    {
        using var db = BukaKoneksi();

        var bulanan = new Dictionary<string, double?[]>();
        using (var cmd = Perintah(db, null, "SELECT data_radiasi_id, bulan, peak_sun_hours FROM radiasi_bulanan;"))
        using (var baca = cmd.ExecuteReader())
        {
            while (baca.Read())
            {
                string kunci = baca.GetString(0);
                if (!bulanan.TryGetValue(kunci, out var larik))
                    bulanan[kunci] = larik = new double?[12];
                larik[baca.GetInt32(1) - 1] = baca.GetDouble(2);
            }
        }

        var daftar = new List<SimulasiSkenario>();
        using (var cmd = Perintah(db, null, QueryMuat, ("$id", id)))
        using (var baca = cmd.ExecuteReader())
        {
            while (baca.Read())
                daftar.Add(Rakit(baca, bulanan));
        }
        return daftar;
    }

    private static bool Semua(SqliteDataReader r, params int[] kolom) => kolom.All(k => !r.IsDBNull(k));

    private static DateTime Tanggal(string teks) =>
        DateTime.Parse(teks, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    /// <summary>Merakit skenario utuh bila semua inputnya ada; kalau tidak, kembalikan baris lama yang hanya berisi hasil.</summary>
    private static SimulasiSkenario Rakit(SqliteDataReader r, Dictionary<string, double?[]> bulanan)
    {
        string id = r.GetString(0), nama = r.GetString(1);
        DateTime tanggal = Tanggal(r.GetString(2));
        double produksi = r.GetDouble(3), hemat = r.GetDouble(4), payback = r.GetDouble(5), emisi = r.GetDouble(6);

        bool lengkap = Semua(r, 7, 8, 9, 10)                      // skenario: jumlah panel, kapasitas, biaya/Wp, degradasi
                    && Semua(r, 12, 13, 14, 15, 16, 17)           // lokasi
                    && Semua(r, 18, 19, 20, 21, 22, 23)           // panel
                    && Semua(r, 25, 26, 27)                       // tarif
                    && Semua(r, 28, 29, 30, 31);                  // data radiasi

        if (lengkap && bulanan.TryGetValue(r.GetString(28), out var bulan) && bulan.All(x => x.HasValue))
        {
            try
            {
                var lokasi = new Lokasi(r.GetString(12), r.GetDouble(13), r.GetDouble(14),
                                        r.GetDouble(15), r.GetDouble(16), r.GetDouble(17));

                string jenis = r.IsDBNull(24) ? "monokristalin" : r.GetString(24);
                PanelSurya panel = jenis == "thin_film"
                    ? new PanelThinFilm(r.GetString(18), r.GetDouble(19), r.GetDouble(20), r.GetDouble(21), r.GetDouble(22), r.GetDouble(23))
                    : new PanelMonokristal(r.GetString(18), r.GetDouble(19), r.GetDouble(20), r.GetDouble(21), r.GetDouble(22), r.GetDouble(23));

                var tarif = new TarifListrikPLN(r.GetString(25), r.GetDouble(26), Tanggal(r.GetString(27)));
                var radiasi = new DataRadiasi(bulan.Select(x => x!.Value).ToList(), r.GetDouble(29),
                                              r.GetString(30), Tanggal(r.GetString(31)));
                var analisis = new AnalisisFinansial(r.GetDouble(9), r.GetDouble(10));

                return new SimulasiSkenario(id, nama, tanggal, lokasi, panel, radiasi, analisis, tarif,
                                            r.GetInt32(7), r.GetDouble(8) / 1000.0,   // ERD menyimpan Wp
                                            produksi, hemat, payback, emisi)
                {
                    TagihanBulananKwh = r.IsDBNull(11) ? (double?)null : r.GetDouble(11),
                };
            }
            catch (Exception e) when (e is ArgumentException or InvalidOperationException or FormatException)
            {
                // Input tersimpan tidak bisa dirakit (misalnya data rusak): tetap tampilkan hasilnya.
            }
        }

        return new SimulasiSkenario(id, nama, tanggal, produksi, hemat, payback, emisi);
    }

    public List<SimulasiSkenario> ambilSemua() => Muat(null);

    public SimulasiSkenario? ambilById(string id) => Muat(id).FirstOrDefault();

    // ---- Hapus --------------------------------------------------------------------------------

    public bool hapus(string id)
    {
        using var db = BukaKoneksi();
        using var tx = db.BeginTransaction();
        bool terhapus = HapusDi(db, tx, id);
        tx.Commit();
        return terhapus;
    }

    /// <summary>
    /// Menghapus skenario, lalu data radiasi dan lokasinya bila tidak dipakai skenario lain.
    /// Panel dan tarif adalah data master, jadi tidak ikut dihapus.
    /// </summary>
    private static bool HapusDi(SqliteConnection db, SqliteTransaction tx, string id)
    {
        string? lokasiId = null, radiasiId = null;
        using (var cari = Perintah(db, tx, "SELECT lokasi_id, data_radiasi_id FROM skenario WHERE id = $id;", ("$id", id)))
        using (var baca = cari.ExecuteReader())
        {
            if (baca.Read())
            {
                lokasiId = baca.IsDBNull(0) ? null : baca.GetString(0);
                radiasiId = baca.IsDBNull(1) ? null : baca.GetString(1);
            }
        }

        int terhapus;
        using (var hapus = Perintah(db, tx, "DELETE FROM skenario WHERE id = $id;", ("$id", id)))
            terhapus = hapus.ExecuteNonQuery();

        if (radiasiId is not null)
        {
            using var cmd = Perintah(db, tx, """
                DELETE FROM data_radiasi WHERE id = $id
                  AND NOT EXISTS (SELECT 1 FROM skenario WHERE data_radiasi_id = $id);
                """, ("$id", radiasiId));
            cmd.ExecuteNonQuery();
        }

        if (lokasiId is not null)
        {
            using var cmd = Perintah(db, tx, """
                DELETE FROM lokasi WHERE id = $id
                  AND NOT EXISTS (SELECT 1 FROM skenario WHERE lokasi_id = $id)
                  AND NOT EXISTS (SELECT 1 FROM data_radiasi WHERE lokasi_id = $id);
                """, ("$id", lokasiId));
            cmd.ExecuteNonQuery();
        }

        return terhapus > 0;
    }
}