using System;
using System.Windows;
using System.Windows.Controls;
<<<<<<< HEAD
using SunCost.Models;
using SunCost.Services;
=======
>>>>>>> b2cc82b51aec8d65bda18eb9fb173a895ebd291c
using SunCost.Views;

namespace SunCost;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>Dipanggil juga oleh halaman lain, mis. tombol "Mulai Simulasi" di Beranda.</summary>
    public void Buka(int halaman) => Nav.SelectedIndex = halaman;

    private void Nav_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Konten is null) return; // terpanggil saat InitializeComponent

        // ponytail: "Simulasi Baru" langsung ke Hasil sampai halaman Input Simulasi dibuat
        Konten.Content = Nav.SelectedIndex switch
        {
            0 => new BerandaPage(),
            1 => new HasilPage(),
            2 => new RiwayatPage(),
<<<<<<< HEAD
            _ => new TextBlock
            {
                Text = "Halaman ini belum dibuat.",
                Margin = new Thickness(40, 36, 40, 36),
                Foreground = (System.Windows.Media.Brush)FindResource("MutedBrush")
            }
        };
    }
}
=======
            _ => new TextBlock { Text = "Halaman ini belum dibuat.", Margin = new Thickness(40, 36, 40, 36),
                                 Foreground = (System.Windows.Media.Brush)FindResource("MutedBrush") }
        };
    }
}
>>>>>>> b2cc82b51aec8d65bda18eb9fb173a895ebd291c
