using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SunCost.Models;

namespace SunCost.Views;

public partial class BerandaPage : UserControl
{
    /// <summary>Teks siap tampil untuk satu kartu riwayat.</summary>
    private record Kartu(string Nama, string ProduksiTeks, string PaybackTeks, string HematTeks);

    public BerandaPage()
    {
        InitializeComponent();

        // Data contoh. Diganti RiwayatSimulasi.muatRiwayat() saat halaman Simulasi Baru sudah bisa menyimpan.
        var contoh = new List<SimulasiSkenario>
        {
            new("1", "Sleman, DIY",       DateTime.Today, 6_100, 8_800_000, 6.3, 4_843),
            new("2", "Bantul, DIY",       DateTime.Today, 4_300, 6_200_000, 7.1, 3_414),
            new("3", "Depok, Jawa Barat", DateTime.Today, 7_900, 11_400_000, 5.8, 6_272)
        };
        RiwayatTerakhir.ItemsSource = contoh.Select(s => new Kartu(
            s.Nama,
            s.ProduksiTahunan.ToString("#,##0", FormatId.Angka) + " kWh",
            double.IsInfinity(s.PaybackPeriod) ? "-" : s.PaybackPeriod.ToString("0.0", FormatId.Angka) + " tahun",
            "Rp " + s.PenghematanTahunan.ToString("#,##0", FormatId.Angka))).ToList();
    }

    private void MulaiSimulasi_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Halaman Simulasi Baru belum dibuat.", "SunCost");
    }
}
