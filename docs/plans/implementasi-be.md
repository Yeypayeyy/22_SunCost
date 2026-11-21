# Rencana Implementasi Backend

Pemilik: **Sri Wahyuni Arista**

Lingkup: `Models/`, `Services/`, skema database. Tidak menyentuh `Views/`, `App.xaml`,
atau `MainWindow.*`: itu milik jalur frontend, lihat [implementasi-fe.md](implementasi-fe.md).

Keadaan sekarang: mesin perhitungan sudah lengkap dan terbukti lewat `SelfCheck.cs`, tetapi
database hanya menyimpan hasil akhir dan layanan cuaca tidak pernah dipanggil dari alur
pengguna mana pun.

## BE-1 · Migrasi skema ke ERD penuh

Berkas: `Services/RepositoriSimulasi.cs`

Tabel `skenario` sekarang hanya punya tujuh kolom hasil. DDL lengkap untuk enam tabel
**sudah ditulis** di [docs/ERD.md](../ERD.md) bagian "DDL (SQLite)", tinggal dieksekusi:
`lokasi`, `panel_surya`, `tarif_listrik_pln`, `data_radiasi`, `radiasi_bulanan`, `skenario`.

Dua hal yang mudah terlewat:

- SQLite mematikan foreign key secara bawaan. Jalankan `PRAGMA foreign_keys = ON;` pada
  **setiap** koneksi yang dibuka, bukan sekali saat membuat tabel.
- Database pengguna yang sudah ada tidak boleh rusak. Pakai `CREATE TABLE IF NOT EXISTS`
  untuk tabel baru dan `ALTER TABLE skenario ADD COLUMN` untuk kolom tambahan. Kolom FK pada
  `skenario` dibuat nullable supaya baris lama tetap terbaca.

**Selesai bila:** membuka aplikasi dengan `suncost.db` versi lama tidak membuang pengecualian,
dan riwayat lama masih tampil.

## BE-2 · Menyimpan input, bukan hanya hasil

Berkas: `Services/RepositoriSimulasi.cs`, `Models/SimulasiSkenario.cs`

Inilah penyebab kolom Luas atap, Kapasitas, dan Untung 25 tahun menampilkan `-` di halaman
Riwayat: datanya memang tidak pernah disimpan.

Tambahkan penyimpanan untuk `Lokasi`, `PanelSurya`, `TarifListrikPLN`, dan `DataRadiasi`,
lalu rakit ulang `SimulasiSkenario` secara utuh di `ambilSemua()` sehingga skenario dari
riwayat bisa dihitung ulang dan dibandingkan.

Setelah ini, konstruktor "skenario dari riwayat" di `SimulasiSkenario` yang membuat semua
asosiasi bernilai `null` sebaiknya dihapus, berikut pengecualian
`"Skenario dari riwayat tidak dapat dihitung ulang."`.

**Dua tambahan pada `SimulasiSkenario` yang dibutuhkan B1**: tanpa ini `HasilPage` tidak bisa
menghitung produksi bulanan dari skenario yang diterimanya:

```csharp
// asosiasi sekarang private tanpa getter; HasilPage butuh untuk KalkulatorEnergi
public Lokasi Lokasi { get; }
public PanelSurya Panel { get; }
public DataRadiasi DataRadiasi { get; }
public TarifListrikPLN Tarif { get; }

// isian "Tagihan listrik bulanan" di form belum punya tempat; dipakai KPI "Setara konsumsi"
public double? TagihanBulananKwh { get; init; }
```

Simpan juga `TagihanBulananKwh` di database (kolom nullable pada `skenario`).

Catatan performa: `ambilById()` sekarang memindai seluruh riwayat
(`ambilSemua().Find(...)`). Sekalian ganti menjadi query `WHERE id = $id`.

**Selesai bila:** halaman Riwayat tidak lagi menampilkan `-`, dan
`RiwayatSimulasi.bandingkanSkenario()` bekerja pada dua skenario yang dimuat dari database.

## BE-3 · Layanan cuaca menjadi asinkron

Berkas: `Services/LayananCuacaAPI.cs`

```csharp
// sekarang: memblokir thread pemanggil sampai 20 detik
string json = httpClient.GetStringAsync(url).GetAwaiter().GetResult();

// menjadi
public async Task<DataRadiasi> ambilDataRadiasiAsync(Lokasi lokasi)
```

Dipanggil dari tombol Hitung, bentuk sekarang membekukan jendela selama pengambilan data.
Komentar di kode menyebut "tidak kembali ke thread UI", itu tidak berlaku lagi begitu
`SimulasiPage` memanggilnya.

`HttpClient` juga dibuat baru pada setiap instance. Jadikan `static readonly` supaya tidak
menghabiskan socket.

Kembalikan juga penanda apakah hasil berasal dari API atau dari `DataCadangan()`, karena
frontend wajib menampilkannya (lihat B4). Nilai `DataRadiasi.sumberData` sudah membedakan
keduanya, cukup ekspos sebagai properti publik.

**Selesai bila:** jendela tetap bisa digerakkan selama pengambilan data berlangsung.

## BE-4 · Cache data radiasi

Berkas: `Services/LayananCuacaAPI.cs`, `Services/RepositoriSimulasi.cs`

Bila sudah ada baris `data_radiasi` untuk koordinat yang sama dan `tanggal_ambil` kurang dari
30 hari, baca dari database alih-alih memanggil API.

Ini yang membuat klaim README "aplikasi tetap berjalan tanpa koneksi internet" menjadi benar,
sekaligus memangkas waktu tunggu simulasi kedua di lokasi yang sama menjadi nol.

**Selesai bila:** menjalankan dua simulasi di lokasi yang sama hanya menghasilkan satu panggilan
jaringan.

## BE-5 · Data master awal

Berkas: `Services/RepositoriSimulasi.cs`

Dropdown "Jenis panel" dan "Golongan tarif PLN" pada halaman Simulasi Baru membutuhkan isi.
Sisipkan data awal saat database pertama kali dibuat:

- `panel_surya`: tiga sampai empat panel yang beredar di pasar, sertakan minimal satu
  monokristalin dan satu thin film supaya kedua rumus koreksi suhu dapat dicoba pengguna.
- `tarif_listrik_pln`: golongan R-1/900 VA, R-1/1300 VA, R-1/2200 VA, dan R-2.

Gunakan `INSERT OR IGNORE` dengan id tetap supaya tidak menggandakan baris setiap kali
aplikasi dibuka.

**Selesai bila:** database baru langsung punya isi dropdown tanpa pengguna menambah apa pun.

## BE-6 · Dokumen pengujian Modul 10

Berkas: `docs/specs/pengujian.md` *(baru)*

`Services/SelfCheck.cs` sudah memuat sekitar tiga puluh pernyataan uji yang menutup validasi
lokasi, jumlah panel, kapasitas, koreksi suhu, polimorfisme panel, kewajaran produksi,
perhitungan payback, emisi, dan penyimpanan. Itu bahan mentahnya, tinggal diangkat menjadi
tabel kasus uji berisi ID, skenario, input, hasil diharapkan, hasil aktual, dan status.

> Berkas Modul 10 belum ada di repositori ini, jadi **format tabelnya harus disamakan sendiri**
> dengan yang diminta modul. Jangan memakai format di atas kalau modul meminta bentuk lain.

**Selesai bila:** setiap pernyataan di `SelfCheck.cs` punya satu baris di dokumen pengujian.

## BE-7 · Tiga method untuk tab Finansial

Berkas: `Services/AnalisisFinansial.cs`, `Services/DampakLingkungan.cs`

Desain frame `4 · Hasil (Finansial)` meminta data yang belum ada method-nya. Frontend tidak
boleh menghitungnya sendiri di view.

```csharp
// AnalisisFinansial: 26 nilai (tahun 0..25) untuk grafik arus kas kumulatif.
// Tahun 0 bernilai negatif sebesar biaya instalasi; titik potong nol = periode balik modal.
public List<double> proyeksiArusKas()

// DampakLingkungan: emisi (kg CO2) menjadi kilometer mobil bensin yang tidak ditempuh.
public double setaraKilometerMobil(double emisi)

// DampakLingkungan: total CO2 selama 25 tahun, ikut memperhitungkan degradasi panel.
public double totalEmisi25Tahun(double produksiTahunan, double degradasiTahunan)
```

`proyeksiArusKas()` harus konsisten dengan `hitungPaybackPeriod()` yang sudah ada: keduanya
memakai degradasi tahunan yang sama, sehingga titik potong nol pada grafik wajib sama dengan
angka periode balik modal pada KPI.

Tambahkan pernyataan uji untuk ketiganya di `SelfCheck.cs`.

**Selesai bila:** `proyeksiArusKas()` menyilang nol pada tahun yang sama dengan
`hitungPaybackPeriod()`, dibuktikan oleh sebuah `Debug.Assert`.

## BE-8 · Menyelaraskan konstanta

Berkas: `Services/KalkulatorEnergi.cs`, `Services/DampakLingkungan.cs`, `Models/Lokasi.cs`

Terapkan nilai yang diputuskan pada [ADR 0001](../decisions/0001-asumsi-perhitungan.md)
(sudah diterima 2026-11-21):

| Berkas | Dari | Menjadi |
|---|---|---|
| `Services/KalkulatorEnergi.cs` → `performanceRatio` | `0.80` | `0.78` |
| `Services/DampakLingkungan.cs` → `faktorEmisiCO2` | `0.794` | `0.87` |

Sekalian buka konstanta yang dibutuhkan halaman Tentang (A7) sebagai `public const`, supaya
frontend membacanya alih-alih menyalin angkanya:

| Berkas | Konstanta |
|---|---|
| `Services/KalkulatorEnergi.cs` | `PerformanceRatio` |
| `Services/AnalisisFinansial.cs` | `UmurSistemTahun`, `DegradasiBawaan` |
| `Services/DampakLingkungan.cs` | `FaktorEmisiBawaan` |
| `Models/Lokasi.cs` | `FaktorPemanfaatan` |
| `Models/PanelSurya.cs` | `SuhuReferensi` |

Dua `Debug.Assert` di `SelfCheck.cs` akan gagal setelah perubahan angka, pemeriksaan
kewajaran produksi per kWp dan pemeriksaan emisi yang membandingkan dengan `0.794`.
Perbarui keduanya.

**Selesai bila:** nilai di kode, di teks Figma, dan di halaman Tentang sama persis.

---

## Urutan pengerjaan

```
BE-1 ─ BE-2 ─ BE-4
BE-3 ─────────┘
BE-5  ← setelah BE-1
BE-7  ← bebas, tidak bergantung pada yang lain
BE-6  ← terus diperbarui seiring tugas lain selesai
BE-8  ← menunggu ADR 0001
```

BE-1 adalah jalur kritis: A6 dan B1 di sisi frontend menunggu hasilnya. Kerjakan lebih dulu.

## Aturan kerja

- Satu tugas = satu commit, pesan berawalan kode tugas: `feat(BE-1): migrasi skema ERD penuh`.
- Jangan menyentuh `Views/`, `App.xaml`, atau `MainWindow.*`.
- Jangan menambah dependensi. Proyek ini hanya memakai `Microsoft.Data.Sqlite`.
- Setiap perubahan rumus atau skema wajib disertai pernyataan uji baru di `SelfCheck.cs`.
- Query selalu memakai parameter (`$id`), tidak pernah merangkai string SQL.
