# Dokumentasi SunCost

| Dokumen | Isi |
|---|---|
| [ERD.md](ERD.md) | Rancangan basis data: diagram, kardinalitas, DDL SQLite |
| [specs/layar-ui.md](specs/layar-ui.md) | Peta 12 frame Figma ke view WPF, token desain, aturan state |
| [plans/implementasi-fe.md](plans/implementasi-fe.md) | Pembagian kerja frontend: jalur FE-A dan FE-B |
| [plans/implementasi-be.md](plans/implementasi-be.md) | Pembagian kerja backend: BE-1 sampai BE-8 |
| [decisions/0001-asumsi-perhitungan.md](decisions/0001-asumsi-perhitungan.md) | Tujuh angka asumsi yang bentrok antara Figma dan kode |
| [decisions/0002-database-sqlite.md](decisions/0002-database-sqlite.md) | Basis data tetap SQLite lokal, beserta alternatif yang ditimbang |

## Pembagian tugas

| Orang | Peran | Tugas |
|---|---|---|
| Muhammad Farrel Al Ghazy | Software Architect + Frontend | FE-A: A1–A7 ✅, BE-3, BE-6, keluaran Modul 10 |
| Mayravivania Syahda Charisa | Frontend Developer | FE-B: B1–B6 ✅ (B2 menunggu BE-7) |
| Sri Wahyuni Arista | Backend Developer | BE-1 ✅, BE-2, BE-4, BE-5, BE-7, BE-8 |

Jalur FE-A dan FE-B sengaja dipisah berdasarkan kepemilikan berkas, sehingga keduanya dapat
dikerjakan bersamaan tanpa konflik merge. Daftar kepemilikannya ada di
[plans/implementasi-fe.md](plans/implementasi-fe.md#pembagian-kepemilikan-berkas).

## Keadaan sekarang

- **Frontend:** seluruh halaman sudah di `main`: Simulasi Baru dengan validasi dan tarif manual,
  Hasil (tab Energi, tata letak tab Finansial, state memuat dan data cadangan), Riwayat (hapus,
  bandingkan, keadaan kosong), Beranda, dan Tentang. Logo dan ikon aplikasi terpasang.
- **Backend:** mesin perhitungan lengkap dan teruji lewat `Services/SelfCheck.cs`, termasuk
  polimorfisme panel dan faktor orientasi atap. Skema ERD enam tabel sudah ada di SQLite (BE-1).
- **Yang masih kurang:** penyimpanan input skenario (BE-2), sehingga Riwayat dan Beranda masih
  menampilkan `-` untuk kapasitas; angka tab Finansial (BE-7); data master panel dan tarif dari
  database (BE-5).

## Keluaran wajib Modul 10

| Keluaran | Pemilik | Status |
|---|---|---|
| Aplikasi final | semua | berjalan, menunggu BE-2, BE-5, BE-7 |
| Layanan web API | Farrel | Open-Meteo sudah dipakai, sama dengan preseden kating (OpenWeatherMap) |
| Dokumen pengujian | Farrel (BE-6) | belum |
| Panduan singkat penggunaan | Farrel | belum |
| Panduan konfigurasi basis data | Farrel | belum; isinya sederhana, lihat ADR 0002 |
| Pengemasan `.exe` | Farrel | belum |

Perintah pengemasan satu berkas. `IncludeNativeLibrariesForSelfExtract` wajib, supaya library
native SQLite ikut terbawa:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```
