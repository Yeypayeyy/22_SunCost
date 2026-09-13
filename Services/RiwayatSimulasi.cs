using System;
using System.Collections.Generic;
using SunCost.Models;

namespace SunCost.Services;

/// <summary>Daftar skenario yang pernah disimulasikan, disinkronkan dengan database.</summary>
public class RiwayatSimulasi
{
    private List<SimulasiSkenario> daftarSkenario = new();

    private readonly RepositoriSimulasi repositori;   // relasi «uses» pada class diagram

    public RiwayatSimulasi(RepositoriSimulasi repositori)
    {
        this.repositori = repositori;
    }

    public void tambahSkenario(SimulasiSkenario s)
    {
        if (!repositori.simpan(s))
            throw new InvalidOperationException("Skenario gagal disimpan.");
        daftarSkenario.Add(s);
    }

    public List<SimulasiSkenario> muatRiwayat()
    {
        daftarSkenario = repositori.ambilSemua();
        return daftarSkenario;
    }

    /// <summary>Kalimat perbandingan: skenario mana yang lebih cepat balik modal dan selisih penghematannya.</summary>
    public string bandingkanSkenario(SimulasiSkenario a, SimulasiSkenario b)
    {
        double selisihHemat = Math.Abs(a.PenghematanTahunan - b.PenghematanTahunan);
        string hemat = string.Format(FormatId.Angka, "Selisih penghematan Rp {0:#,##0} per tahun.", selisihHemat);

        if (a.PaybackPeriod.Equals(b.PaybackPeriod))
            return $"{a.Nama} dan {b.Nama} balik modal dalam waktu yang sama. {hemat}";

        var cepat = a.PaybackPeriod < b.PaybackPeriod ? a : b;
        var lambat = ReferenceEquals(cepat, a) ? b : a;
        return $"{cepat.Nama} balik modal lebih cepat daripada {lambat.Nama}. {hemat}";
    }

    public bool hapusSkenario(string id)
    {
        bool terhapus = repositori.hapus(id);
        daftarSkenario.RemoveAll(s => s.Id == id);
        return terhapus;
    }
}
