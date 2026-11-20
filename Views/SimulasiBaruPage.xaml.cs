using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SunCost.Models;

namespace SunCost.Views;

public partial class SimulasiBaruPage : UserControl
{
    public SimulasiBaruPage()
    {
        InitializeComponent();
    }

    // Terima angka dengan koma maupun titik desimal
    private static bool TryAngka(string teks, out double nilai) =>
        double.TryParse(teks.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out nilai);

    private void Hitung_Click(object sender, RoutedEventArgs e)
    {
        if (!TryAngka(TbLat.Text, out double lat) ||
            !TryAngka(TbLon.Text, out double lon) ||
            !TryAngka(TbLuas.Text, out double luas))
        {
            MessageBox.Show("Latitude, longitude, dan luas atap harus berupa angka.", "SunCost");
            return;
        }

        // Tag pada ComboBoxItem menyimpan azimut dalam derajat (0 = utara, 180 = selatan)
        var item = (ComboBoxItem)CbOrientasi.SelectedItem;
        double azimut = double.Parse((string)item.Tag, CultureInfo.InvariantCulture);

        var lokasi = new Lokasi(TbLokasi.Text.Trim(), lat, lon, luas, azimut, SlKemiringan.Value);

        if (!lokasi.validasi())
        {
            MessageBox.Show("Data lokasi belum valid. Pastikan nama terisi, latitude -90 sampai 90, " +
                            "longitude -180 sampai 180, dan luas atap lebih dari 0.", "SunCost");
            return;
        }

        // Sementara baru memakai Lokasi. Hasil lengkap (produksi, payback) menyusul
        // setelah SimulasiSkenario.jalankanSimulasi() dihubungkan ke halaman ini.
        TxtHasil.Text = "Koordinat: " + lokasi.getKoordinat() + ". Luas efektif terpakai panel: "
                        + lokasi.hitungLuasEfektif().ToString("0.0", FormatId.Angka)
                        + " m² (75% dari luas atap).";
    }
}