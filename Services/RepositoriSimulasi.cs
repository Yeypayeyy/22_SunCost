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
    private string connectionString;

    public RepositoriSimulasi(string? berkasDb = null)
    {
        berkasDb ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SunCost", "suncost.db");
        Directory.CreateDirectory(Path.GetDirectoryName(berkasDb)!);
        connectionString = new SqliteConnectionStringBuilder { DataSource = berkasDb }.ToString();

        using var db = new SqliteConnection(connectionString);
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS skenario (
                id                  TEXT PRIMARY KEY,
                nama                TEXT NOT NULL,
                tanggal             TEXT NOT NULL,
                produksi_tahunan    REAL NOT NULL,
                penghematan_tahunan REAL NOT NULL,
                payback_period      REAL NOT NULL,
                emisi_dihindari     REAL NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public bool simpan(SimulasiSkenario s)
    {
        using var db = new SqliteConnection(connectionString);
        db.Open();
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

        using var db = new SqliteConnection(connectionString);
        db.Open();
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

    // ponytail: memindai seluruh riwayat; ganti ke query WHERE id kalau riwayat mencapai ribuan baris.
    public SimulasiSkenario? ambilById(string id) => ambilSemua().Find(s => s.Id == id);

    public bool hapus(string id)
    {
        using var db = new SqliteConnection(connectionString);
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM skenario WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteNonQuery() > 0;
    }
}
