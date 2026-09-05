using System.Windows;
using System.Windows.Controls;

namespace SunCost.Views
{
    public partial class HasilAnalisisPage : Page
    {
        public HasilAnalisisPage()
        {
            InitializeComponent();
        }

        private void BtnSimpan_Click(object sender, RoutedEventArgs e)
        {
            // Sementara dummy, nanti backend yang simpan ke database lokal
            MessageBox.Show("Simulasi berhasil disimpan ke riwayat.");
        }
    }
}