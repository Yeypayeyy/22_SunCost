using System;

namespace SunCost.Services;

/// <summary>Menghitung emisi CO2 yang dihindari karena listrik diganti energi surya.</summary>
public class DampakLingkungan
{
    /// <summary>Serapan CO2 satu pohon dewasa per tahun (kg), angka acuan umum.</summary>
    private const double SerapanPohonKgPerTahun = 22.0;

        /// <summary>Faktor emisi jaringan Jawa-Bali (KESDM), kg CO2 per kWh. Nilai mengikuti ADR 0001.</summary>
    public const double FaktorEmisiBawaan = 0.87;

    
    /// <summary>Emisi rata-rata mobil bensin, kg CO2 per km (angka acuan umum).</summary>
    public const double KgCO2PerKmMobil = 0.192;

    private double faktorEmisiCO2;   // kg CO2 per kWh listrik jaringan PLN

    public DampakLingkungan(double faktorEmisiCO2 = FaktorEmisiBawaan)
    {
        this.faktorEmisiCO2 = faktorEmisiCO2;
    }

    /// <summary>Emisi yang dihindari (kg CO2 per tahun) dari produksi tahunan dalam kWh.</summary>
    public double hitungEmisiDihindari(double produksiTahunan) => produksiTahunan * faktorEmisiCO2;

    /// <summary>Jumlah pohon yang dibutuhkan untuk menyerap emisi sebanyak itu dalam setahun.</summary>
    public int setaraPohon(double emisi) => (int)Math.Ceiling(emisi / SerapanPohonKgPerTahun);

    
    /// <summary>Emisi (kg CO2) dinyatakan sebagai kilometer mobil bensin yang tidak ditempuh.</summary>
    public double setaraKilometerMobil(double emisi) => emisi / KgCO2PerKmMobil;

    /// <summary>Total CO2 yang dihindari selama umur sistem; produksi turun tiap tahun akibat degradasi panel.</summary>
    public double totalEmisi25Tahun(double produksiTahunan, double degradasiTahunan)
    {
        double total = 0;
        for (int tahun = 0; tahun < AnalisisFinansial.UmurSistemTahun; tahun++)
            total += hitungEmisiDihindari(produksiTahunan * Math.Pow(1 - degradasiTahunan / 100, tahun));
        return total;
    }
}
