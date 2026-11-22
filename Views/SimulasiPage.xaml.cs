using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Data.Sqlite;
using SunCost.Models;
using SunCost.Services;

namespace SunCost.Views;

public partial class SimulasiPage : UserControl
{
    private record LokasiPilihan(string Nama, double Lat, double Lon)
    {
        public override string ToString() => Nama;
    }

    private record ArahPilihan(string Nama, double Derajat)
    {
        public override string ToString() => Nama;
    }

    private record PanelPilihan(string Label, bool ThinFilm, string Merk, double Wp, double Efisiensi,
                                double KoefisienSuhu, double LuasPerPanel, double HargaPerUnit)
    {
        public override string ToString() => Label;

        // Variabel bertipe PanelSurya: kalkulator memanggil hitungKoreksiSuhu milik jenis yang dipilih.
        public PanelSurya Buat() => ThinFilm
            ? new PanelThinFilm(Merk, Wp, Efisiensi, KoefisienSuhu, LuasPerPanel, HargaPerUnit)
            : new PanelMonokristal(Merk, Wp, Efisiensi, KoefisienSuhu, LuasPerPanel, HargaPerUnit);
    }

    private record TarifPilihan(string Golongan, double PerKWh)
    {
        public override string ToString() =>
            Golongan + " · Rp " + PerKWh.ToString("#,##0.00", FormatId.Angka) + " / kWh";
    }

    private record Baris(string Label, string Nilai);

    // ponytail: data master sementara di kode. Ganti dengan isi tabel panel_surya dan
    // tarif_listrik_pln setelah BE-5. Lokasi bisa ditingkatkan ke Open-Meteo Geocoding API
    // (gratis, tanpa key) kalau daftar kota ini terasa sempit.
    private static readonly LokasiPilihan[] DaftarLokasi =
    {
        new("Sleman, Daerah Istimewa Yogyakarta", -7.7326, 110.3800),
        new("Kota Yogyakarta", -7.7956, 110.3695),
        new("Bantul, Daerah Istimewa Yogyakarta", -7.8881, 110.3289),
        new("Jakarta", -6.2088, 106.8456),
        new("Depok, Jawa Barat", -6.4025, 106.7942),
        new("Bandung, Jawa Barat", -6.9175, 107.6191),
        new("Semarang, Jawa Tengah", -6.9667, 110.4167),
        new("Surabaya, Jawa Timur", -7.2575, 112.7521),
        new("Denpasar, Bali", -8.6500, 115.2167),
        new("Makassar, Sulawesi Selatan", -5.1477, 119.4327),
        new("Medan, Sumatera Utara", 3.5952, 98.6722),
        new("Kupang, Nusa Tenggara Timur", -10.1772, 123.6070),
    };

    private static readonly ArahPilihan[] DaftarArah =
    {
        new("Utara", 0), new("Timur Laut", 45), new("Timur", 90), new("Tenggara", 135),
        new("Selatan", 180), new("Barat Daya", 225), new("Barat", 270), new("Barat Laut", 315),
    };

    private static readonly PanelPilihan[] DaftarPanel =
    {
        new("Monokristalin 420 Wp", false, "Mono 420", 420, 21.5, 0.35, 1.95, 2_300_000),
        new("Monokristalin 550 Wp", false, "Mono 550", 550, 21.3, 0.40, 2.60, 2_750_000),
        new("Thin film (a-Si) 150 Wp", true, "Thin film 150", 150, 10.0, 0.20, 1.43, 1_200_000),
    };

    private static readonly TarifPilihan[] DaftarTarif =
    {
        new("R-1 / 900 VA", 1352.00),
        new("R-1 / 1300 VA", 1444.70),
        new("R-1 / 2200 VA", 1444.70),
        new("R-2 / 3500–5500 VA", 1699.53),
        new("R-3 / 6600 VA ke atas", 1699.53),
    };

    /// <summary>Tanggal berlaku tarif di daftar di atas (penyesuaian tarif PLN triwulan I 2025).</summary>
    private static readonly DateTime TarifBerlaku = new(2025, 1, 1);

    private readonly LayananCuacaAPI cuaca = new();
    private DataRadiasi? radiasi;
    private Task<DataRadiasi>? pengambilan;
    private int urutanAmbil;

    private readonly HashSet<TextBox> disentuh = new();
    private bool sudahDicoba;
    private bool sibuk;
    private bool siap;

    public SimulasiPage()
    {
        InitializeComponent();

        LokasiBox.ItemsSource = DaftarLokasi;
        ArahBox.ItemsSource = DaftarArah;
        PanelBox.ItemsSource = DaftarPanel;
        TarifBox.ItemsSource = DaftarTarif;
        LokasiBox.SelectedIndex = 0;
        ArahBox.SelectedIndex = 0;
        PanelBox.SelectedIndex = 0;
        TarifBox.SelectedIndex = 1;

        foreach (var box in new[] { NamaBox, LuasBox, BiayaBox, TagihanBox, TarifManualBox })
        {
            box.TextChanged += (_, _) => Berubah();
            box.LostFocus += (s, _) => { disentuh.Add((TextBox)s); Validasi(); };
        }
        LokasiBox.SelectionChanged += (_, _) => { PerbaruiKoordinat(); AmbilRadiasi(); };
        ArahBox.SelectionChanged += (_, _) => Berubah();
        PanelBox.SelectionChanged += (_, _) => Berubah();
        TarifBox.SelectionChanged += (_, _) => Berubah();
        KemiringanSlider.ValueChanged += (_, _) => Berubah();
        TarifManualCek.Checked += (_, _) => GantiSumberTarif();
        TarifManualCek.Unchecked += (_, _) => GantiSumberTarif();

        siap = true;
        PerbaruiKoordinat();
        AmbilRadiasi();
        Berubah();
    }

    // ---------------------------------------------------------------- pembacaan isian

    /// <summary>Angka berformat Indonesia ("21.000", "1.699,53"); null bila kosong atau bukan angka.</summary>
    private static double? Angka(TextBox box) =>
        double.TryParse(box.Text.Trim(), NumberStyles.Number, FormatId.Angka, out double v) ? v : null;

    private LokasiPilihan TempatDipilih => (LokasiPilihan)LokasiBox.SelectedItem;
    private PanelPilihan PanelDipilih => (PanelPilihan)PanelBox.SelectedItem;

    private Lokasi BuatLokasi(double luas) => new(
        NamaBox.Text.Trim(), TempatDipilih.Lat, TempatDipilih.Lon, luas,
        ((ArahPilihan)ArahBox.SelectedItem).Derajat, Math.Round(KemiringanSlider.Value));

    private TarifListrikPLN BuatTarif()
    {
        if (TarifManualCek.IsChecked == true)
            return new TarifListrikPLN("Tarif manual", Angka(TarifManualBox) ?? 0, DateTime.Today);
        var t = (TarifPilihan)TarifBox.SelectedItem;
        return new TarifListrikPLN(t.Golongan, t.PerKWh, TarifBerlaku);
    }

    /// <summary>Pemakaian bulanan pengguna (kWh). Belum punya penerima, lihat catatan kontrak B1.</summary>
    public double? TagihanBulananKwh => Angka(TagihanBox);

    // ---------------------------------------------------------------- data radiasi

    private void PerbaruiKoordinat()
    {
        var t = TempatDipilih;
        string Derajat(double v) => v.ToString("0.0000", FormatId.Angka).Replace('-', '−') + "°";
        KoordinatTeks.Text = Derajat(t.Lat) + " " + Derajat(t.Lon) + " · data radiasi dari Open-Meteo";
    }

    /// <summary>
    /// Mengambil radiasi lokasi terpilih di latar belakang supaya estimasi PSH dan koreksi suhu
    /// sudah siap sebelum tombol Hitung ditekan, tanpa membekukan jendela.
    /// </summary>
    private async void AmbilRadiasi()
    {
        if (!siap) return;
        int nomor = ++urutanAmbil;
        radiasi = null;
        var t = TempatDipilih;
        var lokasi = new Lokasi(t.Nama, t.Lat, t.Lon, 1, 0, 0);

        // ponytail: membungkus method sinkron dengan Task.Run. Ganti ke ambilDataRadiasiAsync setelah BE-3.
        pengambilan = Task.Run(() => cuaca.ambilDataRadiasi(lokasi));
        Berubah();
        try
        {
            var hasil = await pengambilan;
            if (nomor != urutanAmbil) return; // pengguna sudah ganti lokasi
            radiasi = hasil;
        }
        catch (Exception) when (nomor == urutanAmbil)
        {
            pengambilan = null; // tombol Hitung akan mencoba lagi
        }
        Berubah();
    }

    // ---------------------------------------------------------------- estimasi langsung

    private void Berubah()
    {
        if (!siap) return;
        KemiringanTeks.Text = Math.Round(KemiringanSlider.Value) + "°";
        if (disentuh.Count > 0 || sudahDicoba) Validasi();
        PerbaruiEstimasi();
    }

    private void PerbaruiEstimasi()
    {
        var spek = PanelDipilih;
        PanelSurya panel = spek.Buat();
        double? luas = Angka(LuasBox);
        double? biayaPerWp = Angka(BiayaBox);

        int? jumlah = null;
        double? kWp = null;
        if (luas > 0)
        {
            var lokasi = BuatLokasi(luas.Value);
            jumlah = panel.hitungJumlahPanel(lokasi.hitungLuasEfektif());
            kWp = panel.hitungKapasitasTotal(jumlah.Value) / 1000.0;
        }

        KapasitasTeks.Text = kWp is null ? "-" : kWp.Value.ToString("0.0", FormatId.Angka) + " kWp";

        string psh, koreksi;
        if (radiasi is null)
        {
            psh = pengambilan is { IsCompleted: false } ? "memuat…" : "-";
            koreksi = "-";
        }
        else
        {
            psh = radiasi.getRataRataTahunan().ToString("0.0", FormatId.Angka) + " jam/hari"
                  + (radiasi.AdalahCadangan ? " · cadangan" : "");
            var kalkulator = new KalkulatorEnergi(BuatLokasi(1), panel, radiasi);
            double faktor = kalkulator.hitungKoreksiSuhu(radiasi.SuhuRataRata, panel);
            double suhuSel = radiasi.SuhuRataRata + AsumsiTampilan.KenaikanSuhuSel;
            koreksi = "−" + ((1 - faktor) * 100).ToString("0.0", FormatId.Angka) + "% (sel "
                      + suhuSel.ToString("0", FormatId.Angka) + " °C)";
        }

        string biaya = kWp is null || biayaPerWp is not > 0
            ? "-"
            : "Rp " + new AnalisisFinansial(biayaPerWp.Value).hitungBiayaInstalasi(kWp.Value * 1000)
                          .ToString("#,##0", FormatId.Angka);

        Estimasi.ItemsSource = new[]
        {
            new Baris("Jenis panel", panel.Jenis),
            new Baris("Jumlah panel", jumlah is null ? "-" : $"{jumlah} unit × {spek.Wp:0} Wp"),
            new Baris("Luas terpakai", jumlah is null ? "-" :
                (jumlah.Value * spek.LuasPerPanel).ToString("0.0", FormatId.Angka) + " m² dari "
                + luas!.Value.ToString("0.#", FormatId.Angka) + " m²"),
            new Baris("Peak Sun Hours", psh),
            new Baris("Performance Ratio", AsumsiTampilan.PerformanceRatio.ToString("0.00", FormatId.Angka)),
            new Baris("Koreksi suhu", koreksi),
            new Baris("Estimasi biaya instalasi", biaya),
        };
    }

    // ---------------------------------------------------------------- A3 · validasi

    /// <summary>
    /// Menandai isian yang salah. Galat hanya ditampilkan untuk isian yang sudah disentuh,
    /// atau untuk semua isian setelah tombol Hitung pernah ditekan. Mengembalikan jumlah galat tampil.
    /// </summary>
    private int Validasi()
    {
        int salah = 0;

        void Cek(TextBox box, TextBlock pesan, string? galat)
        {
            bool tampil = galat is not null && (sudahDicoba || disentuh.Contains(box));
            if (tampil) box.BorderBrush = (Brush)FindResource("DangerBrush");
            else box.ClearValue(Control.BorderBrushProperty); // kembali ke gaya, termasuk warna fokus
            pesan.Text = galat ?? "";
            pesan.Visibility = tampil ? Visibility.Visible : Visibility.Collapsed;
            if (tampil) salah++;
        }

        bool manual = TarifManualCek.IsChecked == true;
        Cek(NamaBox, NamaGalat, string.IsNullOrWhiteSpace(NamaBox.Text) ? "Nama skenario wajib diisi." : null);
        Cek(LuasBox, LuasGalat, Angka(LuasBox) is > 0 ? null : "Luas atap harus lebih dari 0 m².");
        Cek(BiayaBox, BiayaGalat, Angka(BiayaBox) is > 0 ? null : "Biaya per Wp harus diisi.");
        Cek(TagihanBox, TagihanGalat,
            string.IsNullOrWhiteSpace(TagihanBox.Text) || Angka(TagihanBox) is > 0 ? null : "Tagihan harus lebih dari 0 kWh.");
        Cek(TarifManualBox, TarifManualGalat,
            !manual || Angka(TarifManualBox) is > 0 ? null : "Tarif per kWh harus lebih dari 0.");

        TampilkanBanner(salah > 0 ? $"{salah} isian belum benar. Perbaiki dulu sebelum menghitung." : null);
        Hitung.IsEnabled = salah == 0 && !sibuk;
        return salah;
    }

    private void TampilkanBanner(string? pesan)
    {
        BannerTeks.Text = pesan ?? "";
        Banner.Visibility = pesan is null ? Visibility.Collapsed : Visibility.Visible;
    }

    // ---------------------------------------------------------------- A4 · tarif manual

    private void GantiSumberTarif()
    {
        bool manual = TarifManualCek.IsChecked == true;
        TarifBox.IsEnabled = !manual;
        TarifManualPanel.Visibility = manual ? Visibility.Visible : Visibility.Collapsed;
        if (manual) TarifManualBox.Focus();
        else disentuh.Remove(TarifManualBox);
        Berubah();
    }

    // ---------------------------------------------------------------- hitung

    private async void Hitung_Click(object sender, RoutedEventArgs e)
    {
        sudahDicoba = true;
        if (Validasi() > 0) return;

        var lokasi = BuatLokasi(Angka(LuasBox)!.Value);
        if (!lokasi.validasi())
        {
            TampilkanBanner("Data lokasi tidak valid. Periksa luas, orientasi, dan kemiringan atap.");
            return;
        }

        sibuk = true;
        Hitung.IsEnabled = false;
        Hitung.Content = "Menghitung…";
        var jendela = Window.GetWindow(this) as MainWindow;
        var halamanHasil = new HasilPage();
        double? konsumsiBulananKwh = TagihanBulananKwh;

        try
        {
            halamanHasil.TampilkanMemuat(lokasi.Nama);
            jendela?.Buka(halamanHasil);

            var data = radiasi ??
                await (pengambilan ?? Task.Run(() => cuaca.ambilDataRadiasi(lokasi)));

            var skenario = new SimulasiSkenario(
                NamaBox.Text.Trim(),
                lokasi,
                PanelDipilih.Buat(),
                data,
                new AnalisisFinansial(Angka(BiayaBox)!.Value),
                BuatTarif())
            {
                KonsumsiBulananKwh = konsumsiBulananKwh
            };

            skenario.jalankanSimulasi();
            new RiwayatSimulasi(new RepositoriSimulasi()).tambahSkenario(skenario);

            halamanHasil.TampilkanHasil(skenario, data.AdalahCadangan);
        }
        catch (Exception ex) when (ex is SqliteException or IOException
                                       or UnauthorizedAccessException or InvalidOperationException)
        {
            jendela?.Buka(this);
            TampilkanBanner("Simulasi gagal: " + ex.Message);
        }
        finally
        {
            sibuk = false;
            Hitung.Content = "Hitung Kelayakan";
            Hitung.IsEnabled = true;
        }
    }
}
