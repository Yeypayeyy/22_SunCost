using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SunCost.Models;

namespace SunCost.Views;

public partial class HasilPage : UserControl
{
    private record Bar(string Bulan, double Tinggi, Brush WarnaBatang, Brush WarnaTeks, FontWeight Bobot, string Tooltip);
    private record Item(string Nama, string Nilai, string Keterangan);

    private const double TinggiMaks = 204; // tinggi batang tertinggi di mockup Figma

    public HasilPage()
    {
        InitializeComponent();

        // Data contoh dari mockup. Diganti hasil KalkulatorEnergi.hitungProduksiBulanan() saat halaman Simulasi Baru jadi.
        string[] bulan = { "Jan", "Feb", "Mar", "Apr", "Mei", "Jun", "Jul", "Ags", "Sep", "Okt", "Nov", "Des" };
        double[] produksi = { 456, 435, 472, 483, 499, 504, 542, 547, 523, 491, 451, 444 };
        double tagihanBulanan = 594; // kWh, konsumsi rata-rata dari tagihan pengguna

        double total = produksi.Sum(), maks = produksi.Max();
        Ringkasan.Text = "Sleman, DIY  ·  atap 32 m², orientasi utara, kemiringan 15°  ·  4,2 kWp";
        ProduksiTahunan.Text = total.ToString("#,##0", FormatId.Angka);
        RataBulanan.Text = (total / 12).ToString("#,##0", FormatId.Angka);
        SetaraKonsumsi.Text = (total / 12 / tagihanBulanan).ToString("0%", FormatId.Angka);

        var aksen = (Brush)FindResource("AccentBrush");
        var batang = (Brush)FindResource("BarBrush");
        var tinta = (Brush)FindResource("InkBrush");
        var redup = (Brush)FindResource("MutedBrush");

        Batang.ItemsSource = bulan.Select((b, i) =>
        {
            bool puncak = produksi[i] == maks;
            return new Bar(b, produksi[i] / maks * TinggiMaks,
                puncak ? aksen : batang, puncak ? tinta : redup,
                puncak ? FontWeights.SemiBold : FontWeights.Normal,
                produksi[i].ToString("#,##0", FormatId.Angka) + " kWh");
        }).ToList();

        Asumsi.ItemsSource = new[]
        {
            new Item("Peak Sun Hours (PSH)", "4,6 jam/hari", "Rata-rata radiasi harian Open-Meteo, 10 tahun terakhir"),
            new Item("Performance Ratio (PR)", "0,78", "Rugi inverter, kabel, kotoran, dan bayangan"),
            new Item("Koreksi suhu", "−0,4% / °C", "Untuk setiap °C di atas 25 °C — signifikan di iklim tropis"),
            new Item("Degradasi panel", "−0,5% / tahun", "Penurunan output panel sepanjang umur pakai"),
        };
    }

    private void TabFinansial_Checked(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Tab Finansial & Lingkungan belum dibuat.", "SunCost");
        TabEnergi.IsChecked = true;
    }
}
