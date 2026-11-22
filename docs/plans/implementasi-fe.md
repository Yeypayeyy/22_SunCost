# Rencana Implementasi Frontend

Pekerjaan frontend dibagi dua jalur yang **tidak menyentuh berkas yang sama**, supaya bisa
dikerjakan bersamaan tanpa konflik merge.

| Jalur | Pemilik | Lingkup |
|---|---|---|
| **FE-A** | Muhammad Farrel Al Ghazy | input, kerangka aplikasi, token, tentang |
| **FE-B** | Mayravivania Syahda Charisa | hasil, riwayat |

Acuan desain: [docs/specs/layar-ui.md](../specs/layar-ui.md).

## Status

| Tugas | Status | Catatan |
|---|---|---|
| A1–A7 (Farrel) | ✅ di `main` | |
| B1 · HasilPage terima skenario | ✅ di `main` | |
| B2 · Tab Finansial & Lingkungan | ✅ di `main` | |
| B3 · State memuat | ✅ di `main` | |
| B4 · State data cadangan | ✅ di `main` | |
| B5 · Hapus skenario | ✅ di `main` | |
| B6 · Riwayat kosong | ✅ di `main` | |

Basis data tetap SQLite ([ADR 0002](../decisions/0002-database-sqlite.md)). Kode PostgreSQL dari
branch `538308` sudah dikeluarkan saat branch itu dirapikan, dan tidak perlu ditambahkan lagi.

## Pembagian kepemilikan berkas

Hanya pemilik yang boleh mengubah berkas di kolomnya.

| Berkas | Pemilik |
|---|---|
| `App.xaml` | FE-A |
| `MainWindow.xaml` / `MainWindow.xaml.cs` | FE-A |
| `Views/SimulasiPage.xaml` + `.cs` *(baru)* | FE-A |
| `Views/BerandaPage.xaml` + `.cs` | FE-A |
| `Views/HasilPage.xaml` + `.cs` | FE-B |
| `Views/RiwayatPage.xaml` + `.cs` | FE-B |
| `Views/TentangPage.xaml` + `.cs` *(baru)* | FE-A |
| `Assets/`: logo `suncost.ico`, `ilustrasi-kosong.png` | FE-A (dipakai bersama) |

Halaman Tentang semula ada di jalur FE-B dan dipindah ke FE-A untuk menyeimbangkan beban:
tab Finansial (B2) di jalur FE-B berisi grafik arus kas yang harus digambar manual karena
proyek tidak memakai pustaka grafik. Pemindahan ini tidak menimbulkan berkas bersama, setiap
berkas tetap dimiliki tepat satu jalur.

## Kontrak antar-jalur

Disepakati di muka supaya kedua jalur bisa jalan paralel. Jangan diubah sepihak.

```csharp
// Views/HasilPage.xaml.cs: dikerjakan FE-B (B1), dipanggil FE-A (A2)
public HasilPage(SimulasiSkenario skenario)

// Views/HasilPage.xaml.cs: konstruktor state memuat, dipanggil FE-A saat menunggu API
public HasilPage()                      // tanpa argumen = state "memuat data"
public void TampilkanHasil(SimulasiSkenario skenario, bool dataCadangan)

// MainWindow.xaml.cs: dikerjakan FE-A (A5), dipakai kedua jalur
public void Buka(int halaman)
public void Buka(UserControl halaman)   // navigasi membawa objek
```

**B1 dikerjakan paling awal di jalur FE-B.** Selama B1 belum ada, A2 tidak bisa dikompilasi.

---

# FE-A: Farrel

## A1 · Token dan gaya bersama

Berkas: `App.xaml`

Tambahkan dua brush yang dipakai state error dan dialog hapus:

```xml
<SolidColorBrush x:Key="DangerBrush"     Color="#9E3B2F"/>
<SolidColorBrush x:Key="DangerSoftBrush" Color="#F9EAE7"/>
<SolidColorBrush x:Key="InputBrush"      Color="#FAF9F7"/>
```

Skeleton memakai `TrackBrush` yang sudah ada (`#ECE9E4`), jangan tambah brush baru untuk itu.

Tambahkan juga gaya yang belum ada dan dibutuhkan kedua jalur:

- `FieldInput`: `TextBox` radius 8, `InputBrush`, stroke 1px `BorderBrush`, padding `14,11`;
  teks satuan (m², Rp/Wp) lewat properti `Tag`. Saat tidak valid cukup set `BorderBrush` ke
  `DangerBrush`, dan `ClearValue` saat valid lagi supaya warna fokus kembali.
- `FieldCombo`, `FieldSlider`, `FieldCheck`: dropdown, slider, dan checkbox bergaya sama
- `SecondaryButton`: radius 10, transparan, stroke 1px `BorderBrush`, teks `InkBrush`
- `DangerButton`: radius 10, `DangerBrush`, teks putih
- `FieldLabel`: 12px `MutedBrush`
- `FieldHelper`: 11px `MutedBrush`
- `FieldError`: 11px `DangerBrush`

**Selesai bila:** `dotnet build` bersih dan `BerandaPage` masih tampil seperti sebelumnya.

## A2 · Halaman Simulasi Baru

Berkas: `Views/SimulasiPage.xaml` + `.cs` *(baru)*
Desain: frame `2 · Input Simulasi`

Form dua kolom berisi sembilan isian sesuai tabel di [layar-ui.md](../specs/layar-ui.md#isian-form-simulasi-baru),
plus panel "Estimasi Langsung" di kanan (lebar 320) yang memperbarui diri setiap isian berubah.

Panel estimasi menampilkan: kapasitas terpasang (kWp), jenis panel, jumlah panel,
luas terpakai, Peak Sun Hours, Performance Ratio, koreksi suhu, estimasi biaya instalasi.

Perhitungannya **jangan ditulis ulang**: pakai yang sudah ada:

```csharp
var lokasi = new Lokasi(nama, lat, lon, luasAtap, orientasi, kemiringan);
PanelSurya panel = jenisPanel == "Thin film"
    ? new PanelThinFilm(merk, kapasitasWp, efisiensi, koefSuhu, luasPerPanel, hargaPerUnit)
    : new PanelMonokristal(merk, kapasitasWp, efisiensi, koefSuhu, luasPerPanel, hargaPerUnit);

int jumlah   = panel.hitungJumlahPanel(lokasi.hitungLuasEfektif());
double kWp   = panel.hitungKapasitasTotal(jumlah) / 1000.0;
double biaya = new AnalisisFinansial(biayaPerWp).hitungBiayaInstalasi(kWp * 1000);
```

Tombol **Hitung Kelayakan** menyusun `SimulasiSkenario`, memanggil `jalankanSimulasi()`,
lalu berpindah ke `HasilPage`.

Karena pengambilan data Open-Meteo memakan waktu, urutannya:

1. buka `new HasilPage()` (state memuat) lewat `Buka(...)`
2. jalankan `ambilDataRadiasiAsync()` (BE-3)
3. panggil `TampilkanHasil(skenario, dataCadangan)`

**Selesai bila:** mengisi form lalu menekan Hitung menghasilkan `HasilPage` berisi angka dari
isian, bukan angka contoh.

**Bergantung pada:** B1, BE-3.

## A3 · State validasi error

Berkas: `Views/SimulasiPage.xaml` + `.cs`
Desain: frame `7 · Input (validasi error)`

Validasi dijalankan saat isian kehilangan fokus dan saat tombol Hitung ditekan.
Gunakan `Lokasi.validasi()` yang sudah ada, **jangan buat validator baru**.

| Isian | Aturan | Pesan |
|---|---|---|
| Nama skenario | tidak boleh kosong | `Nama skenario wajib diisi.` |
| Luas atap | `> 0` | `Luas atap harus lebih dari 0 m².` |
| Orientasi | `0 ≤ x < 360` | `Orientasi harus antara 0° dan 359°.` |
| Kemiringan | `0 ≤ x ≤ 45` | `Kemiringan di luar rentang 0°–45°.` |
| Biaya per Wp | `> 0` | `Biaya per Wp harus diisi.` |
| Tarif per kWh | `> 0` bila manual | `Tarif per kWh harus lebih dari 0.` |

Banner di atas form menyebut jumlah isian bermasalah. Tombol Hitung nonaktif selama ada error,
dan panel estimasi menampilkan `-`.

**Selesai bila:** luas atap `0` memunculkan border merah, pesan, banner, dan tombol nonaktif;
memperbaikinya mengembalikan semuanya ke normal.

## A4 · State tarif manual

Berkas: `Views/SimulasiPage.xaml` + `.cs`
Desain: frame `8 · Input (tarif manual)`

Checkbox "Masukkan tarif sendiri" mematikan dropdown golongan dan memunculkan isian
`Tarif per kWh`. Nilainya dipakai membuat `TarifListrikPLN` dengan golongan `"Tarif manual"`
dan `tanggalBerlaku = DateTime.Today`.

**Selesai bila:** mencentang checkbox mengganti sumber tarif, dan hasil simulasi ikut berubah.

## A5 · Navigasi

Berkas: `MainWindow.xaml.cs`, `Views/TentangPage.xaml` *(kerangka kosong)*

Ganti `switch` yang sekarang mengarahkan "Simulasi Baru" ke `HasilPage`:

```csharp
Konten.Content = Nav.SelectedIndex switch
{
    0 => new BerandaPage(),
    1 => new SimulasiPage(),
    2 => new RiwayatPage(),
    3 => new TentangPage(),
    _ => new BerandaPage()
};
```

Tambahkan `Buka(UserControl halaman)` supaya `SimulasiPage` dapat berpindah ke `HasilPage`
sambil membawa objek skenario, tanpa lewat indeks nav.

Buat `Views/TentangPage.xaml` sebagai `UserControl` kosong supaya navigasi bisa dikompilasi,
isinya dikerjakan di A7.

**Selesai bila:** keempat item sidebar membuka halaman masing-masing, dan komentar
`// ponytail: "Simulasi Baru" langsung ke Hasil…` sudah dihapus.

## A6 · Beranda memakai riwayat asli

Berkas: `Views/BerandaPage.xaml` + `.cs`
Desain: frame `1 · Beranda`

Ganti daftar `contoh` dengan `new RiwayatSimulasi(new RepositoriSimulasi()).muatRiwayat().Take(3)`.

Dua hal yang perlu diperbaiki sekalian:

- kartu di Figma menampilkan **Kapasitas / Balik modal / Hemat per bulan**, sedangkan kode
  sekarang menampilkan **Produksi per tahun / Balik modal / Hemat per tahun**. Ikuti Figma.
- bila riwayat kosong, tampilkan ajakan "Belum ada simulasi, mulai dari Simulasi Baru",
  bukan kartu kosong.

**Selesai bila:** menyimpan simulasi baru membuat kartunya muncul di Beranda setelah pindah halaman.

**Bergantung pada:** BE-2.

## A7 · Halaman Tentang

Berkas: `Views/TentangPage.xaml` + `.cs`
Desain: frame `6 · Tentang`

Mengisi kerangka kosong yang dibuat di A5.

Dua kolom: kiri (616) berisi kartu Tim Pengembang dan Rumus & Konstanta; kanan (320) berisi
Sumber Data, Versi Aplikasi, dan Disclaimer.

Nilai konstanta ditampilkan dari kode, bukan ditulis tangan di XAML, supaya tidak pernah basi:
Performance Ratio dari `KalkulatorEnergi`, degradasi dan umur sistem dari `AnalisisFinansial`,
faktor pemanfaatan atap dari `Lokasi`, faktor emisi dari `DampakLingkungan`.

Sebagian konstanta itu kini `private`. Membukanya **bukan tugas A7**: `Services/` dan `Models/`
milik jalur backend, dan BE-8 sudah menyentuh berkas yang sama. Konstanta publiknya disediakan
di BE-8. Sambil menunggu, susun tata letak dan teks statis (tim, sumber data, disclaimer) lebih
dulu.

Halaman ini memenuhi klaim "menampilkan seluruh asumsi secara terbuka" di README.

**Selesai bila:** mengubah konstanta di service ikut mengubah angka di halaman Tentang.

**Bergantung pada:** A5 untuk kerangka, BE-8 untuk angka konstanta.

---

# FE-B: Mayravivania

## B1 · HasilPage menerima data nyata

Berkas: `Views/HasilPage.xaml.cs`
Desain: frame `3 · Hasil (Energi)`

**Kerjakan ini paling awal**: jalur FE-A tidak bisa dikompilasi sebelum konstruktor ini ada.

Terapkan kontrak di bagian [Kontrak antar-jalur](#kontrak-antar-jalur).

Buang data contoh:

```csharp
// hapus
double[] produksi = { 456, 435, 472, 483, 499, 504, 542, 547, 523, 491, 451, 444 };

// ganti
var kalkulator = new KalkulatorEnergi(lokasi, panel, dataRadiasi);
var produksi = Enumerable.Range(1, 12).Select(kalkulator.hitungProduksiBulanan).ToArray();
```

`Ringkasan.Text` disusun dari lokasi, luas atap, orientasi, kemiringan, dan kapasitas sebenarnya.
`SetaraKonsumsi` memakai tagihan bulanan dari form, bukan angka `594` yang ditulis tetap.

Nilai di daftar Asumsi diambil dari objek, bukan ditulis tangan:
PSH dari `DataRadiasi.getRataRataTahunan()`, Performance Ratio dari `KalkulatorEnergi`,
koreksi suhu dari `panel.hitungKoreksiSuhu(...)`.

> Teks Figma menulis PSH "10 tahun terakhir" dan PR `0,78`, sedangkan kode mengambil 1 tahun
> dan memakai `0.80`. Lihat [ADR 0001](../decisions/0001-asumsi-perhitungan.md): ikuti nilai
> yang diputuskan di sana, jangan menebak.

**Selesai bila:** dua skenario berbeda menghasilkan grafik dan KPI yang berbeda.

**Bergantung pada:** BE-2. `SimulasiSkenario` sekarang menyimpan lokasi, panel, dan data
radiasi sebagai `private` tanpa getter, dan belum punya tempat untuk tagihan bulanan, B1
tidak bisa menghitung grafik bulanan maupun KPI "Setara konsumsi" sebelum BE-2 menambahkannya.
Konstruktor dan tata letaknya tetap bisa dikerjakan lebih dulu.

## B2 · Tab Finansial & Lingkungan

Berkas: `Views/HasilPage.xaml` + `.cs`
Desain: frame `4 · Hasil (Finansial)`

Hapus `TabFinansial_Checked` yang sekarang hanya memunculkan `MessageBox`.

Empat KPI: estimasi biaya instalasi, penghematan per bulan, periode balik modal,
keuntungan bersih 25 tahun.

Grafik **Arus Kas Kumulatif 25 Tahun** dengan garis nol dan penanda titik balik modal.
Panel **Dampak Lingkungan**: emisi CO₂ dihindari per tahun, setara pohon, setara km mobil,
total CO₂ 25 tahun.

Tiga nilai berikut belum punya method, disediakan BE-7, jangan dihitung di view:

```csharp
List<double> AnalisisFinansial.proyeksiArusKas()        // 26 nilai, tahun 0..25
double DampakLingkungan.setaraKilometerMobil(double emisi)
double DampakLingkungan.totalEmisi25Tahun(double produksiTahunan, double degradasi)
```

**Selesai bila:** berpindah tab menampilkan angka finansial yang konsisten dengan tab Energi,
dan titik balik modal pada grafik sama dengan KPI periode balik modal.

**Bergantung pada:** B1, BE-7.

## B3 · State memuat data

Berkas: `Views/HasilPage.xaml` + `.cs`
Desain: frame `9 · Hasil (memuat data)`

Konstruktor tanpa argumen menampilkan skeleton `TrackBrush` di tempat angka KPI, batang grafik,
dan nilai asumsi, plus banner "Mengambil data radiasi Open-Meteo untuk {lokasi}, biasanya
5–20 detik."

`TampilkanHasil(...)` mengganti skeleton dengan angka sebenarnya.

**Selesai bila:** dari menekan Hitung sampai hasil tampil, jendela tetap responsif dan tidak
pernah membeku.

**Bergantung pada:** BE-3.

## B4 · State data cadangan

Berkas: `Views/HasilPage.xaml` + `.cs`
Desain: frame `10 · Hasil (data cadangan)`

Bila `TampilkanHasil` dipanggil dengan `dataCadangan: true`, tampilkan banner `AccentSoftBrush`:

> Open-Meteo tidak dapat dihubungi. Angka di bawah memakai data cadangan rata-rata Indonesia
> (4,5 kWh/m²/hari), kurang akurat untuk lokasi ini.

Baris PSH pada Asumsi ditandai `AccentBrush` dengan keterangan "Cadangan rata-rata Indonesia,
API tidak terjangkau", dan subjudul halaman diberi akhiran "· data cadangan".

Ini bukan hiasan: `LayananCuacaAPI.ambilDataRadiasi()` saat ini mengembalikan rata-rata nasional
tanpa memberi tahu siapa pun ketika API gagal.

**Selesai bila:** mematikan koneksi internet lalu menjalankan simulasi memunculkan banner,
bukan angka yang tampak normal.

## B5 · Hapus skenario

Berkas: `Views/RiwayatPage.xaml` + `.cs`
Desain: frame `12 · Riwayat (konfirmasi hapus)`

Tambahkan kolom **Aksi** berisi ikon hapus di tiap baris. Mengkliknya memunculkan dialog 420px
di atas overlay hitam 38%, dengan tombol `Batal` (`SecondaryButton`) dan `Hapus` (`DangerButton`).

Service-nya sudah ada, `RiwayatSimulasi.hapusSkenario(id)`. Jangan menulis query baru.

Dialog menyebut nama skenario dan menyatakan bahwa tindakan tidak dapat dibatalkan.

**Selesai bila:** menghapus satu baris menghilangkannya dari tabel dan dari database setelah
aplikasi dibuka ulang.

## B6 · Riwayat kosong

Berkas: `Views/RiwayatPage.xaml` + `.cs`
Desain: frame `11 · Riwayat (kosong)`

Ganti perilaku sekarang yang menampilkan lima baris data contoh Figma. Menampilkan data palsu
yang tampak nyata lebih buruk daripada menampilkan keadaan kosong.

Kartu empty state: ilustrasi rumah berpanel, judul "Belum ada simulasi tersimpan", penjelasan,
tombol "Mulai Simulasi" yang membuka halaman Simulasi Baru.

Ilustrasinya **sudah tersedia** di `Assets/ilustrasi-kosong.png` (480×480, latar transparan,
sudah terdaftar sebagai `Resource` di csproj), jangan buat ulang. Pemakaian yang sama sudah ada
di kartu kosong Beranda:

```xml
<Image Source="/Assets/ilustrasi-kosong.png" Width="160" Height="160"
       RenderOptions.BitmapScalingMode="HighQuality"/>
```
Tombol Bandingkan nonaktif bertuliskan `Bandingkan (0)`.

**Selesai bila:** menghapus berkas `%LOCALAPPDATA%\SunCost\suncost.db` lalu membuka Riwayat
menampilkan empty state, bukan data Figma.

---

## Urutan pengerjaan

```
BE-1 ─ BE-2 ──────────────┐
                          ├─ A6
BE-3 ─────────┐           │
              ├─ A2 ─ A3 ─ A4
B1 ───────────┘
B1 ─ B2 (butuh BE-7)
B1 ─ B3 ─ B4
A5 ─ A7 (angka konstanta butuh BE-8)
B5, B6, A1, A5  ← tidak bergantung pada BE, bisa mulai kapan saja
```

Yang bisa dikerjakan **sekarang juga** tanpa menunggu backend: A1, A5, tata letak A7, B5, B6.

## Aturan kerja

- Satu tugas = satu commit, pesan berawalan kode tugas: `feat(A2): halaman Simulasi Baru`.
- Jangan mengubah berkas milik jalur lain. Kalau perlu perubahan di sana, minta pemiliknya.
- Jangan menulis ulang rumus di view. Semua perhitungan sudah ada di `Services/` dan `Models/`.
- Jangan menambah dependensi baru. Proyek ini hanya memakai `Microsoft.Data.Sqlite`.
- Angka yang bentrok antara Figma dan kode: ikuti [ADR 0001](../decisions/0001-asumsi-perhitungan.md).
