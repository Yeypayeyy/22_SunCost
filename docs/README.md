# Dokumentasi SunCost

| Dokumen | Isi |
|---|---|
| [ERD.md](ERD.md) | Rancangan basis data: diagram, kardinalitas, DDL SQLite |
| [specs/layar-ui.md](specs/layar-ui.md) | Peta 12 frame Figma ke view WPF, token desain, aturan state |
| [plans/implementasi-fe.md](plans/implementasi-fe.md) | Pembagian kerja frontend: jalur FE-A dan FE-B |
| [plans/implementasi-be.md](plans/implementasi-be.md) | Pembagian kerja backend: BE-1 sampai BE-8 |
| [decisions/0001-asumsi-perhitungan.md](decisions/0001-asumsi-perhitungan.md) | Tujuh angka asumsi yang bentrok antara Figma dan kode |
| [decisions/0002-database-sqlite.md](decisions/0002-database-sqlite.md) | Basis data tetap SQLite lokal, beserta alternatif yang ditimbang |
| [panduan-penggunaan.md](panduan-penggunaan.md) | Panduan singkat penggunaan (keluaran Modul 10) |
| [panduan-basis-data.md](panduan-basis-data.md) | Panduan konfigurasi basis data (keluaran Modul 10) |

## Pembagian tugas

| Orang | Peran | Tugas |
|---|---|---|
| Muhammad Farrel Al Ghazy | Software Architect + Frontend | FE-A: A1–A7 ✅, BE-3 ✅, BE-4 ✅, BE-6 ✅, keluaran Modul 10 |
| Mayravivania Syahda Charisa | Frontend Developer | FE-B: B1–B6 ✅ |
| Sri Wahyuni Arista | Backend Developer | BE-1 ✅, BE-2 ✅, BE-5 ✅, BE-7 ✅, BE-8 ✅ |

Jalur FE-A dan FE-B sengaja dipisah berdasarkan kepemilikan berkas, sehingga keduanya dapat
dikerjakan bersamaan tanpa konflik merge. Daftar kepemilikannya ada di
[plans/implementasi-fe.md](plans/implementasi-fe.md#pembagian-kepemilikan-berkas).

## Keadaan sekarang

- **Seluruh tugas BE-1 sampai BE-8 dan FE A1–A7, B1–B6 sudah di `main`.**
- **Frontend:** Simulasi Baru (validasi, tarif manual, dropdown dari database), Hasil (Energi,
  Finansial & Lingkungan, state memuat dan data cadangan), Riwayat (lihat ulang, hapus, bandingkan,
  keadaan kosong), Beranda, dan Tentang (konstanta dibaca langsung dari mesin hitung).
- **Backend:** skema ERD enam tabel, input skenario tersimpan dan dirakit ulang, data master,
  layanan cuaca asinkron dengan cache 30 hari, dan 96 pemeriksaan di `Services/SelfCheck.cs`.

### Sisa pekerjaan

| Pemilik | Tugas |
|---|---|
| Farrel | slide demo |
| Mayra | cek ulang tampilan secara manual (opsional) |
| Sri | Uji silang 3 skenario ke PVGIS (opsional) |

## Keluaran wajib Modul 10

| Keluaran | Pemilik | Status |
|---|---|---|
| Aplikasi final | semua | ✅ berjalan, fitur lengkap |
| Layanan web API | Farrel | Open-Meteo sudah dipakai, sama dengan preseden kating (OpenWeatherMap) |
| Dokumen pengujian | Farrel (BE-6) | ✅ 28/28 Sesuai ([md](specs/pengujian.md), [docx](pengujian-blackbox-suncost.docx)), diuji otomatis lewat `tests/uji-blackbox.ps1` |
| Panduan singkat penggunaan | Farrel | ✅ [panduan-penggunaan.md](panduan-penggunaan.md), [docx](panduan-penggunaan-suncost.docx) |
| Panduan konfigurasi basis data | Farrel | ✅ [panduan-basis-data.md](panduan-basis-data.md) |
| Pengemasan `.exe` | Farrel | ✅ satu berkas ±70 MB, self-contained (runtime .NET ikut di dalamnya) |

Perintah pengemasan satu berkas. `IncludeNativeLibrariesForSelfExtract` wajib, supaya library
native SQLite ikut terbawa:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```
