using System;
using SunCost.Models;

namespace SunCost.Services;

/// <summary>Menilai kelayakan finansial: biaya instalasi, penghematan, balik modal, dan keuntungan.</summary>
public class AnalisisFinansial
{
    private const int UmurSistemTahun = 25;

    private double biayaPerWp;          // rupiah per Wp terpasang (panel, inverter, struktur, jasa)
    private double biayaInstalasi;      // rupiah
    private double penghematanBulanan;  // rupiah pada tahun pertama
    private double degradasiTahunan;    // penurunan produksi panel, persen per tahun

    public AnalisisFinansial(double biayaPerWp, double degradasiTahunan = 0.5)
    {
        this.biayaPerWp = biayaPerWp;
        this.degradasiTahunan = degradasiTahunan;
    }

    public double hitungBiayaInstalasi(double kapasitasWp)
    {
        biayaInstalasi = kapasitasWp * biayaPerWp;
        return biayaInstalasi;
    }

    /// <summary>Tagihan PLN yang tidak perlu dibayar untuk produksi (kWh) satu bulan.</summary>
    public double hitungPenghematanBulanan(double produksi, TarifListrikPLN tarif)
    {
        penghematanBulanan = tarif.hitungBiaya(produksi);
        return penghematanBulanan;
    }

    /// <summary>
    /// Tahun saat penghematan kumulatif menutup biaya instalasi. Produksi turun tiap tahun akibat
    /// degradasi, jadi dihitung per tahun, bukan dengan pembagian sederhana.
    /// </summary>
    public double hitungPaybackPeriod()
    {
        double kumulatif = 0;
        for (int tahun = 0; tahun < UmurSistemTahun; tahun++)
        {
            double hematTahunIni = penghematanBulanan * 12 * Math.Pow(1 - degradasiTahunan / 100, tahun);
            if (hematTahunIni <= 0) break;

            if (kumulatif + hematTahunIni >= biayaInstalasi)
                return tahun + (biayaInstalasi - kumulatif) / hematTahunIni;

            kumulatif += hematTahunIni;
        }
        return double.PositiveInfinity; // tidak balik modal selama umur sistem
    }

    /// <summary>Total penghematan 25 tahun dikurangi biaya instalasi.</summary>
    public double proyeksiKeuntungan25Tahun()
    {
        double total = 0;
        for (int tahun = 0; tahun < UmurSistemTahun; tahun++)
            total += penghematanBulanan * 12 * Math.Pow(1 - degradasiTahunan / 100, tahun);
        return total - biayaInstalasi;
    }
}
