# Spesifikasi Layar UI

Sumber desain: [UI-UX-SunCost di Figma](https://www.figma.com/design/eZ7u3wVkggATG2xYOc4SmW/UI-UX-SunCost)
(file key `eZ7u3wVkggATG2xYOc4SmW`, 1 page, 12 frame).

Semua frame berukuran **1280×800**, sama dengan `MainWindow` (`Height="800" Width="1280"`).
Lebar sidebar 240, area konten 1040, padding konten 36 atas/bawah dan 40 kiri/kanan.

## Peta frame → view

| # | Frame Figma | View WPF | Status kode |
|---|---|---|---|
| 1 | `1 · Beranda` | [BerandaPage.xaml](../../Views/BerandaPage.xaml) | ada, data contoh |
| 2 | `2 · Input Simulasi` | `Views/SimulasiPage.xaml` | **belum ada** |
| 3 | `3 · Hasil — Energi` | [HasilPage.xaml](../../Views/HasilPage.xaml) | ada, data contoh |
| 4 | `4 · Hasil — Finansial` | tab kedua `HasilPage` | **belum ada** (MessageBox) |
| 5 | `5 · Riwayat Simulasi` | [RiwayatPage.xaml](../../Views/RiwayatPage.xaml) | ada |
| 6 | `6 · Tentang` | `Views/TentangPage.xaml` | **belum ada** |
| 7 | `7 · Input — validasi error` | state `SimulasiPage` | **belum ada** |
| 8 | `8 · Input — tarif manual` | state `SimulasiPage` | **belum ada** |
| 9 | `9 · Hasil — memuat data` | state `HasilPage` | **belum ada** |
| 10 | `10 · Hasil — data cadangan` | state `HasilPage` | **belum ada** |
| 11 | `11 · Riwayat — kosong` | state `RiwayatPage` | sebagian (teks saja) |
| 12 | `12 · Riwayat — konfirmasi hapus` | state `RiwayatPage` | **belum ada** |

Indeks navigasi di [MainWindow.xaml.cs](../../MainWindow.xaml.cs): `0` Beranda, `1` Simulasi Baru,
`2` Riwayat, `3` Tentang.

## Token desain

File Figma tidak memakai variable/style — semua nilai hardcoded. Daftar di bawah adalah hasil
pembacaan frame yang ada, dipetakan ke resource di [App.xaml](../../App.xaml).

### Warna

| Hex | Resource App.xaml | Dipakai untuk |
|---|---|---|
| `#F5F3EF` | `CanvasBrush` | latar jendela |
| `#FFFFFF` | `SurfaceBrush` | kartu, panel, baris tabel |
| `#1C1B19` | `InkBrush` | teks utama |
| `#6B665E` | `MutedBrush` | label, keterangan, teks sekunder |
| `#E3DFD8` | `BorderBrush` | garis kartu, pemisah baris |
| `#C1652F` | `AccentBrush` | tombol primer, angka sorotan, nav aktif |
| `#F9EFE7` | `AccentSoftBrush` | latar nav aktif, KPI utama, banner |
| `#2F6B4F` | `GreenBrush` | angka penghematan/keuntungan |
| `#ECE9E4` | `TrackBrush` | track slider, **skeleton loading** |
| `#FAF9F7` | *(belum ada)* | latar input |
| `#F8F6F3` | `SubtleBrush` (`#F6F4F1`) | header tabel |
| `#D89867` | `BarBrush` | batang grafik non-puncak |

Dua warna **baru** yang dipakai frame 7 dan 12 dan belum ada di `App.xaml`:

```xml
<SolidColorBrush x:Key="DangerBrush"     Color="#9E3B2F"/>
<SolidColorBrush x:Key="DangerSoftBrush" Color="#F9EAE7"/>
```

Skeleton memakai `TrackBrush` yang sudah ada — tidak perlu brush baru.

### Tipografi

Inter, line-height 140% di seluruh file. WPF memakai `FontFamily="Segoe UI"`; perbedaan ini
diterima apa adanya dan tidak perlu disamakan.

| Ukuran | Berat | Dipakai untuk |
|---|---|---|
| 34 / 32 | Semi Bold | judul hero |
| 26 | Semi Bold | judul halaman |
| 20 | Semi Bold | wordmark sidebar |
| 17 | Semi Bold | judul kartu besar, judul dialog |
| 15–16 | Semi Bold | judul kartu |
| 14 | Semi Bold / Regular | nama baris, isi tabel |
| 13 | Regular / Medium | isi umum, nilai |
| 12 | Regular / Medium | label, header tabel |
| 11 | Regular / Medium | keterangan, badge, helper |

### Bentuk

| Nilai | Dipakai untuk |
|---|---|
| radius 16 | kartu hero |
| radius 12 | kartu, panel, dialog |
| radius 10 | tombol, tab bersegmen |
| radius 8 | input, blok kode, banner |
| radius 20 | badge peran |
| stroke 1px `BorderBrush` | semua kartu dan input |
| padding kartu | 22–24 |
| gap antar-field | 12–18 |
| gap antar-kartu | 24 |

## Isian form Simulasi Baru

Frame 2 memuat sembilan isian dalam dua kolom. Empat di antaranya ditambahkan karena mesin
perhitungan membutuhkannya tetapi desain awal tidak menyediakan tempatnya.

| Isian | Tipe | Contoh | Dipakai oleh |
|---|---|---|---|
| Nama skenario | teks | `Rumah Sleman — atap utara` | `SimulasiSkenario.nama` |
| Lokasi | dropdown + koordinat | `Sleman, DIY` / `-7,7326° 110,3800°` | `Lokasi`, `LayananCuacaAPI` |
| Luas atap tersedia | angka, m² | `32` | `Lokasi.luasAtap` |
| Orientasi atap | dropdown | `Utara` | `Lokasi.orientasi` |
| Kemiringan atap | slider 0–45° | `15°` | `Lokasi.kemiringan` |
| Jenis panel | dropdown | `Monokristalin — 420 Wp` | `PanelMonokristal` / `PanelThinFilm` |
| Biaya per Wp terpasang | angka, Rp/Wp | `21.000` | `AnalisisFinansial.biayaPerWp` |
| Tagihan listrik bulanan | angka, kWh | `594` | KPI "Setara konsumsi" |
| Golongan tarif PLN | dropdown + checkbox | `R-1 / 1300 VA` | `TarifListrikPLN` |

Checkbox **"Masukkan tarif sendiri"** (frame 8) mematikan dropdown golongan dan memunculkan
isian `Tarif per kWh`.

## Aturan state

| State | Pemicu | Tampilan |
|---|---|---|
| Validasi error | `Lokasi.validasi()` gagal atau isian wajib kosong | banner jumlah error di atas form, border `DangerBrush` pada isian bermasalah, pesan 11px di bawahnya, tombol Hitung nonaktif, panel estimasi jadi `—` |
| Memuat | pengambilan data Open-Meteo berjalan | skeleton `TrackBrush` menggantikan angka KPI, batang grafik, dan nilai asumsi; banner "Mengambil data radiasi… 5–20 detik" |
| Data cadangan | `LayananCuacaAPI` mengembalikan `DataCadangan()` | banner peringatan `AccentSoftBrush`, PSH `4,5 jam/hari`, sumber data ditandai "Cadangan rata-rata Indonesia" dengan warna `AccentBrush` |
| Riwayat kosong | `muatRiwayat()` mengembalikan daftar kosong | kartu empty state dengan ikon, ajakan, tombol "Mulai Simulasi"; tombol Bandingkan nonaktif `Bandingkan (0)` |
| Konfirmasi hapus | klik ikon hapus pada satu baris | overlay hitam 38%, dialog 420px, tombol `Batal` (garis) dan `Hapus` (`DangerBrush`) |

State **data cadangan wajib terlihat**. `LayananCuacaAPI.ambilDataRadiasi()` saat ini diam-diam
mengembalikan rata-rata nasional ketika API gagal — tanpa banner ini pengguna menerima angka
yang salah tanpa tahu.

## Yang belum didesain

Berikut tidak ada di Figma dan boleh diputuskan saat implementasi:

- state hover/focus/pressed untuk tombol, baris tabel, dan isian
- dropdown dalam keadaan terbuka
- perilaku saat jendela diperkecil di bawah 1280×800
