using System;
using System.Collections.Generic;
using System.Linq;

namespace SunCost.Models;

/// <summary>Data radiasi matahari dan suhu satu lokasi, hasil pembacaan API cuaca.</summary>
public class DataRadiasi
{
    private double peakSunHours;            // rata-rata tahunan, kWh/m2 per hari
    private List<double> radiasiBulanan;    // Peak Sun Hours tiap bulan, indeks 0 = Januari
    private double suhuRataRata;            // derajat Celsius
    private string sumberData;
    private DateTime tanggalAmbil;

    public DataRadiasi(List<double> radiasiBulanan, double suhuRataRata, string sumberData, DateTime tanggalAmbil)
    {
        if (radiasiBulanan.Count != 12)
            throw new ArgumentException("Radiasi bulanan harus berisi 12 nilai.", nameof(radiasiBulanan));

        this.radiasiBulanan = radiasiBulanan;
        this.suhuRataRata = suhuRataRata;
        this.sumberData = sumberData;
        this.tanggalAmbil = tanggalAmbil;
        peakSunHours = radiasiBulanan.Average();
    }

    /// <summary>Dibaca KalkulatorEnergi untuk koreksi suhu.</summary>
    public double SuhuRataRata => suhuRataRata;

    /// <summary>Peak Sun Hours untuk bulan 1 (Januari) sampai 12 (Desember).</summary>
    public double getPeakSunHours(int bulan)
    {
        if (bulan is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(bulan), "Bulan harus 1-12.");
        return radiasiBulanan[bulan - 1];
    }

    public double getRataRataTahunan() => peakSunHours;
}
