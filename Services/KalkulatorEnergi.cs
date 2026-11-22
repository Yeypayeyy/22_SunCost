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

        /// <summary>Performance Ratio: rugi inverter, kabel, kotoran, dan mismatch. Nilai mengikuti ADR 0001.</summary>
    public const double PerformanceRatio = 0.78;

    // Objek yang dipakai perhitungan (relasi "uses" pada class diagram).
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

    /// <summary>
    /// Faktor pengali (0..1) akibat arah dan sudut atap dibanding posisi ideal.
    /// Posisi ideal: menghadap ekuator (utara di belahan bumi selatan, selatan di utara)
    /// dengan kemiringan sebesar lintang.
    /// </summary>
    public double hitungFaktorOrientasi(Lokasi lokasi)
    {
        double azIdeal = lokasi.Latitude < 0 ? 0 : 180;
        double tiltIdeal = Math.Max(Math.Abs(lokasi.Latitude), 5);

        double t1 = tiltIdeal * Math.PI / 180;
        double t2 = lokasi.Kemiringan * Math.PI / 180;
        double dAz = (lokasi.Orientasi - azIdeal) * Math.PI / 180;

        double cosSudut = Math.Cos(t1) * Math.Cos(t2)
                        + Math.Sin(t1) * Math.Sin(t2) * Math.Cos(dAz);
        return Math.Clamp(cosSudut, 0, 1);
    }

    /// <summary>Energi bulan ke-1..12 (kWh) = kWp x PSH x hari x PR x koreksi suhu x faktor orientasi.</summary>
    public double hitungProduksiBulanan(int bulan)
    {
        if (bulan < 1 || bulan > 12)
            throw new ArgumentOutOfRangeException(nameof(bulan), "Bulan harus 1 sampai 12.");

        return hitungKapasitasTerpasang(lokasi, panel)
            * dataRadiasi.getPeakSunHours(bulan)
            * HariPerBulan[bulan - 1]
            * PerformanceRatio
            * hitungKoreksiSuhu(dataRadiasi.SuhuRataRata, panel)
            * hitungFaktorOrientasi(lokasi);
    }

    public double hitungProduksiTahunan() => Enumerable.Range(1, 12).Sum(hitungProduksiBulanan);
}