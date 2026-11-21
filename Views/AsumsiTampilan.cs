namespace SunCost.Views;

/// <summary>
/// Konstanta mesin hitung yang perlu ditampilkan di layar tetapi masih <c>private</c> di
/// Services/ dan Models/. Nilainya sama persis dengan yang dipakai mesin saat ini.
/// </summary>
// ponytail: salinan sementara. Hapus berkas ini begitu BE-8 membuka konstanta sebagai public
// (KalkulatorEnergi.PerformanceRatio, AnalisisFinansial.UmurSistemTahun, dst.), lalu ganti
// setiap pemakaian ke konstanta aslinya. Selama berkas ini ada, angkanya bisa basi.
internal static class AsumsiTampilan
{
    public const double PerformanceRatio = 0.80;     // KalkulatorEnergi.performanceRatio
    public const double KenaikanSuhuSel = 25.0;      // KalkulatorEnergi.KenaikanSuhuSel
    public const double SuhuReferensi = 25.0;        // PanelSurya.SuhuReferensi
    public const double DegradasiTahunan = 0.5;      // AnalisisFinansial, nilai bawaan konstruktor
    public const int UmurSistemTahun = 25;           // AnalisisFinansial.UmurSistemTahun
    public const double FaktorPemanfaatan = 0.75;    // Lokasi.FaktorPemanfaatan
    public const double FaktorEmisiCO2 = 0.794;      // DampakLingkungan, nilai bawaan konstruktor
}
