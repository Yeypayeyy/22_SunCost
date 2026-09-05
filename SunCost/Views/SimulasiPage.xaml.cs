using System.Windows;
using System.Windows.Controls;

namespace SunCost.Views
{
    public partial class SimulasiPage : Page
    {
        public SimulasiPage()
        {
            InitializeComponent();
        }

        private void BtnHitung_Click(object sender, RoutedEventArgs e)
        {
            // Sementara masih data dummy, nanti tinggal panggil method backend
            string lokasi = TxtLokasi.Text;
            string luas = TxtLuasAtap.Text;

            TxtHasil.Text = $"Simulasi untuk {lokasi} dengan luas {luas} m² sedang diproses...";
        }
    }
}