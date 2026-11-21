using System.Windows;
using System.Windows.Controls;
using SunCost.Views;

namespace SunCost;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>Pindah lewat indeks sidebar, mis. tombol "Mulai Simulasi" di Beranda.</summary>
    public void Buka(int halaman) => Nav.SelectedIndex = halaman;

    /// <summary>
    /// Pindah ke halaman yang sudah dibuat pemanggil, mis. Simulasi Baru membuka Hasil sambil
    /// membawa skenarionya. Item sidebar yang terpilih sengaja tidak diubah.
    /// </summary>
    public void Buka(UserControl halaman) => Konten.Content = halaman;

    private void Nav_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Konten is null) return; // terpanggil saat InitializeComponent

        Konten.Content = Nav.SelectedIndex switch
        {
            1 => new SimulasiPage(),
            2 => new RiwayatPage(),
            3 => new TentangPage(),
            _ => new BerandaPage()
        };
    }
}
