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

namespace SunCost.Views;

public partial class RiwayatPage : UserControl
{
    internal sealed class Baris : INotifyPropertyChanged
    {
        public SimulasiSkenario Skenario { get; }
        public string Nama => Skenario.Nama;
        public string Luas { get; init; } = "—";
        public string Kapasitas { get; init; } = "—";
        public string Judul { get; init; }
        public string Keterangan { get; init; } = "Data riwayat tersimpan";
        public string Untung { get; init; } = "—";
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
            var riwayat = new RiwayatSimulasi(new RepositoriSimulasi()).muatRiwayat();
            daftar = riwayat.Select(s => new Baris(s)).ToList();
            if (daftar.Count == 0)
            {
                daftar = Contoh();
                Status.Text = "Belum ada riwayat tersimpan. Menampilkan data contoh Figma; contoh ini tidak disimpan.";
                daftar[0].Dipilih = daftar[1].Dipilih = true;
            }
            else
            {
                // shortcut: riwayat lama tidak menyimpan detail atap dan proyeksi, tampilkan setelah data tersebut dipersistenkan.
                Status.Text = "Luas atap, kapasitas, dan keuntungan 25 tahun belum tersedia pada data riwayat tersimpan (—).";
            }
            Tabel.ItemsSource = daftar;
            PerbaruiPilihan();
            if (daftar.Count(s => s.Dipilih) >= 2) TampilkanPerbandingan();
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException or FormatException)
        {
            Status.Text = "Riwayat tidak dapat dimuat. Periksa akses dan berkas penyimpanan lokal, lalu buka kembali halaman ini.";
            Petunjuk.Text = "Data tersimpan tidak diubah.";
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

    private static List<Baris> Contoh() => new()
    {
        new(new SimulasiSkenario("contoh-1", "Sleman, DIY", new DateTime(2026, 9, 6), 5847, 1_240_000 * 12, 6.3, 5100))
        {
            Luas = "32 m²", Kapasitas = "4,2 kWp", Judul = "Atap menghadap utara",
            Keterangan = "Orientasi 0° · kemiringan 15°", Untung = "Rp 261.000.000"
        },
        new(new SimulasiSkenario("contoh-2", "Sleman, DIY — atap barat", new DateTime(2026, 9, 6), 4738, 1_005_000 * 12, 7.8, 4122))
        {
            Luas = "32 m²", Kapasitas = "4,2 kWp", Judul = "Atap menghadap barat",
            Keterangan = "Orientasi 270° · kemiringan 15°", Untung = "Rp 199.000.000"
        },
        new(new SimulasiSkenario("contoh-3", "Bantul, DIY", new DateTime(2026, 9, 2), 4300, 880_000 * 12, 7.1, 3414))
        { Luas = "24 m²", Kapasitas = "3,0 kWp", Keterangan = "Data contoh Figma" },
        new(new SimulasiSkenario("contoh-4", "Depok, Jawa Barat", new DateTime(2026, 8, 28), 7900, 1_610_000 * 12, 5.8, 6272))
        { Luas = "42 m²", Kapasitas = "5,5 kWp", Keterangan = "Data contoh Figma" },
        new(new SimulasiSkenario("contoh-5", "Sleman — skenario hemat", new DateTime(2026, 8, 24), 3300, 710_000 * 12, 6.9, 2620))
        { Luas = "18 m²", Kapasitas = "2,4 kWp", Keterangan = "Data contoh Figma" }
    };
}
