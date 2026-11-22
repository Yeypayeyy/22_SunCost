using System;
using System.Collections.Generic;
using System.Linq;
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

    // Rincian hasil. Kosong/null untuk baris lama yang tidak punya input tersimpan.
    private double[] produksiBulanan = Array.Empty<double>();   // kWh, indeks 0 = Januari
    private double? kapasitasKwp;
    private int? jumlahPanel;
    private double? biayaInstalasi;       // rupiah
    private double? keuntungan25Tahun;    // rupiah

    // Asosiasi 1-1 pada class diagram. Null hanya untuk baris lama (dari database sebelum input disimpan).
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

    /// <summary>
    /// Skenario utuh yang dibaca dari database: input lengkap plus hasil tersimpan (snapshot).
    /// Rincian (produksi bulanan, biaya, keuntungan) diturunkan dari input, dan skenario ini
    /// tetap bisa dihitung ulang dengan <see cref="jalankanSimulasi"/>.
    /// </summary>
    public SimulasiSkenario(string id, string nama, DateTime tanggal,
                            Lokasi lokasi, PanelSurya panel, DataRadiasi dataRadiasi,
                            AnalisisFinansial analisis, TarifListrikPLN tarif,
                            int jumlahPanel, double kapasitasKwp,
                            double produksiTahunan, double penghematanTahunan,
                            double paybackPeriod, double emisiDihindari)
    {
        this.id = id;
        this.nama = nama;
        this.tanggal = tanggal;
        this.lokasi = lokasi;
        this.panel = panel;
        this.dataRadiasi = dataRadiasi;
        this.analisis = analisis;
        this.tarif = tarif;
        this.jumlahPanel = jumlahPanel;
        this.kapasitasKwp = kapasitasKwp;
        this.produksiTahunan = produksiTahunan;
        this.penghematanTahunan = penghematanTahunan;
        this.paybackPeriod = paybackPeriod;
        this.emisiDihindari = emisiDihindari;

        var kalkulator = new KalkulatorEnergi(lokasi, panel, dataRadiasi);
        produksiBulanan = Enumerable.Range(1, 12).Select(kalkulator.hitungProduksiBulanan).ToArray();
        biayaInstalasi = analisis.hitungBiayaInstalasi(kapasitasKwp * 1000);
        analisis.hitungPenghematanBulanan(produksiTahunan / 12, tarif);   // memenuhi guard AnalisisFinansial
        keuntungan25Tahun = analisis.proyeksiKeuntungan25Tahun();
    }

    /// <summary>Baris lama yang hanya punya hasil (tanpa input); dipakai untuk database sebelum input disimpan.</summary>
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

    // Akses baca untuk RepositoriSimulasi, RiwayatSimulasi, dan halaman Hasil.
    public string Id => id;
    public string Nama => nama;
    public DateTime Tanggal => tanggal;
    public double ProduksiTahunan => produksiTahunan;
    public double PenghematanTahunan => penghematanTahunan;
    public double PaybackPeriod => paybackPeriod;
    public double EmisiDihindari => emisiDihindari;

    /// <summary>Produksi 12 bulan (kWh); kosong kalau skenario tidak punya input tersimpan.</summary>
    public IReadOnlyList<double> ProduksiBulanan => produksiBulanan;
    public double? KapasitasKwp => kapasitasKwp;
    public int? JumlahPanel => jumlahPanel;
    public double? BiayaInstalasi => biayaInstalasi;
    public double? Keuntungan25Tahun => keuntungan25Tahun;

    // Input skenario; null untuk baris lama.
    public Lokasi? Lokasi => lokasi;
    public PanelSurya? Panel => panel;
    public DataRadiasi? DataRadiasi => dataRadiasi;
    public TarifListrikPLN? Tarif => tarif;
    public AnalisisFinansial? Analisis => analisis;

    /// <summary>Konsumsi listrik bulanan pengguna (kWh) dari isian "Tagihan listrik bulanan"; dipakai KPI "Setara konsumsi".</summary>
    public double? TagihanBulananKwh { get; init; }

    public void jalankanSimulasi()
    {
        if (lokasi is null || panel is null || dataRadiasi is null || analisis is null || tarif is null)
            throw new InvalidOperationException("Skenario dari riwayat tidak dapat dihitung ulang.");
        if (!lokasi.validasi())
            throw new InvalidOperationException("Data lokasi tidak valid.");

        var kalkulator = new KalkulatorEnergi(lokasi, panel, dataRadiasi);
        var dampak = new DampakLingkungan();

        kapasitasKwp = kalkulator.hitungKapasitasTerpasang(lokasi, panel);
        jumlahPanel = panel.hitungJumlahPanel(lokasi.hitungLuasEfektif());
        produksiBulanan = Enumerable.Range(1, 12).Select(kalkulator.hitungProduksiBulanan).ToArray();
        produksiTahunan = kalkulator.hitungProduksiTahunan();

        biayaInstalasi = analisis.hitungBiayaInstalasi(kapasitasKwp.Value * 1000);
        penghematanTahunan = analisis.hitungPenghematanBulanan(produksiTahunan / 12, tarif) * 12;
        paybackPeriod = analisis.hitungPaybackPeriod();
        keuntungan25Tahun = analisis.proyeksiKeuntungan25Tahun();
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