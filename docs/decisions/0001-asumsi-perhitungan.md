# 0001: Menyelaraskan asumsi perhitungan

- **Status:** diterima
- **Tanggal usulan:** 2026-11-21
- **Tanggal diputuskan:** 2026-11-21
- **Pemutus:** Muhammad Farrel Al Ghazy

## Konteks

Audit desain Figma terhadap kode menemukan tujuh angka asumsi yang berbeda antara keduanya.
Selama perbedaan ini dibiarkan, setiap implementasi frontend harus menebak mana yang benar,
dan halaman Hasil akan menampilkan angka yang tidak cocok dengan teks asumsinya sendiri,
tepat merusak klaim pembeda aplikasi ini, yaitu "seluruh asumsi ditampilkan terbuka".

## Perbedaan

| # | Hal | Figma | Kode | Usulan |
|---|---|---|---|---|
| 1 | Performance Ratio | `0,78` | `0.80` | **0,78** |
| 2 | Faktor emisi CO₂ | `0,87 kg/kWh` (KESDM Jawa–Bali) | `0.794` | **0,87** |
| 3 | Periode data radiasi | "10 tahun terakhir" | 1 tahun (`AddYears(-1)`) | **perbaiki teks jadi 1 tahun** |
| 4 | Koreksi suhu | `−4,2%` pada 31,5 °C | `−10,8%` pada sel 52 °C | **ikuti kode** |
| 5 | Faktor pemanfaatan atap | 20,4 / 32 = `63,75%` | `0.75` | **0,75** |
| 6 | Panel contoh | `420 Wp` | `550 Wp` | **jadikan data master** |
| 7 | Biaya instalasi contoh | `Rp 88.500.000` | - | **Rp 88.200.000** |

## Alasan tiap usulan

**1. Performance Ratio 0,78.** Keduanya masuk akal untuk PLTS atap. Figma menuliskannya di
dua tempat yang terlihat pengguna, kode di satu tempat yang tidak terlihat. Mengubah satu
konstanta lebih murah daripada mengubah dua label, dan 0,78 sedikit lebih konservatif,
lebih aman untuk aplikasi yang justru dibuat karena estimasi vendor terlalu optimistis.

**2. Faktor emisi 0,87.** Angka Figma punya sumber yang disebut namanya (KESDM, jaringan
Jawa–Bali), sedangkan `0.794` di kode tidak punya rujukan tertulis. Untuk aplikasi yang
menjanjikan transparansi, angka bersumber mengalahkan angka tanpa sumber. Selisihnya 9%
pada klaim dampak lingkungan, jadi bukan hal sepele.

**3. Perbaiki teksnya, bukan kodenya.** `LayananCuacaAPI` mengambil satu tahun arsip
terakhir. Mengubahnya menjadi sepuluh tahun akan memperbesar unduhan sekitar sepuluh kali
lipat dan memperlama simulasi pertama, tanpa manfaat sepadan untuk estimasi atap rumah
tangga. Teks Figma-lah yang keliru menjanjikan.

**4. Pertahankan perhitungan kode.** Ini perbedaan paling serius: 2,5 kali lipat. Figma
menerapkan koreksi pada suhu udara (31,5 °C), padahal panel surya di bawah penyinaran penuh
jauh lebih panas daripada udara sekitarnya. Kode sudah benar secara fisika, menambahkan
kenaikan NOCT 25 °C sehingga suhu sel menjadi 52 °C, lalu menerapkan −0,4% per °C di atas
25 °C. Teks dan angka di Figma yang harus diperbaiki.

Perlu dicatat: tab Energi pada frame 3 menuliskan `−0,4% / °C`: itu benar, karena menyebut
koefisiennya, bukan hasilnya. Yang salah hanya `−4,2%` pada panel estimasi di frame 2.

**5. Faktor pemanfaatan 0,75.** Angka Figma tidak konsisten dengan dirinya sendiri: panel
estimasi menulis "20,4 m² dari 32 m²" (63,75%) sekaligus "10 unit", padahal 20,4 m² dibagi
luas panel yang dipakai tidak menghasilkan 10 unit bulat. Konstanta `0.75` di `Lokasi`
punya penjelasan tertulis, jarak antar-larik dan jalur perawatan.

**6. Spesifikasi panel jadi data master.** Kode memakai contoh 550 Wp, Figma 420 Wp.
Begitu BE-5 mengisi tabel `panel_surya` dan pengguna memilih panel dari dropdown, tidak ada
lagi "panel contoh" yang perlu disepakati. Angka 420 Wp di Figma cukup diperlakukan sebagai
salah satu baris data master.

**7. Biaya instalasi Rp 88.200.000.** Muncul setelah isian "Biaya per Wp" ditambahkan ke
form. Dengan 4,2 kWp dan Rp 21.000/Wp hasilnya Rp 88.200.000, sedangkan frame Finansial
masih menulis Rp 88.500.000. Angka hasil perkalian harus menang atas angka yang ditulis
tangan.

## Akibat bila diterima

Perubahan kode, dikerjakan sebagai **BE-8**:

| Berkas | Perubahan |
|---|---|
| `Services/KalkulatorEnergi.cs` | `performanceRatio` dari `0.80` menjadi `0.78` |
| `Services/DampakLingkungan.cs` | `faktorEmisiCO2` bawaan dari `0.794` menjadi `0.87` |
| `Services/SelfCheck.cs` | perbarui pernyataan uji yang memakai kedua angka di atas |

`Models/Lokasi.cs` tidak berubah, usulan 5 mempertahankan nilai yang sekarang.

Perubahan teks Figma:

| Frame | Perubahan |
|---|---|
| `2 · Input Simulasi` | koreksi suhu `−4,2% (rata-rata 31,5 °C)` → `−10,8% (sel 52 °C)` |
| `2 · Input Simulasi` | luas terpakai `20,4 m² dari 32 m²` → `24 m² dari 32 m²` |
| `3 · Hasil (Energi)` | sumber PSH "10 tahun terakhir" → "1 tahun terakhir" |
| `4 · Hasil (Finansial)` | biaya instalasi `Rp 88.500.000` → `Rp 88.200.000` |

Angka turunan pada frame Finansial (penghematan, balik modal, keuntungan 25 tahun, emisi)
ikut bergeser setelah Performance Ratio dan faktor emisi berubah. Angka-angka itu hanya
contoh tampilan, jadi tidak wajib dihitung ulang satu per satu, tetapi setelah B2 selesai,
pastikan nilai yang dihasilkan aplikasi saling konsisten antar-tab.

## Keputusan

Ketujuh usulan **diterima seluruhnya tanpa perubahan** pada 2026-11-21.

Dua pekerjaan mengikuti keputusan ini:

| Pekerjaan | Pemilik | Status |
|---|---|---|
| Perbaikan teks di Figma (4 butir) | Software Architect | selesai 2026-11-21 |
| **BE-8**: menyelaraskan konstanta di kode | Sri Wahyuni Arista | belum dikerjakan |

BE-8 kini boleh dikerjakan. Dua konstanta yang berubah:

| Berkas | Dari | Menjadi |
|---|---|---|
| `Services/KalkulatorEnergi.cs` → `performanceRatio` | `0.80` | `0.78` |
| `Services/DampakLingkungan.cs` → `faktorEmisiCO2` | `0.794` | `0.87` |

Pernyataan uji di `Services/SelfCheck.cs` yang memakai kedua angka itu ikut diperbarui,
termasuk pemeriksaan kewajaran produksi per kWp (1.100–1.600 kWh) yang batasnya bergeser
sedikit karena Performance Ratio turun, dan pemeriksaan emisi yang membandingkan hasil
dengan pengali `0.794`.

`Models/Lokasi.cs` tidak berubah: faktor pemanfaatan atap tetap `0.75`.
