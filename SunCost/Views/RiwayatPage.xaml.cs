using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace SunCost.Views
{
    public class SimulasiRiwayat
    {
        public bool IsDipilih { get; set; }
        public string NamaSkenario { get; set; }
        public string Tanggal { get; set; }
        public double Kapasitas { get; set; }
        public double Roi { get; set; }
    }

    public partial class RiwayatPage : Page
    {
        public ObservableCollection<SimulasiRiwayat> DaftarRiwayat { get; set; }

        public RiwayatPage()
        {
            InitializeComponent();

            // Data dummy sementara, nanti diganti ambil dari database lokal
            DaftarRiwayat = new ObservableCollection<SimulasiRiwayat>
            {
                new SimulasiRiwayat { NamaSkenario = "Rumah Jakarta 4.5 kWp", Tanggal = "01/09/2026", Kapasitas = 4.5, Roi = 4.4 },
                new SimulasiRiwayat { NamaSkenario = "Rumah Yogyakarta 3.2 kWp", Tanggal = "03/09/2026", Kapasitas = 3.2, Roi = 5.1 }
            };

            GridRiwayat.ItemsSource = DaftarRiwayat;
        }

        private void BtnBandingkan_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Fitur perbandingan akan menampilkan tabel side-by-side dari skenario yang dicentang.");
        }
    }
}