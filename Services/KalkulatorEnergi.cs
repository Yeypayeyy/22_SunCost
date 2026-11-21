using System;
using System.Linq;
using SunCost.Models;

namespace SunCost.Services;

/// <summary>Menghitung kapasitas terpasang dan produksi energi PLTS atap.</summary>
public class KalkulatorEnergi
{
    private static readonly int[] HariPerBulan = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

    /// <summary>Kenaikan suhu sel di atas suhu udara saat penyinaran penuh (pendekatan NOCT).</summary>
    private const double KenaikanSuhuSel = 25.0;

    private double performanceRatio = 0.80;  // rugi inverter, kabel, kotoran, dan mismatch

    // Objek yang dipakai perhitungan (relasi «uses» pada class diagram).
    private readonly Lokasi lokasi;
    private readonly PanelSurya panel;
    private readonly DataRadiasi dataRadiasi;

    /// <summary>Data radiasi diambil langsung dari API cuaca.</summary>
    public KalkulatorEnergi(Lokasi lokasi, PanelSurya panel, LayananCuacaAPI layananCuaca)
        : this(lokasi, panel, layananCuaca.ambilDataRadiasi(lokasi))
    {
    }

    public KalkulatorEnergi(Lokasi lokasi, PanelSurya panel, DataRadiasi dataRadiasi)
    {
        this.lokasi = lokasi;
        this.panel = panel;
        this.dataRadiasi = dataRadiasi;
    }

    /// <summary>Kapasitas (kWp) dari jumlah panel yang benar-benar muat di luas efektif atap.</summary>
    public double hitungKapasitasTerpasang(Lokasi lokasi, PanelSurya panel)
    {
        int jumlah = panel.hitungJumlahPanel(lokasi.hitungLuasEfektif());
        return panel.hitungKapasitasTotal(jumlah) / 1000.0;
    }

    /// <summary>
    /// Faktor pengali akibat panas. Kalkulator hanya mengubah suhu udara menjadi suhu sel;
    /// rumus koreksinya milik panel, jadi tiap jenis panel menjawab panggilan yang sama dengan caranya sendiri.
    /// </summary>
    public double hitungKoreksiSuhu(double suhu, PanelSurya panel) =>
        panel.hitungKoreksiSuhu(suhu + KenaikanSuhuSel);

    /// <summary>Energi bulan ke-1..12 (kWh) = kWp x PSH bulan itu x jumlah hari x PR x koreksi suhu.</summary>
    public double hitungProduksiBulanan(int bulan) =>
        hitungKapasitasTerpasang(lokasi, panel)
        * dataRadiasi.getPeakSunHours(bulan)
        * HariPerBulan[bulan - 1]
        * performanceRatio
        * hitungKoreksiSuhu(dataRadiasi.SuhuRataRata, panel);

    public double hitungProduksiTahunan() => Enumerable.Range(1, 12).Sum(hitungProduksiBulanan);
}
