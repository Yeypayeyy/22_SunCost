using System.Globalization;

namespace SunCost.Models;

/// <summary>Ringkasan satu simulasi untuk ditampilkan sebagai kartu di beranda.</summary>
public class SimulasiRingkas
{
    private static readonly NumberFormatInfo Id = FormatId.Angka;

    public string Lokasi { get; init; } = "";
    public double KapasitasKwp { get; init; }
    public double PaybackTahun { get; init; }
    public decimal HematPerBulan { get; init; }

    public string KapasitasTeks => KapasitasKwp.ToString("0.0", Id) + " kWp";
    public string PaybackTeks => PaybackTahun.ToString("0.0", Id) + " tahun";
    public string HematTeks => "Rp " + HematPerBulan.ToString("#,##0", Id);

    public static SimulasiRingkas Dari(Simulasi s) => new()
    {
        Lokasi = s.Lokasi.Nama,
        KapasitasKwp = s.Hasil?.KapasitasKwp ?? 0,
        PaybackTahun = s.Hasil?.PaybackTahun ?? 0,
        HematPerBulan = s.Hasil?.HematPerBulan ?? 0
    };
}
