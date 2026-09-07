using System;
using SunCost.Models;

namespace SunCost.Services;

/// <summary>
/// Inti perhitungan SunCost: kapasitas terpasang, produksi energi, kelayakan finansial,
/// dan emisi yang dihindari. Semua asumsi dibuka sebagai properti agar bisa dikalibrasi
/// terhadap data lapangan, bukan dipatok di dalam rumus.
/// </summary>
public class KalkulatorPlts
{
    /// <summary>Performance ratio dasar: rugi inverter, kabel, kotoran, dan mismatch. Umumnya 0,75-0,85.</summary>
    public double PerformanceRatio { get; set; } = 0.80;

    /// <summary>Kenaikan suhu sel di atas suhu udara saat penyinaran penuh (pendekatan NOCT).</summary>
    public double KenaikanSuhuSelC { get; set; } = 25.0;

    /// <summary>Faktor emisi jaringan Jawa-Bali, kg CO2 per kWh.</summary>
    public double FaktorEmisiKgPerKwh { get; set; } = 0.794;

    /// <summary>Penurunan produksi panel per tahun akibat degradasi modul.</summary>
    public double DegradasiTahunanPersen { get; set; } = 0.5;

    public int UmurSistemTahun { get; set; } = 25;

    public HasilSimulasi Hitung(Simulasi s)
    {
        var hasil = new HasilSimulasi();

        // Kapasitas dibatasi jumlah panel yang benar-benar muat di atap, bukan luas mentahnya.
        hasil.JumlahPanel = s.Panel.JumlahPanelMuat(s.Atap.LuasEfektifM2);
        hasil.KapasitasKwp = hasil.JumlahPanel * s.Panel.DayaWp / 1000.0;

        hasil.EnergiTahunanKwh = EnergiTahunan(s, hasil.KapasitasKwp);

        hasil.BiayaInstalasi = (decimal)(hasil.JumlahPanel * s.Panel.DayaWp) * s.Panel.HargaPerWp;
        hasil.HematPerBulan = (decimal)(hasil.EnergiTahunanKwh / 12) * s.Tarif.RpPerKwh;
        hasil.PaybackTahun = Payback(hasil.BiayaInstalasi, hasil.EnergiTahunanKwh, s.Tarif);
        hasil.KeuntunganBersih25Tahun = KeuntunganBersih(hasil.BiayaInstalasi, hasil.EnergiTahunanKwh, s.Tarif);

        hasil.Co2DihindariKgPerTahun = hasil.EnergiTahunanKwh * FaktorEmisiKgPerKwh;
        return hasil;
    }

    /// <summary>Energi tahunan = kWp x PSH x PR terkoreksi suhu x faktor orientasi x 365.</summary>
    public double EnergiTahunan(Simulasi s, double kapasitasKwp) =>
        kapasitasKwp
        * s.Iklim.PeakSunHours
        * PerformanceRatioTerkoreksi(s)
        * s.Atap.FaktorOrientasi(s.Lokasi)
        * 365;

    /// <summary>Performance ratio setelah dikurangi rugi panas: 0,4% per derajat di atas 25 C.</summary>
    public double PerformanceRatioTerkoreksi(Simulasi s)
    {
        double suhuSel = s.Iklim.SuhuRataRataC + KenaikanSuhuSelC;
        double rugiPanas = Math.Max(0, suhuSel - 25) * s.Panel.KoefisienSuhuPersen / 100;
        return PerformanceRatio * Math.Max(0.3, 1 - rugiPanas);
    }

    /// <summary>
    /// Tahun saat penghematan kumulatif menutup biaya instalasi. Tarif naik tiap tahun,
    /// produksi turun tiap tahun, jadi dihitung per tahun, bukan dengan pembagian sederhana.
    /// </summary>
    public double Payback(decimal biaya, double energiTahunPertama, TarifListrik tarif)
    {
        decimal kumulatif = 0;
        for (int tahun = 0; tahun < UmurSistemTahun; tahun++)
        {
            decimal hematTahunIni = HematTahunKe(tahun, energiTahunPertama, tarif);
            if (hematTahunIni <= 0) break;

            if (kumulatif + hematTahunIni >= biaya)
                return tahun + (double)((biaya - kumulatif) / hematTahunIni);

            kumulatif += hematTahunIni;
        }
        return double.PositiveInfinity; // tidak balik modal selama umur sistem
    }

    public decimal KeuntunganBersih(decimal biaya, double energiTahunPertama, TarifListrik tarif)
    {
        decimal total = 0;
        for (int tahun = 0; tahun < UmurSistemTahun; tahun++)
            total += HematTahunKe(tahun, energiTahunPertama, tarif);
        return total - biaya;
    }

    private decimal HematTahunKe(int tahun, double energiTahunPertama, TarifListrik tarif)
    {
        double energi = energiTahunPertama * Math.Pow(1 - DegradasiTahunanPersen / 100, tahun);
        decimal tarifTahunIni = tarif.RpPerKwh * (decimal)Math.Pow(1 + tarif.InflasiTarifPersen / 100, tahun);
        return (decimal)energi * tarifTahunIni;
    }
}
