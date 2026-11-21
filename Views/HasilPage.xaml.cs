using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SunCost.Models;

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

        Ringkasan.Text = "Menunggu hasil simulasi…";
        ProduksiTahunan.Text = "—";
        RataBulanan.Text = "—";
        SetaraKonsumsi.Text = "—";
    }

    public HasilPage(SimulasiSkenario skenario) : this()
    {
        TampilkanHasil(skenario, dataCadangan: false);
    }

    public void TampilkanHasil(SimulasiSkenario skenario, bool dataCadangan)
    {
        ArgumentNullException.ThrowIfNull(skenario);

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

    private void TabFinansial_Checked(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Tab Finansial & Lingkungan belum dibuat.", "SunCost");
        TabEnergi.IsChecked = true;
    }
}