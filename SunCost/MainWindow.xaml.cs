using System.Windows;
using SunCost.Views;

namespace SunCost
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new SimulasiPage());
        }

        private void NavigateToSimulasi(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SimulasiPage());
        }

        private void NavigateToHasil(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new HasilAnalisisPage());
        }

        private void NavigateToRiwayat(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new RiwayatPage());
        }

        private void NavigateToAdmin(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AdminReferensiPage());
        }
    }
}