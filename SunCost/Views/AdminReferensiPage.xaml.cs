using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SunCost.Views
{
    public partial class AdminReferensiPage : Page
    {
        public AdminReferensiPage()
        {
            InitializeComponent();
            CmbJenisData.SelectedIndex = 0;
        }

        private void CmbJenisData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridReferensi == null) return;

            var item = (ComboBoxItem)CmbJenisData.SelectedItem;
            switch (item.Content.ToString())
            {
                case "Radiasi Matahari":
                    TampilkanDataRadiasi();
                    break;
                case "Karakteristik Panel":
                    TampilkanDataPanel();
                    break;
                case "Tarif Listrik":
                    TampilkanDataTarif();
                    break;
            }
        }

        private void TampilkanDataRadiasi()
        {
            var data = new List<object>
            {
                new { Kota = "Jakarta", PeakSunHours = 4.8 },
                new { Kota = "Yogyakarta", PeakSunHours = 4.9 }
            };
            GridReferensi.ItemsSource = data;
        }

        private void TampilkanDataPanel()
        {
            var data = new List<object>
            {
                new { Merk = "Panel A", KapasitasWp = 450, EfisiensiPersen = 21.5 },
                new { Merk = "Panel B", KapasitasWp = 400, EfisiensiPersen = 20.1 }
            };
            GridReferensi.ItemsSource = data;
        }

        private void TampilkanDataTarif()
        {
            var data = new List<object>
            {
                new { Golongan = "R1/900VA", HargaPerKwh = 1352 },
                new { Golongan = "R1/1300VA", HargaPerKwh = 1444.7 }
            };
            GridReferensi.ItemsSource = data;
        }

        private void BtnTambah_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Buka form tambah data referensi.");
        }

        private void BtnUbah_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Buka form ubah data referensi yang dipilih.");
        }

        private void BtnHapus_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Data referensi terpilih akan dihapus.");
        }
    }
}