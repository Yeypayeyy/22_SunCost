using System;
using System.Diagnostics;
using System.IO;
using SunCost.Models;

namespace SunCost.Services;

/// <summary>
/// Pemeriksaan cepat yang dijalankan saat aplikasi start pada build DEBUG.
/// Tujuannya menangkap rumus atau skema database yang rusak sebelum jendela terbuka.
/// </summary>
public static class SelfCheck
{
    [Conditional("DEBUG")]
    public static void Jalankan()
    {
        var lokasi = new Lokasi { Nama = "Sleman", Latitude = -7.72, Longitude = 110.36 };
        var s = new Simulasi
        {
            Lokasi = lokasi,
            Atap = new Atap { LuasM2 = 40, Orientasi = Arah.Utara, KemiringanDerajat = 15 },
            Iklim = new DataIklim { PeakSunHours = 4.5, SuhuRataRataC = 27 }
        };
        var kalkulator = new KalkulatorPlts();
        var h = s.Hitung(kalkulator);

        // 40 m2 x 0,75 = 30 m2 efektif, panel 2,6 m2 -> 11 panel x 550 Wp = 6,05 kWp.
        Debug.Assert(h.JumlahPanel == 11, "jumlah panel meleset");
        Debug.Assert(Math.Abs(h.KapasitasKwp - 6.05) < 1e-9, "kapasitas meleset");

        // Koreksi suhu harus memangkas PR: sel 52 C -> rugi 10,8% dari PR dasar 0,80.
        double pr = kalkulator.PerformanceRatioTerkoreksi(s);
        Debug.Assert(pr > 0.70 && pr < 0.72, $"PR terkoreksi di luar dugaan: {pr}");

        // Atap menghadap utara di belahan selatan adalah orientasi optimal.
        Debug.Assert(s.Atap.FaktorOrientasi(lokasi) > 0.97, "faktor orientasi optimal terlalu rendah");
        var terbalik = new Atap { LuasM2 = 40, Orientasi = Arah.Selatan, KemiringanDerajat = 30 };
        Debug.Assert(terbalik.FaktorOrientasi(lokasi) < s.Atap.FaktorOrientasi(lokasi),
            "atap membelakangi matahari mestinya lebih rugi");

        // Produksi wajar untuk PLTS di Indonesia: 1.100-1.600 kWh per kWp per tahun.
        double perKwp = h.EnergiTahunanKwh / h.KapasitasKwp;
        Debug.Assert(perKwp > 1100 && perKwp < 1600, $"produksi per kWp tidak wajar: {perKwp}");

        // Tarif naik lebih cepat daripada panel terdegradasi, jadi payback lebih cepat
        // daripada pembagian sederhana biaya / hemat tahun pertama.
        double paybackSederhana = (double)(h.BiayaInstalasi / h.HematPerTahun);
        Debug.Assert(h.PaybackTahun > 0 && h.PaybackTahun < paybackSederhana,
            $"payback {h.PaybackTahun} mestinya di bawah {paybackSederhana}");

        Debug.Assert(h.KeuntunganBersih25Tahun > 0, "proyeksi 25 tahun mestinya untung");
        Debug.Assert(Math.Abs(h.Co2DihindariKgPerTahun - h.EnergiTahunanKwh * 0.794) < 1e-6, "CO2 meleset");

        CekPenyimpanan(s);
    }

    private static void CekPenyimpanan(Simulasi s)
    {
        string berkas = Path.Combine(Path.GetTempPath(), $"suncost-selfcheck-{Guid.NewGuid():N}.db");
        try
        {
            var repo = new RiwayatRepositorySqlite(berkas);
            int id = repo.Simpan(s);

            var tersimpan = repo.AmbilSemua();
            Debug.Assert(tersimpan.Count == 1, "riwayat mestinya berisi satu baris");
            Debug.Assert(tersimpan[0].Hasil!.HematPerBulan == s.Hasil!.HematPerBulan, "nilai rupiah berubah saat dibaca ulang");
            Debug.Assert(tersimpan[0].Atap.Orientasi == s.Atap.Orientasi, "orientasi berubah saat dibaca ulang");

            repo.Hapus(id);
            Debug.Assert(repo.AmbilSemua().Count == 0, "hapus tidak berpengaruh");
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(berkas)) File.Delete(berkas);
        }
    }
}
