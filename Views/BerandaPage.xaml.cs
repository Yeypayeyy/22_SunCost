using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using SunCost.Models;

namespace SunCost.Views;

public partial class BerandaPage : UserControl
{
    public BerandaPage()
    {
        InitializeComponent();

        // Data contoh. Diganti pembacaan dari SQLite saat tabel riwayat sudah ada.
        RiwayatTerakhir.ItemsSource = new List<SimulasiRingkas>
        {
            new() { Lokasi = "Sleman, DIY",       KapasitasKwp = 4.2, PaybackTahun = 6.3, HematPerBulan = 1_240_000 },
            new() { Lokasi = "Bantul, DIY",       KapasitasKwp = 3.0, PaybackTahun = 7.1, HematPerBulan =   880_000 },
            new() { Lokasi = "Depok, Jawa Barat", KapasitasKwp = 5.5, PaybackTahun = 5.8, HematPerBulan = 1_610_000 }
        };
    }

    private void MulaiSimulasi_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Halaman Simulasi Baru belum dibuat.", "SunCost");
    }
}
