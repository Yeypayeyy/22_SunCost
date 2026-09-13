using System;

namespace SunCost.Services;

/// <summary>Menghitung emisi CO2 yang dihindari karena listrik diganti energi surya.</summary>
public class DampakLingkungan
{
    /// <summary>Serapan CO2 satu pohon dewasa per tahun (kg), angka acuan umum.</summary>
    private const double SerapanPohonKgPerTahun = 22.0;

    private double faktorEmisiCO2;   // kg CO2 per kWh listrik jaringan PLN

    public DampakLingkungan(double faktorEmisiCO2 = 0.794)
    {
        this.faktorEmisiCO2 = faktorEmisiCO2;
    }

    /// <summary>Emisi yang dihindari (kg CO2 per tahun) dari produksi tahunan dalam kWh.</summary>
    public double hitungEmisiDihindari(double produksiTahunan) => produksiTahunan * faktorEmisiCO2;

    /// <summary>Jumlah pohon yang dibutuhkan untuk menyerap emisi sebanyak itu dalam setahun.</summary>
    public int setaraPohon(double emisi) => (int)Math.Ceiling(emisi / SerapanPohonKgPerTahun);
}
