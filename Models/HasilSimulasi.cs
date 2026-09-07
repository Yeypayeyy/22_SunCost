using System.Globalization;

namespace SunCost.Models;

/// <summary>Keluaran satu perhitungan: sisi energi, finansial, dan lingkungan.</summary>
public class HasilSimulasi
{
    private static readonly NumberFormatInfo Id = FormatId.Angka;

    public double KapasitasKwp { get; set; }
    public int JumlahPanel { get; set; }
    public double EnergiTahunanKwh { get; set; }
    public double EnergiBulananKwh => EnergiTahunanKwh / 12;

    public decimal BiayaInstalasi { get; set; }
    public decimal HematPerBulan { get; set; }
    public decimal HematPerTahun => HematPerBulan * 12;
    public double PaybackTahun { get; set; }
    public decimal KeuntunganBersih25Tahun { get; set; }

    public double Co2DihindariKgPerTahun { get; set; }

    public string KapasitasTeks => KapasitasKwp.ToString("0.00", Id) + " kWp";
    public string PaybackTeks => PaybackTahun.ToString("0.0", Id) + " tahun";
    public string HematTeks => "Rp " + HematPerBulan.ToString("#,##0", Id);
}
