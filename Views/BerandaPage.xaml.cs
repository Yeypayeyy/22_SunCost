using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using SunCost.Models;
using SunCost.Services;

namespace SunCost.Views;

public partial class BerandaPage : UserControl
{
    /// <summary>Teks siap tampil untuk satu kartu riwayat.</summary>
    private record Kartu(string Nama, string KapasitasTeks, string PaybackTeks, string HematTeks);

    public BerandaPage()
    {
        InitializeComponent();

        try
        {
            var terakhir = new RiwayatSimulasi(new RepositoriSimulasi()).muatRiwayat().Take(3).ToList();
            if (terakhir.Count == 0)
            {
                Kosong.Visibility = Visibility.Visible;
                return;
            }

            RiwayatTerakhir.ItemsSource = terakhir.Select(s => new Kartu(
                s.Nama,
                s.KapasitasKwp is double kapasitas ? kapasitas.ToString("0.##", FormatId.Angka) + " kWp" : "-",
                double.IsFinite(s.PaybackPeriod) ? s.PaybackPeriod.ToString("0.0", FormatId.Angka) + " tahun" : "-",
                "Rp " + (s.PenghematanTahunan / 12).ToString("#,##0", FormatId.Angka))).ToList();
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException or FormatException)
        {
            KosongGambar.Visibility = Visibility.Collapsed; // ilustrasi "belum ada" tidak cocok untuk galat
            KosongJudul.Text = "Riwayat tidak dapat dimuat";
            KosongIsi.Text = "Periksa akses ke berkas penyimpanan lokal, lalu buka kembali halaman ini.";
            Kosong.Visibility = Visibility.Visible;
        }
    }

    private void MulaiSimulasi_Click(object sender, RoutedEventArgs e)
    {
        ((MainWindow)Window.GetWindow(this)).Buka(1);
    }
}
