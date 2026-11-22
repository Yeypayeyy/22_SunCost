using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using SunCost.Models;
using SunCost.Services;

namespace SunCost.Views;

public partial class TentangPage : UserControl
{
    private record Anggota(string Nama, string Nim, string Peran);
    private record Baris(string Label, string Nilai, Brush Warna);

    public TentangPage()
    {
        InitializeComponent();

        var tinta = (Brush)FindResource("InkBrush");
        var hijau = (Brush)FindResource("GreenBrush");
        string Persen(double v) => v.ToString("0.0##", FormatId.Angka);

        Tim.ItemsSource = new[]
        {
            new Anggota("Muhammad Farrel Al Ghazy", "24/540589/TK/60022", "Software Architect"),
            new Anggota("Mayravivania Syahda Charisa", "24/538308/TK/59701", "Frontend Developer"),
            new Anggota("Sri Wahyuni Arista", "23/521971/TK/57593", "Backend Developer"),
        };

        Konstanta.ItemsSource = new[]
        {
            new Baris("Performance Ratio (PR)", KalkulatorEnergi.PerformanceRatio.ToString("0.00", FormatId.Angka), tinta),
            new Baris("Suhu sel saat penyinaran penuh",
                      "suhu udara + " + KalkulatorEnergi.KenaikanSuhuSel.ToString("0", FormatId.Angka) + " °C", tinta),
            new Baris("Koreksi suhu", "berlaku di atas " + PanelSurya.SuhuReferensi.ToString("0", FormatId.Angka) + " °C", tinta),
            new Baris("Degradasi panel", "−" + Persen(AnalisisFinansial.DegradasiBawaan) + "% per tahun", tinta),
            new Baris("Faktor pemanfaatan atap", (Lokasi.FaktorPemanfaatan * 100).ToString("0", FormatId.Angka) + "% dari luas total", tinta),
            new Baris("Umur sistem", AnalisisFinansial.UmurSistemTahun + " tahun", tinta),
        };

        Sumber.ItemsSource = new[]
        {
            new Baris("Radiasi & suhu", "Open-Meteo Archive", tinta),
            new Baris("Tarif listrik", "PLN / Permen ESDM", tinta),
            new Baris("Faktor emisi CO₂", DampakLingkungan.FaktorEmisiBawaan.ToString("0.00", FormatId.Angka) + " kg/kWh", tinta),
            new Baris("Penyimpanan", "SQLite lokal", tinta),
        };

        var versi = Assembly.GetExecutingAssembly().GetName().Version;
        Versi.ItemsSource = new[]
        {
            new Baris("Versi", versi is null ? "1.0 (MVP)" : $"{versi.Major}.{versi.Minor} (MVP)", hijau),
            new Baris("Runtime", ".NET 8 · WPF", tinta),
            new Baris("Basis data", "SQLite", tinta),
            new Baris("Lisensi", "Proyek akademik", tinta),
        };
    }
}
