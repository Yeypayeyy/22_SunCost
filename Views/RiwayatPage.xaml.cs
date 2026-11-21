using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using SunCost.Models;
using SunCost.Services;
using System.Windows.Input;

namespace SunCost.Views;

public partial class RiwayatPage : UserControl
{
    internal sealed class Baris : INotifyPropertyChanged
    {
        public SimulasiSkenario Skenario { get; }
        public string Nama => Skenario.Nama;
        public string Luas { get; init; } = "-";
        public string Kapasitas { get; init; } = "-";
        public string Judul { get; init; }
        public string Keterangan { get; init; } = "Data riwayat tersimpan";
        public string Untung { get; init; } = "-";
        public string Produksi => Skenario.ProduksiTahunan.ToString("#,##0", FormatId.Angka) + " kWh";
        public string Payback => double.IsFinite(Skenario.PaybackPeriod)
            ? Skenario.PaybackPeriod.ToString("0.0", FormatId.Angka) + " tahun" : "Tidak balik modal";
        public string Hemat => "Rp " + (Skenario.PenghematanTahunan / 12).ToString("#,##0", FormatId.Angka);
        public string Tanggal => Skenario.Tanggal.ToString("d MMM yyyy", FormatId.Angka);
        public bool Terbaik { get; set; }

        private bool dipilih;
        public bool Dipilih
        {
            get => dipilih;
            set
            {
                if (dipilih == value) return;
                dipilih = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Dipilih)));
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        public Baris(SimulasiSkenario skenario)
        {
            Skenario = skenario;
            Judul = skenario.Nama;
        }
    }

    private List<Baris> daftar = new();

    public RiwayatPage()
    {
        InitializeComponent();

        try
        {
            var riwayat = new RiwayatSimulasi(new RepositoriSimulasi());
            daftar = riwayat.muatRiwayat().Select(s => new Baris(s)).ToList();
            Tabel.ItemsSource = daftar.ToArray();

            Status.Text = daftar.Count == 0
                ? ""
                : "Luas atap, kapasitas, dan keuntungan 25 tahun belum tersedia pada data riwayat tersimpan (-).";

            PerbaruiKeadaanRiwayat();
        }
        catch (Exception ex) when (
            ex is SqliteException or IOException or UnauthorizedAccessException or FormatException)
        {
            KartuTabel.Visibility = Visibility.Collapsed;
            Petunjuk.Visibility = Visibility.Collapsed;
            Status.Text = "Riwayat tidak dapat dimuat. Periksa akses penyimpanan lokal.";
        }
    }

    private void PerbaruiKeadaanRiwayat()
    {
        bool kosong = daftar.Count == 0;
        KartuKosong.Visibility = kosong ? Visibility.Visible : Visibility.Collapsed;
        KartuTabel.Visibility = kosong ? Visibility.Collapsed : Visibility.Visible;
        Petunjuk.Visibility = kosong ? Visibility.Collapsed : Visibility.Visible;
        PerbaruiPilihan();
    }

    private void MulaiSimulasi_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow jendela)
            jendela.Buka(1);
    }

    private Baris? targetHapus;
    private Button? tombolPemicuHapus;

    private void MintaHapus_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button tombol || tombol.DataContext is not Baris baris)
            return;

        targetHapus = baris;
        tombolPemicuHapus = tombol;
        PesanHapus.Text = $"Skenario “{baris.Nama}” akan dihapus. " +
                          "Tindakan ini tidak dapat dibatalkan.";

        KontenRiwayat.IsEnabled = false;
        OverlayHapus.Visibility = Visibility.Visible;
        BatalHapus.Focus();
    }

    private void TutupKonfirmasiHapus()
    {
        targetHapus = null;
        OverlayHapus.Visibility = Visibility.Collapsed;
        KontenRiwayat.IsEnabled = true;
        tombolPemicuHapus?.Focus();
        tombolPemicuHapus = null;
    }

    private void BatalHapus_Click(object sender, RoutedEventArgs e) =>
        TutupKonfirmasiHapus();

    private void OverlayHapus_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        e.Handled = true;
        TutupKonfirmasiHapus();
    }

    private void KonfirmasiHapus_Click(object sender, RoutedEventArgs e)
    {
        if (targetHapus is not { } baris)
            return;

        TutupKonfirmasiHapus();

        try
        {
            var riwayat = new RiwayatSimulasi(new RepositoriSimulasi());
            bool terhapus = riwayat.hapusSkenario(baris.Skenario.Id);

            daftar.Remove(baris);
            Tabel.ItemsSource = daftar.ToArray();
            KartuPerbandingan.ItemsSource = null;
            BagianPerbandingan.Visibility = Visibility.Collapsed;
            PerbaruiKeadaanRiwayat();

            Status.Text = terhapus
                ? $"Skenario “{baris.Nama}” berhasil dihapus."
                : $"Skenario “{baris.Nama}” sudah tidak ada di penyimpanan.";
        }
        catch (Exception ex) when (
            ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            Status.Text = "Skenario gagal dihapus. Periksa akses penyimpanan lokal.";
        }
    }

    private void Pilihan_Click(object sender, RoutedEventArgs e)
    {
        var baris = (Baris)((CheckBox)sender).DataContext;
        if (daftar.Count(s => s.Dipilih) > 3)
        {
            baris.Dipilih = false;
            Petunjuk.Text = "Maksimal 3 skenario. Batalkan salah satu pilihan untuk memilih skenario lain.";
            return;
        }
        BagianPerbandingan.Visibility = Visibility.Collapsed;
        PerbaruiPilihan();
    }

    private void PerbaruiPilihan()
    {
        int jumlah = daftar.Count(s => s.Dipilih);
        Bandingkan.Content = $"Bandingkan ({jumlah})";
        Bandingkan.IsEnabled = jumlah is >= 2 and <= 3;
        Petunjuk.Text = jumlah < 2 ? "Pilih minimal 2 skenario untuk dibandingkan."
            : $"{jumlah} skenario dipilih. Klik Bandingkan untuk melihat hasil.";
    }

    private void Bandingkan_Click(object sender, RoutedEventArgs e) => TampilkanPerbandingan();

    private void TampilkanPerbandingan()
    {
        var pilihan = daftar.Where(s => s.Dipilih).ToList();
        if (pilihan.Count is < 2 or > 3) return;
        double tercepat = pilihan.Min(s => s.Skenario.PaybackPeriod);
        foreach (var baris in pilihan)
            baris.Terbaik = double.IsFinite(tercepat) && baris.Skenario.PaybackPeriod == tercepat;
        KartuPerbandingan.ItemsSource = null;
        KartuPerbandingan.ItemsSource = pilihan;
        BagianPerbandingan.Visibility = Visibility.Visible;
        Petunjuk.Text = "Penanda menunjukkan periode balik modal tercepat di antara skenario yang dipilih.";
    }
}
