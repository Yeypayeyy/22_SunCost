using System.Globalization;

namespace SunCost.Models;

/// <summary>Ringkasan satu simulasi untuk ditampilkan sebagai kartu di beranda.</summary>
public class SimulasiRingkas
{
    // Format angka Indonesia didefinisikan eksplisit, bukan lewat CultureInfo("id-ID"),
    // supaya hasilnya sama di mesin mana pun (termasuk mode globalization-invariant).
    private static readonly NumberFormatInfo Id = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
        NumberGroupSizes = new[] { 3 }
    };

    public string Lokasi { get; init; } = "";
    public double KapasitasKwp { get; init; }
    public double PaybackTahun { get; init; }
    public decimal HematPerBulan { get; init; }

    public string KapasitasTeks => KapasitasKwp.ToString("0.0", Id) + " kWp";
    public string PaybackTeks => PaybackTahun.ToString("0.0", Id) + " tahun";
    public string HematTeks => "Rp " + HematPerBulan.ToString("#,##0", Id);
}
