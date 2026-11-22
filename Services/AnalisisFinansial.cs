using System;
using System.Collections.Generic;
using SunCost.Models;

namespace SunCost.Services;

/// <summary>Menilai kelayakan finansial: biaya instalasi, penghematan, balik modal, dan keuntungan.</summary>
public class AnalisisFinansial
{
        /// <summary>Umur sistem yang dianalisis, dalam tahun.</summary>
    public const int UmurSistemTahun = 25;

    /// <summary>Degradasi panel bawaan, persen per tahun.</summary>
    public const double DegradasiBawaan = 0.5;

    private double biayaPerWp;          // rupiah per Wp terpasang (panel, inverter, struktur, jasa)
    private double biayaInstalasi;      // rupiah
    private double penghematanBulanan;  // rupiah pada tahun pertama
    private double degradasiTahunan;    // penurunan produksi panel, persen per tahun

    private bool biayaSudahDihitung;
    private bool penghematanSudahDihitung;

        public AnalisisFinansial(double biayaPerWp, double degradasiTahunan = DegradasiBawaan)
    {
        this.biayaPerWp = biayaPerWp;
        this.degradasiTahunan = degradasiTahunan;
    }

    public double hitungBiayaInstalasi(double kapasitasWp)
    {
        biayaInstalasi = kapasitasWp * biayaPerWp;
        biayaSudahDihitung = true;
        return biayaInstalasi;
    }

    /// <summary>Tagihan PLN yang tidak perlu dibayar untuk produksi (kWh) satu bulan.</summary>
    public double hitungPenghematanBulanan(double produksi, TarifListrikPLN tarif)
    {
        penghematanBulanan = tarif.hitungBiaya(produksi);
        penghematanSudahDihitung = true;
        return penghematanBulanan;
    }

    private void PastikanSiap()
    {
        if (!biayaSudahDihitung || !penghematanSudahDihitung)
            throw new InvalidOperationException(
                "Panggil hitungBiayaInstalasi dan hitungPenghematanBulanan terlebih dahulu.");
    }

    /// <summary>
    /// Tahun saat penghematan kumulatif menutup biaya instalasi. Produksi turun tiap tahun akibat
    /// degradasi, jadi dihitung per tahun, bukan dengan pembagian sederhana.
    /// </summary>
    public double hitungPaybackPeriod()
    {
        PastikanSiap();
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
        /// <summary>
    /// Arus kas kumulatif per akhir tahun 0..25 (26 nilai). Tahun 0 bernilai negatif sebesar biaya
    /// instalasi; titik potong nol sama dengan periode balik modal pada hitungPaybackPeriod.
    /// </summary>
    public List<double> proyeksiArusKas()
    {
        PastikanSiap();
        var arus = new List<double>(UmurSistemTahun + 1);
        double kumulatif = -biayaInstalasi;
        arus.Add(kumulatif);
        for (int tahun = 0; tahun < UmurSistemTahun; tahun++)
        {
            kumulatif += penghematanBulanan * 12 * Math.Pow(1 - degradasiTahunan / 100, tahun);
            arus.Add(kumulatif);
        }
        return arus;
    }

    /// <summary>Total penghematan 25 tahun dikurangi biaya instalasi.</summary>
    public double proyeksiKeuntungan25Tahun()
    {
        PastikanSiap();
        double total = 0;
        for (int tahun = 0; tahun < UmurSistemTahun; tahun++)
            total += penghematanBulanan * 12 * Math.Pow(1 - degradasiTahunan / 100, tahun);
        return total - biayaInstalasi;
    }
}