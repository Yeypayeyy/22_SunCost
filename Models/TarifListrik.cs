namespace SunCost.Models;

/// <summary>Tarif tenaga listrik PLN yang dipakai untuk menilai penghematan.</summary>
public class TarifListrik
{
    public string Golongan { get; set; } = "";
    public decimal RpPerKwh { get; set; }

    /// <summary>Kenaikan tarif tahunan yang diasumsikan pada proyeksi 25 tahun.</summary>
    public double InflasiTarifPersen { get; set; } = 3.0;

    public static TarifListrik R1_1300VA() => new() { Golongan = "R-1/TR 1.300 VA", RpPerKwh = 1444.70m };
    public static TarifListrik R1_2200VA() => new() { Golongan = "R-1/TR 2.200 VA", RpPerKwh = 1444.70m };
    public static TarifListrik R1M_3500VA() => new() { Golongan = "R-1/TR 3.500-5.500 VA", RpPerKwh = 1699.53m };
}
