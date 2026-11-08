using System;
using System.Collections.Generic;
using System.Globalization;
using Npgsql;
using SunCost.Models;

namespace SunCost.Services;

/// <summary>Menyimpan skenario pada tabel skenario di PostgreSQL (database suncost dari Modul 5).</summary>
public class RepositoriSimulasiPostgres : IRepositoriSimulasi
{
    private readonly string connectionString;

    public string Jenis => "PostgreSQL";

    public RepositoriSimulasiPostgres(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public bool simpan(SimulasiSkenario s)
    {
        using var db = new NpgsqlConnection(connectionString);
        db.Open();
        using var cmd = new NpgsqlCommand("""
            INSERT INTO skenario
                (id, nama, tanggal, produksi_tahunan, penghematan_tahunan, payback_period, emisi_dihindari)
            VALUES (@id, @nama, @tanggal, @produksi, @hemat, @payback, @emisi)
            ON CONFLICT (id) DO UPDATE SET
                nama                = EXCLUDED.nama,
                tanggal             = EXCLUDED.tanggal,
                produksi_tahunan    = EXCLUDED.produksi_tahunan,
                penghematan_tahunan = EXCLUDED.penghematan_tahunan,
                payback_period      = EXCLUDED.payback_period,
                emisi_dihindari     = EXCLUDED.emisi_dihindari;
            """, db);
        cmd.Parameters.AddWithValue("id", s.Id);
        cmd.Parameters.AddWithValue("nama", s.Nama);
        cmd.Parameters.AddWithValue("tanggal", s.Tanggal.ToString("o", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("produksi", s.ProduksiTahunan);
        cmd.Parameters.AddWithValue("hemat", s.PenghematanTahunan);
        cmd.Parameters.AddWithValue("payback", s.PaybackPeriod);
        cmd.Parameters.AddWithValue("emisi", s.EmisiDihindari);
        return cmd.ExecuteNonQuery() == 1;
    }

    public List<SimulasiSkenario> ambilSemua()
    {
        var daftar = new List<SimulasiSkenario>();

        using var db = new NpgsqlConnection(connectionString);
        db.Open();
        using var cmd = new NpgsqlCommand("""
            SELECT id, nama, tanggal, produksi_tahunan, penghematan_tahunan, payback_period, emisi_dihindari
            FROM skenario ORDER BY tanggal DESC;
            """, db);
        using var baca = cmd.ExecuteReader();
        while (baca.Read())
        {
            daftar.Add(new SimulasiSkenario(
                baca.GetString(0),
                baca.GetString(1),
                DateTime.Parse(baca.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                AngkaAtauNol(baca, 3),
                AngkaAtauNol(baca, 4),
                AngkaAtauNol(baca, 5),
                AngkaAtauNol(baca, 6)));
        }
        return daftar;
    }

    public SimulasiSkenario? ambilById(string id) => ambilSemua().Find(s => s.Id == id);

    public bool hapus(string id)
    {
        using var db = new NpgsqlConnection(connectionString);
        db.Open();
        using var cmd = new NpgsqlCommand("DELETE FROM skenario WHERE id = @id;", db);
        cmd.Parameters.AddWithValue("id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    // Kolom hasil di tabel PostgreSQL boleh NULL, jadi dibaca aman.
    private static double AngkaAtauNol(NpgsqlDataReader baca, int kolom) =>
        baca.IsDBNull(kolom) ? 0 : Convert.ToDouble(baca.GetValue(kolom));
}