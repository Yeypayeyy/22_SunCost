using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using SunCost.Models;

namespace SunCost.Services;

/// <summary>Riwayat simulasi disimpan pada satu berkas SQLite di folder data aplikasi pengguna.</summary>
public class RiwayatRepositorySqlite : IRiwayatRepository
{
    private readonly string _connectionString;

    public RiwayatRepositorySqlite(string? berkasDb = null)
    {
        berkasDb ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SunCost", "suncost.db");

        Directory.CreateDirectory(Path.GetDirectoryName(berkasDb)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = berkasDb }.ToString();
        SiapkanSkema();
    }

    private void SiapkanSkema()
    {
        using var db = Buka();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS simulasi (
                id                INTEGER PRIMARY KEY AUTOINCREMENT,
                tanggal           TEXT    NOT NULL,
                catatan           TEXT    NOT NULL DEFAULT '',
                lokasi_nama       TEXT    NOT NULL,
                latitude          REAL    NOT NULL,
                longitude         REAL    NOT NULL,
                luas_m2           REAL    NOT NULL,
                orientasi         INTEGER NOT NULL,
                kemiringan        REAL    NOT NULL,
                panel_model       TEXT    NOT NULL,
                panel_wp          INTEGER NOT NULL,
                panel_harga_wp    TEXT    NOT NULL,
                tarif_golongan    TEXT    NOT NULL,
                tarif_rp_kwh      TEXT    NOT NULL,
                psh               REAL    NOT NULL,
                suhu              REAL    NOT NULL,
                kapasitas_kwp     REAL    NOT NULL,
                energi_tahunan    REAL    NOT NULL,
                biaya_instalasi   TEXT    NOT NULL,
                hemat_per_bulan   TEXT    NOT NULL,
                payback_tahun     REAL    NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public int Simpan(Simulasi s)
    {
        var h = s.Hasil ?? throw new InvalidOperationException("Simulasi belum dihitung.");

        using var db = Buka();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            INSERT INTO simulasi (tanggal, catatan, lokasi_nama, latitude, longitude,
                luas_m2, orientasi, kemiringan, panel_model, panel_wp, panel_harga_wp,
                tarif_golongan, tarif_rp_kwh, psh, suhu,
                kapasitas_kwp, energi_tahunan, biaya_instalasi, hemat_per_bulan, payback_tahun)
            VALUES ($tanggal, $catatan, $lokasi, $lat, $lon,
                $luas, $orientasi, $kemiringan, $model, $wp, $hargaWp,
                $golongan, $tarif, $psh, $suhu,
                $kwp, $energi, $biaya, $hemat, $payback);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$tanggal", s.Tanggal.ToString("o"));
        cmd.Parameters.AddWithValue("$catatan", s.Catatan);
        cmd.Parameters.AddWithValue("$lokasi", s.Lokasi.Nama);
        cmd.Parameters.AddWithValue("$lat", s.Lokasi.Latitude);
        cmd.Parameters.AddWithValue("$lon", s.Lokasi.Longitude);
        cmd.Parameters.AddWithValue("$luas", s.Atap.LuasM2);
        cmd.Parameters.AddWithValue("$orientasi", (int)s.Atap.Orientasi);
        cmd.Parameters.AddWithValue("$kemiringan", s.Atap.KemiringanDerajat);
        cmd.Parameters.AddWithValue("$model", s.Panel.Model);
        cmd.Parameters.AddWithValue("$wp", s.Panel.DayaWp);
        // Nilai rupiah disimpan sebagai teks: SQLite hanya punya REAL, dan pembulatan biner merusak uang.
        cmd.Parameters.AddWithValue("$hargaWp", s.Panel.HargaPerWp.ToString(CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$golongan", s.Tarif.Golongan);
        cmd.Parameters.AddWithValue("$tarif", s.Tarif.RpPerKwh.ToString(CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$psh", s.Iklim.PeakSunHours);
        cmd.Parameters.AddWithValue("$suhu", s.Iklim.SuhuRataRataC);
        cmd.Parameters.AddWithValue("$kwp", h.KapasitasKwp);
        cmd.Parameters.AddWithValue("$energi", h.EnergiTahunanKwh);
        cmd.Parameters.AddWithValue("$biaya", h.BiayaInstalasi.ToString(CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$hemat", h.HematPerBulan.ToString(CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$payback", h.PaybackTahun);

        s.Id = Convert.ToInt32(cmd.ExecuteScalar());
        return s.Id;
    }

    public IReadOnlyList<Simulasi> AmbilSemua()
    {
        var daftar = new List<Simulasi>();

        using var db = Buka();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT * FROM simulasi ORDER BY tanggal DESC;";
        using var baca = cmd.ExecuteReader();

        while (baca.Read())
        {
            var s = new Simulasi
            {
                Id = baca.GetInt32(baca.GetOrdinal("id")),
                Tanggal = DateTime.Parse(baca.GetString(baca.GetOrdinal("tanggal")), CultureInfo.InvariantCulture),
                Catatan = baca.GetString(baca.GetOrdinal("catatan")),
                Lokasi = new Lokasi
                {
                    Nama = baca.GetString(baca.GetOrdinal("lokasi_nama")),
                    Latitude = baca.GetDouble(baca.GetOrdinal("latitude")),
                    Longitude = baca.GetDouble(baca.GetOrdinal("longitude"))
                },
                Atap = new Atap
                {
                    LuasM2 = baca.GetDouble(baca.GetOrdinal("luas_m2")),
                    Orientasi = (Arah)baca.GetInt32(baca.GetOrdinal("orientasi")),
                    KemiringanDerajat = baca.GetDouble(baca.GetOrdinal("kemiringan"))
                },
                Panel = new PanelSurya
                {
                    Model = baca.GetString(baca.GetOrdinal("panel_model")),
                    DayaWp = baca.GetInt32(baca.GetOrdinal("panel_wp")),
                    HargaPerWp = decimal.Parse(baca.GetString(baca.GetOrdinal("panel_harga_wp")), CultureInfo.InvariantCulture)
                },
                Tarif = new TarifListrik
                {
                    Golongan = baca.GetString(baca.GetOrdinal("tarif_golongan")),
                    RpPerKwh = decimal.Parse(baca.GetString(baca.GetOrdinal("tarif_rp_kwh")), CultureInfo.InvariantCulture)
                },
                Iklim = new DataIklim
                {
                    PeakSunHours = baca.GetDouble(baca.GetOrdinal("psh")),
                    SuhuRataRataC = baca.GetDouble(baca.GetOrdinal("suhu"))
                }
            };
            s.Hasil = new HasilSimulasi
            {
                KapasitasKwp = baca.GetDouble(baca.GetOrdinal("kapasitas_kwp")),
                EnergiTahunanKwh = baca.GetDouble(baca.GetOrdinal("energi_tahunan")),
                BiayaInstalasi = Rupiah(baca, "biaya_instalasi"),
                HematPerBulan = Rupiah(baca, "hemat_per_bulan"),
                PaybackTahun = baca.GetDouble(baca.GetOrdinal("payback_tahun"))
            };
            daftar.Add(s);
        }
        return daftar;
    }

    private static decimal Rupiah(SqliteDataReader baca, string kolom) =>
        decimal.Parse(baca.GetString(baca.GetOrdinal(kolom)), CultureInfo.InvariantCulture);

    public void Hapus(int id)
    {
        using var db = Buka();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM simulasi WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Buka()
    {
        var db = new SqliteConnection(_connectionString);
        db.Open();
        return db;
    }
}
