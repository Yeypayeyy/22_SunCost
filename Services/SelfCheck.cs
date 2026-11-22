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
        PanelSurya panel = new PanelMonokristal("Contoh 550", 550, 21.3, koefisienSuhu: 0.4, luasPerPanel: 2.6, hargaPerUnit: 2_750_000);
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

        // Koefisien dari datasheet sering negatif; hasilnya harus sama.
        var panelNegatif = new PanelMonokristal("Negatif", 550, 21.3, koefisienSuhu: -0.4, luasPerPanel: 2.6, hargaPerUnit: 2_750_000);
        Debug.Assert(Math.Abs(kalkulator.hitungKoreksiSuhu(27, panelNegatif) - 0.892) < 1e-9, "koefisien negatif harus diperlakukan sama");

        // Orientasi: di belahan selatan, atap menghadap utara harus lebih baik daripada menghadap selatan.
        var atapUtara = new Lokasi("U", -7.72, 110.36, 40, orientasi: 0, kemiringan: 15);
        var atapSelatan = new Lokasi("S", -7.72, 110.36, 40, orientasi: 180, kemiringan: 15);
        var atapIdeal = new Lokasi("I", -7.72, 110.36, 40, orientasi: 0, kemiringan: 7.72);
        Debug.Assert(kalkulator.hitungFaktorOrientasi(atapUtara) > kalkulator.hitungFaktorOrientasi(atapSelatan), "utara harus lebih baik dari selatan");
        Debug.Assert(Math.Abs(kalkulator.hitungFaktorOrientasi(atapIdeal) - 1.0) < 1e-9, "posisi ideal harus bernilai 1");

        // Bulan di luar 1..12 harus ditolak.
        bool ditolak = false;
        try { kalkulator.hitungProduksiBulanan(13); } catch (ArgumentOutOfRangeException) { ditolak = true; }
        Debug.Assert(ditolak, "bulan 13 mestinya ditolak");

        CekPolimorfismePanel(kalkulator, panel);

        // Produksi wajar untuk PLTS di Indonesia: 1.100-1.600 kWh per kWp per tahun.
        double produksi = kalkulator.hitungProduksiTahunan();
        double perKwp = produksi / 6.05;
        Debug.Assert(perKwp > 1050 && perKwp < 1600, $"produksi per kWp tidak wajar: {perKwp}");
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
                // BE-7: arus kas kumulatif harus menyilang nol tepat di tahun balik modal.
        var arusKas = analisis.proyeksiArusKas();
        Debug.Assert(arusKas.Count == 26, "arus kas harus berisi 26 nilai (tahun 0..25)");
        Debug.Assert(Math.Abs(arusKas[0] + biaya) < 1e-3, "tahun 0 harus sebesar minus biaya instalasi");
        Debug.Assert(Math.Abs(arusKas[25] - analisis.proyeksiKeuntungan25Tahun()) < 1e-3, "tahun 25 harus sama dengan keuntungan 25 tahun");
        int tahunSilang = arusKas.FindIndex(x => x >= 0);
        Debug.Assert(tahunSilang >= 1 && tahunSilang == (int)Math.Ceiling(payback),
            $"arus kas menyilang nol di tahun {tahunSilang}, payback {payback}");

        var analisisRugi = new AnalisisFinansial(biayaPerWp: 1_000_000);
        analisisRugi.hitungBiayaInstalasi(6050);
        analisisRugi.hitungPenghematanBulanan(produksi / 12, tarif);
        Debug.Assert(double.IsPositiveInfinity(analisisRugi.hitungPaybackPeriod()) && analisisRugi.proyeksiArusKas().All(x => x < 0),
            "skenario yang tidak balik modal harus punya arus kas negatif sepanjang 25 tahun");

        var dampak = new DampakLingkungan();
        Debug.Assert(Math.Abs(dampak.hitungEmisiDihindari(produksi) - produksi * 0.87) < 1e-9, "CO2 meleset");

// Konstanta hasil ADR 0001 dan konstanta yang dibaca halaman Tentang.
Debug.Assert(KalkulatorEnergi.PerformanceRatio == 0.78, "Performance Ratio harus 0,78 (ADR 0001)");
Debug.Assert(DampakLingkungan.FaktorEmisiBawaan == 0.87, "faktor emisi harus 0,87 (ADR 0001)");
Debug.Assert(Math.Abs(new DampakLingkungan().hitungEmisiDihindari(1000) - 870) < 1e-9, "faktor emisi bawaan tidak terpakai");
Debug.Assert(AnalisisFinansial.UmurSistemTahun == 25 && AnalisisFinansial.DegradasiBawaan == 0.5, "konstanta analisis finansial berubah");
Debug.Assert(Lokasi.FaktorPemanfaatan == 0.75 && PanelSurya.SuhuReferensi == 25.0, "konstanta lokasi atau panel berubah");
        Debug.Assert(dampak.setaraPohon(22) == 1 && dampak.setaraPohon(23) == 2, "pembulatan pohon meleset");
                // BE-7: kilometer mobil dan total emisi 25 tahun.
        Debug.Assert(Math.Abs(dampak.setaraKilometerMobil(DampakLingkungan.KgCO2PerKmMobil * 1000) - 1000) < 1e-6,
            "konversi kilometer mobil meleset");
        Debug.Assert(Math.Abs(dampak.totalEmisi25Tahun(1000, 0) - 25 * 870) < 1e-6,
            "tanpa degradasi, total 25 tahun harus 25 kali emisi tahunan");
        double deret = 870 * (1 - Math.Pow(0.995, 25)) / 0.005;   // jumlah deret geometrik, degradasi 0,5%
        Debug.Assert(Math.Abs(dampak.totalEmisi25Tahun(1000, 0.5) - deret) < 1e-6,
            "total emisi 25 tahun dengan degradasi meleset");

        var skenario = new SimulasiSkenario("Rumah Sleman", lokasi, panel, radiasi, new AnalisisFinansial(15_000), tarif);
        skenario.jalankanSimulasi();
        Debug.Assert(Math.Abs(skenario.PaybackPeriod - payback) < 1e-9, "payback skenario beda dengan analisis langsung");
        Debug.Assert(skenario.getRingkasan().StartsWith("Rumah Sleman:"), "ringkasan tidak memuat nama");

        Debug.Assert(skenario.ProduksiBulanan.Count == 12, "skenario harus menyimpan 12 nilai bulanan");
Debug.Assert(Math.Abs(skenario.ProduksiBulanan.Sum() - skenario.ProduksiTahunan) < 1e-6, "jumlah bulanan harus sama dengan produksi tahunan");
Debug.Assert(skenario.KapasitasKwp is { } kapasitasSkenario && Math.Abs(kapasitasSkenario - 6.05) < 1e-9, "kapasitas skenario meleset");
Debug.Assert(skenario.JumlahPanel == 11, "jumlah panel skenario meleset");
Debug.Assert(skenario.BiayaInstalasi is { } biayaSkenario && Math.Abs(biayaSkenario - 6050 * 15_000) < 1e-6, "biaya instalasi skenario meleset (cek satuan Wp/kWp)");
Debug.Assert(skenario.Keuntungan25Tahun > 0, "keuntungan 25 tahun mestinya positif");

        CekPenyimpanan(skenario);
        CekSkemaPenuh();
        CekDataAwal();
        CekInputSkenario(skenario);
        CekTampilanRiwayat();
    }

    /// <summary>
    /// Satu panggilan method yang sama (hitungKoreksiSuhu) harus memberi hasil berbeda
    /// sesuai jenis panel yang sedang dipegang: itu polimorfisme yang dipakai kalkulator.
    /// </summary>
    private static void CekPolimorfismePanel(KalkulatorEnergi kalkulator, PanelSurya mono)
    {
        PanelSurya thinFilm = new PanelThinFilm("Contoh TF 550", 550, 21.3, koefisienSuhu: 0.4,
                                                luasPerPanel: 2.6, hargaPerUnit: 2_750_000);

        Debug.Assert(mono.Jenis == "Monokristalin" && thinFilm.Jenis == "Thin film (a-Si)",
            "jenis panel harus dijawab masing-masing turunan");

        // Spesifikasi identik, hanya teknologi selnya beda: thin film menahan panas lebih baik.
        double koreksiMono = kalkulator.hitungKoreksiSuhu(27, mono);
        double koreksiThin = kalkulator.hitungKoreksiSuhu(27, thinFilm);
        Debug.Assert(koreksiThin > koreksiMono,
            $"thin film mestinya lebih tahan panas: mono {koreksiMono}, thin film {koreksiThin}");

        // Sel 52 C -> annealing (52 - 40) x 0,1% = 1,2% di atas rumus kristalin 0,892.
        Debug.Assert(Math.Abs(koreksiThin - 0.904) < 1e-9, $"koreksi thin film meleset: {koreksiThin}");

        // Di bawah suhu annealing kedua panel kembali sama.
        Debug.Assert(mono.hitungKoreksiSuhu(35).Equals(thinFilm.hitungKoreksiSuhu(35)),
            "di bawah 40 C thin film harus memakai rumus warisan dari class dasar");

        // Method bukan-virtual tetap diwarisi apa adanya oleh kedua turunan.
        Debug.Assert(thinFilm.hitungJumlahPanel(30) == mono.hitungJumlahPanel(30),
            "hitungJumlahPanel diwarisi, tidak boleh beda");
    }

    private static void CekTampilanRiwayat()
    {
        var baris = new Views.RiwayatPage.Baris(new SimulasiSkenario("uji", "Rumah", new DateTime(2026, 9, 6),
            5847, 14_880_000, double.PositiveInfinity, 5100));
        Debug.Assert(baris.Hemat == "Rp 1.240.000", "riwayat harus mengubah penghematan tahunan ke bulanan");
        Debug.Assert(baris.Payback == "Tidak balik modal", "payback tak hingga harus terbaca di riwayat");
        Debug.Assert(baris.Luas == "-" && baris.Kapasitas == "-" && baris.Untung == "-",
            "detail yang belum tersimpan tidak boleh ditebak");
        int perubahan = 0;
        baris.PropertyChanged += (_, _) => perubahan++;
        baris.Dipilih = true;
        baris.Dipilih = true;
        baris.Dipilih = false;
        Debug.Assert(perubahan == 2, "checkbox riwayat harus memperbarui pilihan hanya saat nilainya berubah");
    }

    private static long Hitung(Microsoft.Data.Sqlite.SqliteConnection db, string sql)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    private static void PeriksaSkema(RepositoriSimulasi repo, string label)
    {
        using var db = repo.BukaKoneksi();
        Debug.Assert(Hitung(db, "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN " +
            "('lokasi','panel_surya','tarif_listrik_pln','data_radiasi','radiasi_bulanan','skenario')") == 6,
            $"{label}: enam tabel ERD harus ada");
        Debug.Assert(Hitung(db, "SELECT COUNT(*) FROM pragma_table_info('skenario') WHERE name IN " +
            "('lokasi_id','panel_id','tarif_id','data_radiasi_id','biaya_per_wp','degradasi_tahunan'," +
            "'faktor_emisi_co2','jumlah_panel','kapasitas_wp')") == 9,
            $"{label}: sembilan kolom tambahan skenario harus ada");
        Debug.Assert(Hitung(db, "PRAGMA foreign_keys;") == 1, $"{label}: foreign key harus menyala di setiap koneksi");
    }

    private static void CekSkemaPenuh()
    {
        string lama = Path.Combine(Path.GetTempPath(), $"suncost-skema-lama-{Guid.NewGuid():N}.db");
        string baru = Path.Combine(Path.GetTempPath(), $"suncost-skema-baru-{Guid.NewGuid():N}.db");
        try
        {
            // Database versi lama: hanya tujuh kolom hasil dan satu baris data.
            using (var db = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={lama}"))
            {
                db.Open();
                using var cmd = db.CreateCommand();
                cmd.CommandText = """
                    CREATE TABLE skenario (id TEXT PRIMARY KEY, nama TEXT NOT NULL, tanggal TEXT NOT NULL,
                        produksi_tahunan REAL NOT NULL, penghematan_tahunan REAL NOT NULL,
                        payback_period REAL NOT NULL, emisi_dihindari REAL NOT NULL);
                    INSERT INTO skenario VALUES ('lama', 'Skenario lama', '2026-01-01T00:00:00.0000000', 5000, 7000000, 8.5, 4000);
                    """;
                cmd.ExecuteNonQuery();
            }

            var repoLama = new RepositoriSimulasi(lama);
            PeriksaSkema(repoLama, "database lama");
            var riwayat = repoLama.ambilSemua();
            Debug.Assert(riwayat.Count == 1 && riwayat[0].Nama == "Skenario lama" && riwayat[0].ProduksiTahunan == 5000,
                "riwayat lama harus tetap terbaca setelah migrasi");
            _ = new RepositoriSimulasi(lama);   // migrasi kedua tidak boleh melempar pengecualian

            // Database baru langsung memakai skema penuh.
            PeriksaSkema(new RepositoriSimulasi(baru), "database baru");

            // Foreign key benar-benar ditegakkan.
            using var uji = repoLama.BukaKoneksi();
            bool ditolak = false;
            try
            {
                using var cmd = uji.CreateCommand();
                cmd.CommandText = "INSERT INTO skenario (id, nama, tanggal, produksi_tahunan, penghematan_tahunan, " +
                                  "payback_period, emisi_dihindari, lokasi_id) " +
                                  "VALUES ('x', 'x', '2026-01-01', 1, 1, 1, 1, 'lokasi-tidak-ada');";
                cmd.ExecuteNonQuery();
            }
            catch (Microsoft.Data.Sqlite.SqliteException) { ditolak = true; }
            Debug.Assert(ditolak, "skenario dengan lokasi_id yang tidak ada harus ditolak foreign key");
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(lama)) File.Delete(lama);
            if (File.Exists(baru)) File.Delete(baru);
        }
    }

        private static void CekDataAwal()
    {
        string baru = Path.Combine(Path.GetTempPath(), $"suncost-awal-baru-{Guid.NewGuid():N}.db");
        string lama = Path.Combine(Path.GetTempPath(), $"suncost-awal-lama-{Guid.NewGuid():N}.db");
        try
        {
            // Database baru langsung berisi data master.
            var repo = new RepositoriSimulasi(baru);
            using (var db = repo.BukaKoneksi())
            {
                Debug.Assert(Hitung(db, "SELECT COUNT(*) FROM panel_surya") == 3, "database baru harus punya 3 panel");
                Debug.Assert(Hitung(db, "SELECT COUNT(*) FROM panel_surya WHERE jenis = 'monokristalin'") >= 1, "butuh minimal satu panel monokristalin");
                Debug.Assert(Hitung(db, "SELECT COUNT(*) FROM panel_surya WHERE jenis = 'thin_film'") >= 1, "butuh minimal satu panel thin film");
                Debug.Assert(Hitung(db, "SELECT COUNT(*) FROM tarif_listrik_pln") == 4, "database baru harus punya 4 golongan tarif");
                Debug.Assert(Hitung(db, "SELECT COUNT(*) FROM tarif_listrik_pln WHERE golongan LIKE 'R-1/TR 1.300 VA%' AND tarif_per_kwh = 1444.70") == 1,
                    "tarif R-1/1300 VA harus Rp1.444,70");
            }

            // Dibuka berulang kali tidak menggandakan baris.
            _ = new RepositoriSimulasi(baru);
            using (var db = repo.BukaKoneksi())
            {
                Debug.Assert(Hitung(db, "SELECT COUNT(*) FROM panel_surya") == 3, "panel tergandakan saat database dibuka ulang");
                Debug.Assert(Hitung(db, "SELECT COUNT(*) FROM tarif_listrik_pln") == 4, "tarif tergandakan saat database dibuka ulang");
            }

            // Database dari BE-1 (panel_surya belum punya kolom jenis) dan berisi data pengguna.
            using (var db = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={lama}"))
            {
                db.Open();
                using var cmd = db.CreateCommand();
                cmd.CommandText = """
                    CREATE TABLE panel_surya (id TEXT PRIMARY KEY, merk TEXT NOT NULL,
                        kapasitas_wp REAL NOT NULL CHECK (kapasitas_wp > 0), efisiensi REAL NOT NULL,
                        koefisien_suhu REAL NOT NULL, luas_per_panel REAL NOT NULL CHECK (luas_per_panel > 0),
                        harga_per_unit REAL NOT NULL);
                    INSERT INTO panel_surya VALUES ('panel-mono-420', 'Punyaku', 400, 20, 0.3, 2, 1000000);
                    """;
                cmd.ExecuteNonQuery();
            }
            var repoLama = new RepositoriSimulasi(lama);
            using (var db = repoLama.BukaKoneksi())
            {
                Debug.Assert(Hitung(db, "SELECT COUNT(*) FROM pragma_table_info('panel_surya') WHERE name = 'jenis'") == 1,
                    "kolom jenis harus ditambahkan ke database lama");
                Debug.Assert(Hitung(db, "SELECT COUNT(*) FROM panel_surya WHERE id = 'panel-mono-420' AND merk = 'Punyaku'") == 1,
                    "data pengguna tidak boleh ditimpa data awal");
                Debug.Assert(Hitung(db, "SELECT COUNT(*) FROM panel_surya") == 3, "database lama harus tetap berisi 3 panel");
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(baru)) File.Delete(baru);
            if (File.Exists(lama)) File.Delete(lama);
        }
    }
    private static void CekInputSkenario(SimulasiSkenario s)
    {
        string berkas = Path.Combine(Path.GetTempPath(), $"suncost-input-{Guid.NewGuid():N}.db");
        try
        {
            var repo = new RepositoriSimulasi(berkas);
            var riwayat = new RiwayatSimulasi(repo);
            riwayat.tambahSkenario(s);

            void PeriksaJumlah(long lokasi, long radiasi, long bulanan, string label)
            {
                using var db = repo.BukaKoneksi();
                Debug.Assert(Hitung(db, "SELECT COUNT(*) FROM lokasi") == lokasi, $"{label}: jumlah lokasi");
                Debug.Assert(Hitung(db, "SELECT COUNT(*) FROM data_radiasi") == radiasi, $"{label}: jumlah data radiasi");
                Debug.Assert(Hitung(db, "SELECT COUNT(*) FROM radiasi_bulanan") == bulanan, $"{label}: jumlah radiasi bulanan");
            }
            PeriksaJumlah(1, 1, 12, "setelah simpan");

            // Skenario dirakit utuh dari database.
            var muat = repo.ambilById(s.Id);
            Debug.Assert(muat is not null && muat.Lokasi is not null && muat.Panel is not null
                         && muat.Tarif is not null && muat.DataRadiasi is not null && muat.Analisis is not null,
                "skenario yang dimuat harus punya semua input");
            Debug.Assert(muat!.Lokasi!.Nama == s.Lokasi!.Nama && muat.Lokasi.LuasAtap == s.Lokasi.LuasAtap
                         && muat.Lokasi.Orientasi == s.Lokasi.Orientasi && muat.Lokasi.Kemiringan == s.Lokasi.Kemiringan
                         && muat.Lokasi.Latitude == s.Lokasi.Latitude && muat.Lokasi.Longitude == s.Lokasi.Longitude,
                "lokasi hasil muat berbeda");
            Debug.Assert(muat.Panel!.Merk == s.Panel!.Merk && muat.Panel.Jenis == s.Panel.Jenis
                         && muat.Panel.KapasitasWp == s.Panel.KapasitasWp && muat.Panel.KoefisienSuhu == s.Panel.KoefisienSuhu,
                "panel hasil muat berbeda (cek jenis panel)");
            Debug.Assert(muat.Tarif!.Golongan == s.Tarif!.Golongan && muat.Tarif.getTarifPerKWh() == s.Tarif.getTarifPerKWh(),
                "tarif hasil muat berbeda");
            Debug.Assert(muat.DataRadiasi!.SuhuRataRata == s.DataRadiasi!.SuhuRataRata
                         && Enumerable.Range(1, 12).All(b => muat.DataRadiasi.getPeakSunHours(b) == s.DataRadiasi.getPeakSunHours(b)),
                "data radiasi hasil muat berbeda");

            // Rincian yang dulu tampil "-" di Riwayat sekarang terisi.
            Debug.Assert(muat.JumlahPanel == s.JumlahPanel, "jumlah panel hasil muat berbeda");
            Debug.Assert(muat.KapasitasKwp is { } kap && Math.Abs(kap - s.KapasitasKwp!.Value) < 1e-9, "kapasitas hasil muat berbeda");
            Debug.Assert(muat.BiayaInstalasi is { } biaya && Math.Abs(biaya - s.BiayaInstalasi!.Value) < 1e-3, "biaya instalasi hasil muat berbeda");
            Debug.Assert(muat.Keuntungan25Tahun is { } untung && Math.Abs(untung - s.Keuntungan25Tahun!.Value) < 1e-3, "keuntungan 25 tahun hasil muat berbeda");
            Debug.Assert(muat.ProduksiBulanan.Count == 12 && Math.Abs(muat.ProduksiBulanan.Sum() - s.ProduksiTahunan) < 1e-6,
                "produksi bulanan hasil muat berbeda");

            // Skenario dari database bisa dihitung ulang.
            muat.jalankanSimulasi();
            Debug.Assert(Math.Abs(muat.PaybackPeriod - s.PaybackPeriod) < 1e-6 && Math.Abs(muat.ProduksiTahunan - s.ProduksiTahunan) < 1e-6,
                "hitung ulang skenario dari database menghasilkan angka berbeda");

            // Menyimpan ulang mengganti isinya, tidak menggandakan baris input.
            riwayat.tambahSkenario(s);
            PeriksaJumlah(1, 1, 12, "setelah simpan ulang");

            // Tagihan bulanan ikut tersimpan.
            var denganTagihan = new SimulasiSkenario("Tagihan", s.Lokasi!, s.Panel!, s.DataRadiasi!,
                                                     new AnalisisFinansial(15_000), s.Tarif!) { TagihanBulananKwh = 594 };
            denganTagihan.jalankanSimulasi();
            riwayat.tambahSkenario(denganTagihan);
            Debug.Assert(repo.ambilById(denganTagihan.Id)?.TagihanBulananKwh == 594, "tagihan bulanan tidak tersimpan");
            PeriksaJumlah(2, 2, 24, "dua skenario");

            // Baris tanpa input (gaya lama) tetap bisa disimpan dan dibaca, tanpa rincian karangan.
            var lama = new SimulasiSkenario("lama-uji", "Hanya hasil", DateTime.Now.AddDays(-2), 5000, 7_000_000, 8.5, 4000);
            riwayat.tambahSkenario(lama);
            var lamaMuat = repo.ambilById("lama-uji");
            Debug.Assert(lamaMuat is not null && lamaMuat.Lokasi is null && lamaMuat.JumlahPanel is null && lamaMuat.BiayaInstalasi is null,
                "skenario tanpa input tidak boleh punya rincian");
            PeriksaJumlah(2, 2, 24, "setelah baris lama");

            // Menghapus membersihkan input yang tidak dipakai lagi.
            Debug.Assert(riwayat.hapusSkenario(s.Id) && riwayat.hapusSkenario(denganTagihan.Id) && riwayat.hapusSkenario("lama-uji"),
                "hapus skenario gagal");
            PeriksaJumlah(0, 0, 0, "setelah hapus");
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(berkas)) File.Delete(berkas);
        }
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

                var rugi2 = new SimulasiSkenario("rugi2", "Sama-sama rugi", DateTime.Now, 1, 1, double.PositiveInfinity, 1);
Debug.Assert(riwayat.bandingkanSkenario(dimuat[1], rugi2).Contains("sama-sama tidak balik modal"),
    "dua skenario tanpa balik modal harus dilaporkan sama-sama rugi");

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
