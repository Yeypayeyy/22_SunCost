using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        var lokasi = new Lokasi("Sleman", -7.72, 110.36, luasAtap: 40, orientasi: 0, kemiringan: 15);
        var panel = new PanelSurya("Contoh 550", 550, 21.3, koefisienSuhu: 0.4, luasPerPanel: 2.6, hargaPerUnit: 2_750_000);
        var radiasi = new DataRadiasi(Enumerable.Repeat(4.5, 12).ToList(), 27, "uji", DateTime.Now);
        var tarif = new TarifListrikPLN("R-1/TR 1.300 VA", 1444.70, new DateTime(2025, 1, 1));

        Debug.Assert(lokasi.validasi(), "lokasi contoh mestinya valid");
        Debug.Assert(!new Lokasi("X", 100, 0, 40, 0, 15).validasi(), "latitude 100 mestinya ditolak");
        Debug.Assert(lokasi.getKoordinat() == "-7.72,110.36", $"format koordinat berubah: {lokasi.getKoordinat()}");

        // 40 m2 x 0,75 = 30 m2 efektif, panel 2,6 m2 -> 11 panel x 550 Wp = 6,05 kWp.
        var kalkulator = new KalkulatorEnergi(lokasi, panel, radiasi);
        Debug.Assert(panel.hitungJumlahPanel(lokasi.hitungLuasEfektif()) == 11, "jumlah panel meleset");
        Debug.Assert(Math.Abs(kalkulator.hitungKapasitasTerpasang(lokasi, panel) - 6.05) < 1e-9, "kapasitas meleset");

        // Udara 27 C -> sel 52 C -> rugi (52 - 25) x 0,4% = 10,8%.
        Debug.Assert(Math.Abs(kalkulator.hitungKoreksiSuhu(27, panel) - 0.892) < 1e-9, "koreksi suhu meleset");

        // Produksi wajar untuk PLTS di Indonesia: 1.100-1.600 kWh per kWp per tahun.
        double produksi = kalkulator.hitungProduksiTahunan();
        double perKwp = produksi / 6.05;
        Debug.Assert(perKwp > 1100 && perKwp < 1600, $"produksi per kWp tidak wajar: {perKwp}");
        Debug.Assert(Math.Abs(Enumerable.Range(1, 12).Sum(kalkulator.hitungProduksiBulanan) - produksi) < 1e-6,
            "produksi tahunan harus sama dengan jumlah bulanan");
        Debug.Assert(radiasi.getRataRataTahunan() == 4.5, "rata-rata tahunan meleset");

        Debug.Assert(tarif.hitungBiaya(100) == 144_470, "biaya listrik meleset");

        // Degradasi mengurangi penghematan tiap tahun, jadi payback lebih lama daripada pembagian sederhana.
        var analisis = new AnalisisFinansial(biayaPerWp: 15_000);
        double biaya = analisis.hitungBiayaInstalasi(6050);
        double hematBulanan = analisis.hitungPenghematanBulanan(produksi / 12, tarif);
        double paybackSederhana = biaya / (hematBulanan * 12);
        double payback = analisis.hitungPaybackPeriod();
        Debug.Assert(payback > paybackSederhana && payback < 25, $"payback {payback} tidak wajar (sederhana {paybackSederhana})");
        Debug.Assert(analisis.proyeksiKeuntungan25Tahun() > 0, "proyeksi 25 tahun mestinya untung");

        var dampak = new DampakLingkungan();
        Debug.Assert(Math.Abs(dampak.hitungEmisiDihindari(produksi) - produksi * 0.794) < 1e-9, "CO2 meleset");
        Debug.Assert(dampak.setaraPohon(22) == 1 && dampak.setaraPohon(23) == 2, "pembulatan pohon meleset");

        var skenario = new SimulasiSkenario("Rumah Sleman", lokasi, panel, radiasi, new AnalisisFinansial(15_000), tarif);
        skenario.jalankanSimulasi();
        Debug.Assert(Math.Abs(skenario.PaybackPeriod - payback) < 1e-9, "payback skenario beda dengan analisis langsung");
        Debug.Assert(skenario.getRingkasan().StartsWith("Rumah Sleman:"), "ringkasan tidak memuat nama");

        CekPenyimpanan(skenario);
    }

    private static void CekPenyimpanan(SimulasiSkenario s)
    {
        string berkas = Path.Combine(Path.GetTempPath(), $"suncost-selfcheck-{Guid.NewGuid():N}.db");
        try
        {
            var riwayat = new RiwayatSimulasi(new RepositoriSimulasi(berkas));
            riwayat.tambahSkenario(s);

            // Skenario yang tidak pernah balik modal menyimpan payback tak hingga.
            var rugi = new SimulasiSkenario("rugi", "Tidak balik modal", DateTime.Now.AddDays(-1), 1, 1,
                double.PositiveInfinity, 1);
            riwayat.tambahSkenario(rugi);

            var dimuat = riwayat.muatRiwayat();
            Debug.Assert(dimuat.Count == 2, "riwayat mestinya berisi dua skenario");
            Debug.Assert(dimuat[0].Id == s.Id && dimuat[0].PenghematanTahunan == s.PenghematanTahunan,
                "skenario berubah saat dibaca ulang");
            Debug.Assert(double.IsPositiveInfinity(dimuat[1].PaybackPeriod), "payback tak hingga hilang di database");
            Debug.Assert(riwayat.bandingkanSkenario(dimuat[1], dimuat[0]).StartsWith("Rumah Sleman balik modal lebih cepat"),
                "perbandingan salah memilih skenario");

            var repo = new RepositoriSimulasi(berkas);
            Debug.Assert(repo.ambilById(s.Id)?.Nama == "Rumah Sleman", "ambilById tidak menemukan skenario");
            Debug.Assert(riwayat.hapusSkenario(s.Id) && repo.ambilById(s.Id) is null, "hapus tidak berpengaruh");
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(berkas)) File.Delete(berkas);
        }
    }
}
