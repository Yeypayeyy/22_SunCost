using System;
using SunCost.Services;

namespace SunCost.Models;

/// <summary>Satu skenario pemasangan PLTS beserta hasil simulasinya.</summary>
public class SimulasiSkenario
{
    private string id;
    private string nama;
    private DateTime tanggal;
    private double produksiTahunan;      // kWh per tahun
    private double penghematanTahunan;   // rupiah per tahun
    private double paybackPeriod;        // tahun
    private double emisiDihindari;       // kg CO2 per tahun

    // Asosiasi 1-1 pada class diagram. Bernilai null untuk skenario yang dimuat dari riwayat,
    // karena database hanya menyimpan hasil perhitungannya.
    private readonly Lokasi? lokasi;
    private readonly PanelSurya? panel;
    private readonly DataRadiasi? dataRadiasi;
    private readonly AnalisisFinansial? analisis;
    private readonly TarifListrikPLN? tarif;   // diteruskan ke AnalisisFinansial.hitungPenghematanBulanan

    /// <summary>Skenario baru yang siap dihitung dengan <see cref="jalankanSimulasi"/>.</summary>
    public SimulasiSkenario(string nama, Lokasi lokasi, PanelSurya panel, DataRadiasi dataRadiasi,
                            AnalisisFinansial analisis, TarifListrikPLN tarif)
    {
        id = Guid.NewGuid().ToString("N");
        tanggal = DateTime.Now;
        this.nama = nama;
        this.lokasi = lokasi;
        this.panel = panel;
        this.dataRadiasi = dataRadiasi;
        this.analisis = analisis;
        this.tarif = tarif;
    }

    /// <summary>Skenario yang dibaca ulang dari riwayat.</summary>
    public SimulasiSkenario(string id, string nama, DateTime tanggal, double produksiTahunan,
                            double penghematanTahunan, double paybackPeriod, double emisiDihindari)
    {
        this.id = id;
        this.nama = nama;
        this.tanggal = tanggal;
        this.produksiTahunan = produksiTahunan;
        this.penghematanTahunan = penghematanTahunan;
        this.paybackPeriod = paybackPeriod;
        this.emisiDihindari = emisiDihindari;
    }

    // Akses baca untuk RepositoriSimulasi dan RiwayatSimulasi.
    public string Id => id;
    public string Nama => nama;
    public DateTime Tanggal => tanggal;
    public double ProduksiTahunan => produksiTahunan;
    public double PenghematanTahunan => penghematanTahunan;
    public double PaybackPeriod => paybackPeriod;
    public double EmisiDihindari => emisiDihindari;

    public void jalankanSimulasi()
    {
        if (lokasi is null || panel is null || dataRadiasi is null || analisis is null || tarif is null)
            throw new InvalidOperationException("Skenario dari riwayat tidak dapat dihitung ulang.");
        if (!lokasi.validasi())
            throw new InvalidOperationException("Data lokasi tidak valid.");

        var kalkulator = new KalkulatorEnergi(lokasi, panel, dataRadiasi);
        var dampak = new DampakLingkungan();

        double kapasitasKwp = kalkulator.hitungKapasitasTerpasang(lokasi, panel);
        produksiTahunan = kalkulator.hitungProduksiTahunan();

        analisis.hitungBiayaInstalasi(kapasitasKwp * 1000);
        penghematanTahunan = analisis.hitungPenghematanBulanan(produksiTahunan / 12, tarif) * 12;
        paybackPeriod = analisis.hitungPaybackPeriod();
        emisiDihindari = dampak.hitungEmisiDihindari(produksiTahunan);
    }

    public string getRingkasan()
    {
        string payback = double.IsInfinity(paybackPeriod)
            ? "tidak balik modal dalam 25 tahun"
            : "balik modal " + paybackPeriod.ToString("0.0", FormatId.Angka) + " tahun";

        return string.Format(FormatId.Angka,
            "{0}: produksi {1:#,##0} kWh/tahun, hemat Rp {2:#,##0}/tahun, {3}, emisi dihindari {4:#,##0} kg CO2/tahun",
            nama, produksiTahunan, penghematanTahunan, payback, emisiDihindari);
    }
}
