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
    }

    public void TampilkanHasil(SimulasiSkenario skenario, bool dataCadangan)
    {
        ArgumentNullException.ThrowIfNull(skenario);
        AturSkeleton(false);

        Ringkasan.Text = skenario.Nama;
        ProduksiTahunan.Text = skenario.ProduksiTahunan.ToString("#,##0", FormatId.Angka);
        RataBulanan.Text = (skenario.ProduksiTahunan / 12).ToString("#,##0", FormatId.Angka);
        SetaraKonsumsi.Text = "—";

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

        double emisi = skenario.EmisiDihindari;
        int pohon = new DampakLingkungan().setaraPohon(emisi);

        EmisiTahunan.Text =
            $"{emisi.ToString("#,##0", FormatId.Angka)} kg CO₂ dihindari per tahun";
        SetaraPohon.Text =
            $"Setara {pohon.ToString("#,##0", FormatId.Angka)} pohon";
    }

    private static string Rupiah(double? nilai) =>
        nilai is double angka ? "Rp " + angka.ToString("#,##0", FormatId.Angka) : "—";
}