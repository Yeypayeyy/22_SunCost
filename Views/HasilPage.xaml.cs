using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SunCost.Models;
using SunCost.Services;

namespace SunCost.Views;

public partial class HasilPage : UserControl
{
    private record Bar(
        string Bulan,
        double Tinggi,
        Brush WarnaBatang,
        Brush WarnaTeks,
        FontWeight Bobot,
        string Tooltip);

    private static readonly string[] Bulan =
    {
        "Jan", "Feb", "Mar", "Apr", "Mei", "Jun",
        "Jul", "Ags", "Sep", "Okt", "Nov", "Des"
    };

    private const double TinggiMaks = 204;

    public HasilPage()
    {
        InitializeComponent();
        SkeletonAsumsi.ItemsSource = new[]
        {
            "Peak Sun Hours (PSH)",
            "Performance Ratio (PR)",
            "Koreksi suhu"
        };
        TampilkanMemuat();
    }

    public HasilPage(SimulasiSkenario skenario) : this()
    {
        TampilkanHasil(skenario, dataCadangan: false);
    }

    public void TampilkanMemuat(string? lokasi = null)
    {
        TabEnergi.IsChecked = true;
        Ringkasan.Text = "Memuat data radiasi";
        PesanMemuat.Text = string.IsNullOrWhiteSpace(lokasi)
            ? "Mengambil data radiasi Open-Meteo. Biasanya 5–20 detik."
            : $"Mengambil data radiasi Open-Meteo untuk {lokasi}. Biasanya 5–20 detik.";

        AturSkeleton(true);

        var track = (Brush)FindResource("TrackBrush");
        var redup = (Brush)FindResource("MutedBrush");

        Batang.ItemsSource = Bulan.Select(nama =>
            new Bar(nama, 124, track, redup, FontWeights.Normal, "Memuat…")).ToArray();

        Asumsi.ItemsSource = null;
        BannerCadangan.Visibility = Visibility.Collapsed;
    }

    private void AturSkeleton(bool memuat)
    {
        var skeleton = memuat ? Visibility.Visible : Visibility.Collapsed;
        var angka = memuat ? Visibility.Hidden : Visibility.Visible;

        BannerMemuat.Visibility = skeleton;
        SkeletonTahunan.Visibility = skeleton;
        SkeletonBulanan.Visibility = skeleton;
        SkeletonKonsumsi.Visibility = skeleton;

        ProduksiTahunan.Visibility = angka;
        RataBulanan.Visibility = angka;
        SetaraKonsumsi.Visibility = angka;
        TabFinansial.IsEnabled = !memuat;

        SkeletonAsumsi.Visibility = skeleton;
        Asumsi.Visibility = memuat ? Visibility.Collapsed : Visibility.Visible;
    }

    public void TampilkanHasil(SimulasiSkenario skenario, bool dataCadangan)
    {
        ArgumentNullException.ThrowIfNull(skenario);
        AturSkeleton(false);

        var lokasi = skenario.Lokasi;
        double rataBulanan = skenario.ProduksiTahunan / 12;

        Ringkasan.Text = lokasi is null
            ? skenario.Nama
            : string.Format(FormatId.Angka,
                "{0} · atap {1:0.##} m² · orientasi {2:0.##}° · kemiringan {3:0.##}°",
                lokasi.Nama, lokasi.LuasAtap, lokasi.Orientasi, lokasi.Kemiringan);

        ProduksiTahunan.Text = skenario.ProduksiTahunan.ToString("#,##0", FormatId.Angka);
        RataBulanan.Text = rataBulanan.ToString("#,##0", FormatId.Angka);
        SetaraKonsumsi.Text = skenario.TagihanBulananKwh is double konsumsi && konsumsi > 0
            ? (rataBulanan / konsumsi).ToString("0%", FormatId.Angka)
            : "—";

        if (skenario.KapasitasKwp is double kapasitas)
        {
            Ringkasan.Text += $" · {kapasitas.ToString("0.##", FormatId.Angka)} kWp";
        }

        Batang.ItemsSource = null;

        if (skenario.ProduksiBulanan.Count == Bulan.Length)
        {
            TampilkanBatang(skenario.ProduksiBulanan.ToArray());
        }
        TampilkanFinansial(skenario);
        TampilkanAsumsi(skenario, dataCadangan);
        TampilkanBannerCadangan(dataCadangan);
    }

    private void TampilkanBatang(double[] produksi)
    {
        double maksimum = produksi.Max();
        var aksen = (Brush)FindResource("AccentBrush");
        var batang = (Brush)FindResource("BarBrush");
        var tinta = (Brush)FindResource("InkBrush");
        var redup = (Brush)FindResource("MutedBrush");

        Batang.ItemsSource = Bulan.Select((nama, indeks) =>
        {
            double nilai = produksi[indeks];
            bool puncak = maksimum > 0 && nilai == maksimum;
            double tinggi = maksimum > 0 ? nilai / maksimum * TinggiMaks : 0;

            return new Bar(
                nama,
                tinggi,
                puncak ? aksen : batang,
                puncak ? tinta : redup,
                puncak ? FontWeights.SemiBold : FontWeights.Normal,
                nilai.ToString("#,##0", FormatId.Angka) + " kWh");
        }).ToArray();
    }

    private record MetrikFinansial(string Nama, string Nilai);

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        // Checked bisa terpanggil sebelum semua kontrol XAML dibuat.
        if (PanelFinansial is null || KpiEnergi is null || DetailEnergi is null)
            return;

        bool finansialAktif = TabFinansial.IsChecked == true;

        PanelFinansial.Visibility = finansialAktif
            ? Visibility.Visible
            : Visibility.Collapsed;

        KpiEnergi.Visibility = DetailEnergi.Visibility = finansialAktif
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void TampilkanFinansial(SimulasiSkenario skenario)
    {
        string balikModal = double.IsFinite(skenario.PaybackPeriod)
            ? skenario.PaybackPeriod.ToString("0.0", FormatId.Angka) + " tahun"
            : "Tidak balik modal dalam 25 tahun";

        KpiFinansial.ItemsSource = new[]
        {
        new MetrikFinansial("Biaya instalasi", Rupiah(skenario.BiayaInstalasi)),
        new MetrikFinansial("Hemat per bulan", Rupiah(skenario.PenghematanTahunan / 12)),
        new MetrikFinansial("Balik modal", balikModal),
        new MetrikFinansial("Keuntungan bersih 25 tahun", Rupiah(skenario.Keuntungan25Tahun))
        };

        var dampak = new DampakLingkungan();
        double emisi = skenario.EmisiDihindari;

        EmisiTahunan.Text =
            $"{emisi.ToString("#,##0", FormatId.Angka)} kg CO₂ dihindari per tahun";
        SetaraPohon.Text =
            $"Setara {dampak.setaraPohon(emisi).ToString("#,##0", FormatId.Angka)} pohon";
        SetaraKmMobil.Text =
            $"Setara {dampak.setaraKilometerMobil(emisi).ToString("#,##0", FormatId.Angka)} km perjalanan mobil";

        TotalEmisi25Tahun.Text = skenario.Analisis?.DegradasiTahunan is double degradasi
            ? $"{dampak.totalEmisi25Tahun(skenario.ProduksiTahunan, degradasi).ToString("#,##0", FormatId.Angka)} kg CO₂ dihindari dalam 25 tahun"
            : "Total emisi dihindari dalam 25 tahun: —";

        TampilkanGrafikArusKas(skenario);
    }

    private void TampilkanGrafikArusKas(SimulasiSkenario skenario)
    {
        var arusKas = skenario.BiayaInstalasi.HasValue
            ? skenario.Analisis?.proyeksiArusKas()
            : null;

        PanelGrafikArusKas.Visibility = Visibility.Collapsed;
        StatusArusKas.Visibility = Visibility.Visible;
        TitikBalikModal.Visibility = Visibility.Collapsed;

        if (arusKas is null || arusKas.Count != AnalisisFinansial.UmurSistemTahun + 1 ||
            arusKas.Any(nilai => !double.IsFinite(nilai)))
            return;

        double minimum = Math.Min(0, arusKas.Min());
        double maksimum = Math.Max(0, arusKas.Max());
        double rentang = maksimum - minimum;

        double PosisiX(double tahun) => 12 + tahun / (arusKas.Count - 1) * 536;
        double PosisiY(double nilai) => rentang > 0
            ? 12 + (maksimum - nilai) / rentang * 176
            : 100;

        GarisArusKas.Points = new PointCollection(
            arusKas.Select((nilai, tahun) =>
                new Point(PosisiX(tahun), PosisiY(nilai))));

        double posisiNol = PosisiY(0);
        GarisNol.Y1 = posisiNol;
        GarisNol.Y2 = posisiNol;

        double balikModal = skenario.PaybackPeriod;
        if (double.IsFinite(balikModal) &&
            balikModal >= 0 && balikModal <= AnalisisFinansial.UmurSistemTahun)
        {
            Canvas.SetLeft(TitikBalikModal, PosisiX(balikModal) - TitikBalikModal.Width / 2);
            Canvas.SetTop(TitikBalikModal, posisiNol - TitikBalikModal.Height / 2);
            TitikBalikModal.ToolTip =
                $"Balik modal: {balikModal.ToString("0.0", FormatId.Angka)} tahun";
            TitikBalikModal.Visibility = Visibility.Visible;
        }

        RentangArusKas.Text = $"{Rupiah(minimum)} hingga {Rupiah(maksimum)} · garis putus-putus = Rp 0";
        StatusArusKas.Visibility = Visibility.Collapsed;
        PanelGrafikArusKas.Visibility = Visibility.Visible;
    }

    private record AsumsiTampilan(string Nama, string Nilai, string Keterangan);

    private void TampilkanAsumsi(SimulasiSkenario skenario, bool dataCadangan)
    {
        Asumsi.ItemsSource = null;

        if (skenario.Lokasi is not { } lokasi || skenario.Panel is not { } panel ||
            skenario.DataRadiasi is not { } radiasi)
            return;

        var kalkulator = new KalkulatorEnergi(lokasi, panel, radiasi);
        double faktorSuhu = kalkulator.hitungKoreksiSuhu(radiasi.SuhuRataRata, panel);

        Asumsi.ItemsSource = new[]
        {
        new AsumsiTampilan(
            "Peak Sun Hours (PSH)",
            radiasi.getRataRataTahunan().ToString("0.0", FormatId.Angka) + " jam/hari",
            dataCadangan
                ? "Cadangan rata-rata Indonesia — API tidak terjangkau"
                : radiasi.SumberData),
        new AsumsiTampilan(
            "Performance Ratio (PR)",
            KalkulatorEnergi.PerformanceRatio.ToString("0.00", FormatId.Angka),
            "Faktor efisiensi sistem yang dipakai kalkulator"),
        new AsumsiTampilan(
            "Koreksi suhu",
            faktorSuhu.ToString("0.0%", FormatId.Angka),
            "Persentase daya panel setelah koreksi suhu")
    };
    }

    private void TampilkanBannerCadangan(bool dataCadangan)
    {
        BannerCadangan.Visibility = dataCadangan
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (dataCadangan)
            Ringkasan.Text += " · data cadangan";
    }

    private static string Rupiah(double? nilai) =>
        nilai is double angka ? "Rp " + angka.ToString("#,##0", FormatId.Angka) : "—";
}