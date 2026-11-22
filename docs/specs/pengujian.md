# Pengujian Black Box SunCost

Format mengikuti Lab 10.2 Modul 10 (Functional Testing dengan teknik Black Box). 28 kasus uji mencakup seluruh halaman, validasi isian, data cadangan, cache, dan instalasi.

Pengujian otomatis untuk mesin hitung dan basis data (white box) terpisah, ada di `Services/SelfCheck.cs` dan berjalan setiap kali build Debug dibuka.

| | |
|---|---|
| **Nama Aplikasi** | SunCost, Kalkulator PLTS Atap |
| **Tanggal Pengujian** | 22 November 2026 |
| **Tester** | Muhammad Farrel Al Ghazy |

**Hasil: 28 dari 28 kasus Sesuai, 0 Tidak Sesuai, 0 belum diuji. Pengujian dijalankan otomatis dengan Windows UI Automation (tests/uji-blackbox.ps1) pada build terbaru, mulai dari basis data kosong. Kondisi tanpa internet (C3 dan C4) disimulasikan khusus untuk proses SunCost lewat proxy yang tidak aktif, tanpa mematikan jaringan laptop.**

## A. Navigasi dan Beranda

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
|---|---|---|---|---|---|
| 1 | Beranda | Membuka aplikasi untuk pertama kali (riwayat masih kosong) | Tampil Beranda dengan ajakan mulai simulasi dan keterangan belum ada simulasi | Aplikasi gagal terbuka atau tampil kartu kosong | Sesuai (✓) Tidak Sesuai ( ) |
|  |  | Membuka Beranda setelah ada simulasi tersimpan | Tampil maksimal 3 kartu simulasi terakhir berisi kapasitas, balik modal, dan hemat per bulan | Kartu tidak tampil atau angka berisi "-" | Sesuai (✓) Tidak Sesuai ( ) |
|  |  | Klik tombol Mulai Simulasi | Pindah ke halaman Simulasi Baru | Halaman tidak berpindah | Sesuai (✓) Tidak Sesuai ( ) |
| 2 | Sidebar | Klik menu Beranda, Simulasi Baru, Riwayat, dan Tentang bergantian | Halaman yang sesuai tampil dan menu terpilih ditandai | Halaman tidak berpindah atau salah halaman | Sesuai (✓) Tidak Sesuai ( ) |

## B. Halaman Simulasi Baru

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
|---|---|---|---|---|---|
| 1 | Lokasi | Memilih kota pada dropdown Lokasi | Koordinat di bawah dropdown berubah dan data radiasi diambil di latar belakang tanpa membekukan jendela | Koordinat tidak berubah atau jendela tidak merespons | Sesuai (✓) Tidak Sesuai ( ) |
| 2 | Estimasi langsung | Mengisi luas atap 40 m² dengan panel Monokristalin 550 Wp | Panel Estimasi Langsung menampilkan 11 unit × 550 Wp dan kapasitas ±6,05 kWp beserta estimasi biaya | Estimasi tidak berubah atau angka tidak wajar | Sesuai (✓) Tidak Sesuai ( ) |
|  |  | Mengganti jenis panel ke Thin film | Kapasitas, jumlah panel, dan koreksi suhu pada estimasi ikut berubah | Estimasi tidak berubah | Sesuai (✓) Tidak Sesuai ( ) |
| 3 | Validasi isian | Mengosongkan nama skenario | Tampil pesan "Nama skenario wajib diisi.", kolom bergaris merah, dan tombol Hitung nonaktif sehingga simulasi tidak dijalankan | Simulasi tetap berjalan tanpa nama | Sesuai (✓) Tidak Sesuai ( ) |
|  |  | Mengisi luas atap 0 lalu klik Hitung Kelayakan | Tampil pesan "Luas atap harus lebih dari 0 m².", banner jumlah isian salah, dan tombol Hitung nonaktif | Simulasi tetap berjalan atau aplikasi error | Sesuai (✓) Tidak Sesuai ( ) |
|  |  | Mengosongkan biaya per Wp | Tampil pesan "Biaya per Wp harus diisi." dan tombol Hitung nonaktif | Tidak ada pesan kesalahan | Sesuai (✓) Tidak Sesuai ( ) |
|  |  | Mengisi tagihan bulanan dengan 0 | Tampil pesan "Tagihan harus lebih dari 0 kWh." | Tidak ada pesan kesalahan | Sesuai (✓) Tidak Sesuai ( ) |
| 4 | Tarif listrik | Memilih golongan tarif PLN pada dropdown | Dropdown berisi 4 golongan tarif dari basis data beserta harga per kWh | Dropdown kosong | Sesuai (✓) Tidak Sesuai ( ) |
|  |  | Mencentang "Masukkan tarif sendiri" dan mengisi 2.000 | Dropdown golongan nonaktif, kolom tarif manual tampil, dan hasil simulasi memakai tarif 2.000 | Dropdown tetap aktif atau tarif manual tidak dipakai | Sesuai (✓) Tidak Sesuai ( ) |
|  |  | Mencentang tarif manual, mengosongkan isinya, lalu klik Hitung Kelayakan | Tampil pesan "Tarif per kWh harus lebih dari 0.", tombol Hitung nonaktif, dan simulasi tidak dijalankan | Simulasi tetap berjalan | Sesuai (✓) Tidak Sesuai ( ) |
| 5 | Hitung kelayakan | Klik Hitung Kelayakan dengan seluruh isian valid | Tampil keadaan memuat, lalu halaman Hasil Simulasi; skenario tersimpan ke riwayat | Halaman Hasil tidak tampil atau skenario tidak tersimpan | Sesuai (✓) Tidak Sesuai ( ) |

## C. Halaman Hasil Simulasi

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
|---|---|---|---|---|---|
| 1 | Tab Energi | Melihat tab Energi setelah menghitung | Tampil produksi tahunan, rata-rata bulanan, setara konsumsi, grafik produksi 12 bulan, dan panel asumsi (PSH, PR 0,78, koreksi suhu) | Angka atau grafik tidak tampil | Sesuai (✓) Tidak Sesuai ( ) |
| 2 | Tab Finansial & Lingkungan | Klik tab Finansial & Lingkungan | Tampil biaya instalasi, hemat per bulan, balik modal, keuntungan bersih 25 tahun, grafik arus kas kumulatif, dan dampak lingkungan (CO₂, setara pohon, setara km) | Tab tidak bisa dibuka atau angka kosong | Sesuai (✓) Tidak Sesuai ( ) |
| 3 | Data cadangan | Mematikan internet lalu menghitung di kota yang belum pernah disimulasikan | Hasil tetap tampil dengan banner peringatan bahwa data radiasi memakai nilai cadangan | Aplikasi error atau tidak ada peringatan | Sesuai (✓) Tidak Sesuai ( ) |
| 4 | Cache radiasi | Mematikan internet lalu menghitung lagi di kota yang sudah disimulasikan kurang dari 30 hari lalu | Hasil langsung tampil tanpa banner data cadangan karena data dibaca dari basis data | Muncul banner data cadangan atau menunggu koneksi | Sesuai (✓) Tidak Sesuai ( ) |

## D. Halaman Riwayat

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
|---|---|---|---|---|---|
| 1 | Riwayat kosong | Membuka Riwayat saat belum ada simulasi | Tampil ilustrasi keadaan kosong dan tombol Mulai Simulasi | Tampil tabel kosong tanpa keterangan | Sesuai (✓) Tidak Sesuai ( ) |
| 2 | Daftar riwayat | Membuka Riwayat setelah ada simulasi | Tampil tabel berisi lokasi, luas atap, kapasitas, balik modal, hemat per bulan, dan tanggal, urut dari yang terbaru | Data tidak tampil atau kolom berisi "-" | Sesuai (✓) Tidak Sesuai ( ) |
|  |  | Klik tombol Lihat (ikon mata) pada satu skenario | Halaman Hasil Simulasi skenario tersebut terbuka dan kedua tab dapat dibuka | Halaman Hasil tidak terbuka | Sesuai (✓) Tidak Sesuai ( ) |
| 3 | Bandingkan | Mencentang 2 atau 3 skenario lalu klik Bandingkan | Tampil kartu perbandingan berdampingan dengan penanda "Balik modal tercepat" | Tombol Bandingkan tidak aktif atau perbandingan tidak tampil | Sesuai (✓) Tidak Sesuai ( ) |
|  |  | Mencentang skenario keempat | Centang ditolak dengan pesan maksimal 3 skenario | Skenario keempat ikut terpilih | Sesuai (✓) Tidak Sesuai ( ) |
| 4 | Hapus | Klik ikon hapus lalu klik Batal | Dialog konfirmasi tampil, lalu tertutup tanpa menghapus data | Data langsung terhapus tanpa konfirmasi | Sesuai (✓) Tidak Sesuai ( ) |
|  |  | Klik ikon hapus lalu klik Hapus | Skenario hilang dari tabel dan tetap tidak ada setelah aplikasi dibuka ulang | Skenario tidak terhapus atau muncul lagi | Sesuai (✓) Tidak Sesuai ( ) |

## E. Halaman Tentang

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
|---|---|---|---|---|---|
| 1 | Tentang | Membuka halaman Tentang | Tampil tim pengembang, rumus energi, konstanta (PR 0,78, degradasi 0,5%, faktor emisi 0,87 kg/kWh), sumber data, dan disclaimer | Halaman kosong atau konstanta tidak sesuai | Sesuai (✓) Tidak Sesuai ( ) |

## F. Instalasi dan Basis Data

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
|---|---|---|---|---|---|
| 1 | Basis data | Membuka aplikasi pertama kali saat basis data belum ada | Berkas %LOCALAPPDATA%\SunCost\suncost.db dibuat otomatis, dropdown panel dan tarif langsung terisi | Muncul galat basis data atau dropdown kosong | Sesuai (✓) Tidak Sesuai ( ) |

## Catatan hasil pengujian

Nilai yang benar-benar terbaca dari aplikasi saat pengujian.

| Kasus | Halaman | Hasil | Yang teramati |
|---|---|---|---|
| A1.1 | Beranda | Sesuai | tampil: Belum ada simulasi |
| A1.2 | Beranda | Sesuai | 3 kartu: Rumah Sleman, Rumah Kupang, Rumah Surabaya |
| A1.3 | Beranda | Sesuai | halaman Simulasi Baru terbuka |
| A2 | Sidebar | Sesuai | Beranda/Simulasi/Riwayat/Tentang: True/True/True/True |
| B1 | Lokasi | Sesuai | koordinat '−7,7326° 110,3800° · data radiasi dari Open-Meteo' -> '−7,2575° 112,7521° · data radiasi dari Open-Meteo'; respons UI terlama 21 ms selama pengambilan data |
| B2.1 | Estimasi langsung | Sesuai | kapasitas 6,1 kWp; jumlah panel 11 unit × 550 Wp; luas terpakai 28,6 m² dari 40 m²; biaya Rp 90.750.000 |
| B2.2 | Estimasi langsung | Sesuai | kapasitas 6,1 kWp -> 2,3 kWp; 21 unit × 110 Wp; koreksi suhu −11,3% (sel 53 °C) -> −4,6% (sel 53 °C) |
| B3.1 | Validasi isian | Sesuai | pesan: Nama skenario wajib diisi.; Hitung aktif: False; tetap di halaman Simulasi |
| B3.2 | Validasi isian | Sesuai | pesan: Luas atap harus lebih dari 0 m².; banner: 1 isian belum benar. Perbaiki dulu sebelum menghitung.; Hitung aktif: False |
| B3.3 | Validasi isian | Sesuai | pesan: Biaya per Wp harus diisi.; Hitung aktif: False |
| B3.4 | Validasi isian | Sesuai | pesan: Tagihan harus lebih dari 0 kWh. |
| B4.1 | Tarif listrik | Sesuai | R-1/TR 900 VA (RTM) · Rp 1.352,00 / kWh; R-1/TR 1.300 VA · Rp 1.444,70 / kWh; R-1/TR 2.200 VA · Rp 1.444,70 / kWh; R-2/TR 3.500-5.500 VA · Rp 1.699,53 / kWh |
| B4.2 | Tarif listrik | Sesuai | dropdown golongan nonaktif: True; hemat/bulan tarif 2.000 = Rp 1.509.281 vs tarif 1.444,70 = Rp 1.090.229 (rasio 1,384, seharusnya 1,384) |
| B4.3 | Tarif listrik | Sesuai | pesan: Tarif per kWh harus lebih dari 0.; Hitung aktif: False; tetap di halaman Simulasi |
| B5 | Hitung kelayakan | Sesuai | halaman Hasil: 'Rumah Surabaya · atap 40 m² · orientasi 0° · kemiringan 15° · 6,05 kWp'; keadaan memuat terlihat: False |
| C1 | Tab Energi | Sesuai | produksi 9.056 kWh/tahun, rata-rata 755, setara 252%, 12 batang grafik, PR 0,78 tampil |
| C2 | Tab Finansial & Lingkungan | Sesuai | balik modal 7,0 tahun; untung 25 thn Rp 217.426.552; 7.878 kg CO₂ dihindari per tahun; Setara 359 pohon; Setara 41.034 km perjalanan mobil |
| C3 | Data cadangan | Sesuai | hasil tampil: True; banner: Open-Meteo tidak dapat dihubungi. Angka di bawah memakai data cadangan rata-rata Indonesia (4,5 kWh/m²/hari) — kurang akurat untuk lokasi ini. |
| C4 | Cache radiasi | Sesuai | hasil dalam 1386 ms tanpa banner cadangan; produksi 9.056 sama dengan simulasi online (9.056) |
| D1 | Riwayat kosong | Sesuai | keadaan kosong: True, tombol Mulai Simulasi: 2 |
| D2.1 | Daftar riwayat | Sesuai | 4 baris, terbaru di atas: Rumah Sleman, 40 m², 6,05 kWp, 7,7 tahun, Rp 1.001.999, 22 Nov 2026 |
| D2.2 | Daftar riwayat | Sesuai | terbuka: 'Rumah Kupang · atap 50 m² · orientasi 0° · kemiringan 15° · 7,98 kWp'; tab Finansial bisa dibuka |
| D3.1 | Bandingkan | Sesuai | tombol 'Bandingkan (3)' aktif: True; penanda balik modal tercepat dan untung 25 thn tampil di 3 kartu |
| D3.2 | Bandingkan | Sesuai | pesan: Maksimal 3 skenario. Batalkan salah satu pilihan untuk memilih skenario lain.; centang keempat: Off |
| D4.1 | Hapus | Sesuai | dialog: 'Skenario “Rumah Sleman” akan dihapus. Tindakan ini tidak dapat dibatalkan.'; setelah Batal jumlah baris tetap 4 |
| D4.2 | Hapus | Sesuai | pesan: 'Skenario “Uji Tarif Manual” berhasil dihapus.'; setelah dibuka ulang 3 baris, 'Uji Tarif Manual' tidak ada |
| E1 | Tentang | Sesuai | PR 0,78, degradasi 0,5%, emisi 0,87 kg/kWh, tim tampil |
| F1 | Basis data | Sesuai | suncost.db dibuat: True; panel 3, tarif 4 |
