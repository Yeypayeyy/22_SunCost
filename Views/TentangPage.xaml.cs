using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using SunCost.Models;

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
            new Baris("Performance Ratio (PR)", AsumsiTampilan.PerformanceRatio.ToString("0.00", FormatId.Angka), tinta),
            new Baris("Suhu sel saat penyinaran penuh",
                      "suhu udara + " + AsumsiTampilan.KenaikanSuhuSel.ToString("0", FormatId.Angka) + " °C", tinta),
            new Baris("Koreksi suhu", "berlaku di atas " + AsumsiTampilan.SuhuReferensi.ToString("0", FormatId.Angka) + " °C", tinta),
            new Baris("Degradasi panel", "−" + Persen(AsumsiTampilan.DegradasiTahunan) + "% per tahun", tinta),
            new Baris("Faktor pemanfaatan atap", (AsumsiTampilan.FaktorPemanfaatan * 100).ToString("0", FormatId.Angka) + "% dari luas total", tinta),
            new Baris("Umur sistem", AsumsiTampilan.UmurSistemTahun + " tahun", tinta),
        };

        Sumber.ItemsSource = new[]
        {
            new Baris("Radiasi & suhu", "Open-Meteo Archive", tinta),
            new Baris("Tarif listrik", "PLN / Permen ESDM", tinta),
            new Baris("Faktor emisi CO₂", AsumsiTampilan.FaktorEmisiCO2.ToString("0.000", FormatId.Angka) + " kg/kWh", tinta),
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
