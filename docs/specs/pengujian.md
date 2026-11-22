# Pengujian Black Box SunCost

Format mengikuti Lab 10.2 Modul 10 (Functional Testing dengan teknik Black Box). 29 kasus uji mencakup seluruh halaman, validasi isian, data cadangan, cache, dan instalasi.

Pengujian otomatis untuk mesin hitung dan basis data (white box) terpisah, ada di `Services/SelfCheck.cs` dan berjalan setiap kali build Debug dibuka.

| | |
|---|---|
| **Nama Aplikasi** | SunCost, Kalkulator PLTS Atap |
| **Tanggal Pengujian** | |
| **Tester** | |

Isi kolom Hasil dengan ✓ pada Sesuai atau Tidak Sesuai setelah menjalankan setiap aksi.

## A. Navigasi dan Beranda

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
|---|---|---|---|---|---|
| 1 | Beranda | Membuka aplikasi untuk pertama kali (riwayat masih kosong) | Tampil Beranda dengan ajakan mulai simulasi dan keterangan belum ada simulasi | Aplikasi gagal terbuka atau tampil kartu kosong | Sesuai ( ) Tidak Sesuai ( ) |
|  |  | Membuka Beranda setelah ada simulasi tersimpan | Tampil maksimal 3 kartu simulasi terakhir berisi kapasitas, balik modal, dan hemat per bulan | Kartu tidak tampil atau angka berisi "-" | Sesuai ( ) Tidak Sesuai ( ) |
|  |  | Klik tombol Mulai Simulasi | Pindah ke halaman Simulasi Baru | Halaman tidak berpindah | Sesuai ( ) Tidak Sesuai ( ) |
| 2 | Sidebar | Klik menu Beranda, Simulasi Baru, Riwayat, dan Tentang bergantian | Halaman yang sesuai tampil dan menu terpilih ditandai | Halaman tidak berpindah atau salah halaman | Sesuai ( ) Tidak Sesuai ( ) |

## B. Halaman Simulasi Baru

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
|---|---|---|---|---|---|
| 1 | Lokasi | Memilih kota pada dropdown Lokasi | Koordinat di bawah dropdown berubah dan data radiasi diambil di latar belakang tanpa membekukan jendela | Koordinat tidak berubah atau jendela tidak merespons | Sesuai ( ) Tidak Sesuai ( ) |
| 2 | Estimasi langsung | Mengisi luas atap 40 m² dengan panel Monokristalin 550 Wp | Panel Estimasi Langsung menampilkan 11 unit × 550 Wp dan kapasitas ±6,05 kWp beserta estimasi biaya | Estimasi tidak berubah atau angka tidak wajar | Sesuai ( ) Tidak Sesuai ( ) |
|  |  | Mengganti jenis panel ke Thin film | Kapasitas, jumlah panel, dan koreksi suhu pada estimasi ikut berubah | Estimasi tidak berubah | Sesuai ( ) Tidak Sesuai ( ) |
| 3 | Validasi isian | Mengosongkan nama skenario lalu klik Hitung Kelayakan | Tampil pesan "Nama skenario wajib diisi.", kolom bergaris merah, dan simulasi tidak dijalankan | Simulasi tetap berjalan tanpa nama | Sesuai ( ) Tidak Sesuai ( ) |
|  |  | Mengisi luas atap 0 lalu klik Hitung Kelayakan | Tampil pesan "Luas atap harus lebih dari 0 m².", banner jumlah isian salah, dan tombol Hitung nonaktif | Simulasi tetap berjalan atau aplikasi error | Sesuai ( ) Tidak Sesuai ( ) |
|  |  | Mengosongkan biaya per Wp | Tampil pesan "Biaya per Wp harus diisi." dan tombol Hitung nonaktif | Tidak ada pesan kesalahan | Sesuai ( ) Tidak Sesuai ( ) |
|  |  | Mengisi tagihan bulanan dengan 0 | Tampil pesan "Tagihan harus lebih dari 0 kWh." | Tidak ada pesan kesalahan | Sesuai ( ) Tidak Sesuai ( ) |
| 4 | Tarif listrik | Memilih golongan tarif PLN pada dropdown | Dropdown berisi 4 golongan tarif dari basis data beserta harga per kWh | Dropdown kosong | Sesuai ( ) Tidak Sesuai ( ) |
|  |  | Mencentang "Masukkan tarif sendiri" dan mengisi 2.000 | Dropdown golongan nonaktif, kolom tarif manual tampil, dan hasil simulasi memakai tarif 2.000 | Dropdown tetap aktif atau tarif manual tidak dipakai | Sesuai ( ) Tidak Sesuai ( ) |
|  |  | Mencentang tarif manual tetapi mengosongkan isinya | Tampil pesan "Tarif per kWh harus lebih dari 0." dan tombol Hitung nonaktif | Simulasi tetap berjalan | Sesuai ( ) Tidak Sesuai ( ) |
| 5 | Hitung kelayakan | Klik Hitung Kelayakan dengan seluruh isian valid | Tampil keadaan memuat, lalu halaman Hasil Simulasi; skenario tersimpan ke riwayat | Halaman Hasil tidak tampil atau skenario tidak tersimpan | Sesuai ( ) Tidak Sesuai ( ) |

## C. Halaman Hasil Simulasi

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
|---|---|---|---|---|---|
| 1 | Tab Energi | Melihat tab Energi setelah menghitung | Tampil produksi tahunan, rata-rata bulanan, setara konsumsi, grafik produksi 12 bulan, dan panel asumsi (PSH, PR 0,78, koreksi suhu) | Angka atau grafik tidak tampil | Sesuai ( ) Tidak Sesuai ( ) |
| 2 | Tab Finansial & Lingkungan | Klik tab Finansial & Lingkungan | Tampil biaya instalasi, hemat per bulan, balik modal, keuntungan bersih 25 tahun, grafik arus kas kumulatif, dan dampak lingkungan (CO₂, setara pohon, setara km) | Tab tidak bisa dibuka atau angka kosong | Sesuai ( ) Tidak Sesuai ( ) |
| 3 | Data cadangan | Mematikan internet lalu menghitung di kota yang belum pernah disimulasikan | Hasil tetap tampil dengan banner peringatan bahwa data radiasi memakai nilai cadangan | Aplikasi error atau tidak ada peringatan | Sesuai ( ) Tidak Sesuai ( ) |
| 4 | Cache radiasi | Mematikan internet lalu menghitung lagi di kota yang sudah disimulasikan kurang dari 30 hari lalu | Hasil langsung tampil tanpa banner data cadangan karena data dibaca dari basis data | Muncul banner data cadangan atau menunggu koneksi | Sesuai ( ) Tidak Sesuai ( ) |

## D. Halaman Riwayat

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
|---|---|---|---|---|---|
| 1 | Riwayat kosong | Membuka Riwayat saat belum ada simulasi | Tampil ilustrasi keadaan kosong dan tombol Mulai Simulasi | Tampil tabel kosong tanpa keterangan | Sesuai ( ) Tidak Sesuai ( ) |
| 2 | Daftar riwayat | Membuka Riwayat setelah ada simulasi | Tampil tabel berisi lokasi, luas atap, kapasitas, balik modal, hemat per bulan, dan tanggal, urut dari yang terbaru | Data tidak tampil atau kolom berisi "-" | Sesuai ( ) Tidak Sesuai ( ) |
|  |  | Klik tombol Lihat (ikon mata) pada satu skenario | Halaman Hasil Simulasi skenario tersebut terbuka dan kedua tab dapat dibuka | Halaman Hasil tidak terbuka | Sesuai ( ) Tidak Sesuai ( ) |
| 3 | Bandingkan | Mencentang 2 atau 3 skenario lalu klik Bandingkan | Tampil kartu perbandingan berdampingan dengan penanda "Balik modal tercepat" | Tombol Bandingkan tidak aktif atau perbandingan tidak tampil | Sesuai ( ) Tidak Sesuai ( ) |
|  |  | Mencentang skenario keempat | Centang ditolak dengan pesan maksimal 3 skenario | Skenario keempat ikut terpilih | Sesuai ( ) Tidak Sesuai ( ) |
| 4 | Hapus | Klik ikon hapus lalu klik Batal | Dialog konfirmasi tampil, lalu tertutup tanpa menghapus data | Data langsung terhapus tanpa konfirmasi | Sesuai ( ) Tidak Sesuai ( ) |
|  |  | Klik ikon hapus lalu klik Hapus | Skenario hilang dari tabel dan tetap tidak ada setelah aplikasi dibuka ulang | Skenario tidak terhapus atau muncul lagi | Sesuai ( ) Tidak Sesuai ( ) |

## E. Halaman Tentang

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
|---|---|---|---|---|---|
| 1 | Tentang | Membuka halaman Tentang | Tampil tim pengembang, rumus energi, konstanta (PR 0,78, degradasi 0,5%, faktor emisi 0,87 kg/kWh), sumber data, dan disclaimer | Halaman kosong atau konstanta tidak sesuai | Sesuai ( ) Tidak Sesuai ( ) |

## F. Instalasi dan Basis Data

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
|---|---|---|---|---|---|
| 1 | SunCost.exe | Menjalankan SunCost.exe di laptop yang tidak memasang .NET | Aplikasi terbuka tanpa instalasi tambahan | Muncul pesan runtime .NET tidak ditemukan | Sesuai ( ) Tidak Sesuai ( ) |
|  |  | Membuka aplikasi pertama kali di laptop baru | Berkas %LOCALAPPDATA%\SunCost\suncost.db dibuat otomatis, dropdown panel dan tarif langsung terisi | Muncul galat basis data atau dropdown kosong | Sesuai ( ) Tidak Sesuai ( ) |
