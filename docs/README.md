# Dokumentasi SunCost

| Dokumen | Isi |
|---|---|
| [ERD.md](ERD.md) | Rancangan basis data: diagram, kardinalitas, DDL SQLite |
| [specs/layar-ui.md](specs/layar-ui.md) | Peta 12 frame Figma ke view WPF, token desain, aturan state |
| [plans/implementasi-fe.md](plans/implementasi-fe.md) | Pembagian kerja frontend: jalur FE-A dan FE-B |
| [plans/implementasi-be.md](plans/implementasi-be.md) | Pembagian kerja backend: BE-1 sampai BE-8 |
| [decisions/0001-asumsi-perhitungan.md](decisions/0001-asumsi-perhitungan.md) | Tujuh angka asumsi yang bentrok antara Figma dan kode |

## Pembagian tugas

| Orang | Peran | Tugas |
|---|---|---|
| Muhammad Farrel Al Ghazy | Software Architect + Frontend | FE-A: A1–A7 |
| Mayravivania Syahda Charisa | Frontend Developer | FE-B: B1–B6 |
| Sri Wahyuni Arista | Backend Developer | BE-1 sampai BE-8 |

Jalur FE-A dan FE-B sengaja dipisah berdasarkan kepemilikan berkas, sehingga keduanya dapat
dikerjakan bersamaan tanpa konflik merge. Daftar kepemilikannya ada di
[plans/implementasi-fe.md](plans/implementasi-fe.md#pembagian-kepemilikan-berkas).

## Keadaan sekarang

Mesin perhitungan sudah lengkap dan terbukti lewat `Services/SelfCheck.cs`. Yang belum ada
adalah jembatan antara mesin itu dan antarmuka: halaman input belum dibuat, ketiga halaman
yang ada masih memakai data contoh, layanan cuaca tidak pernah dipanggil dari alur pengguna,
dan basis data baru memiliki satu dari enam tabel pada ERD.

## Yang belum ditulis

Tiga keluaran wajib Modul 10 belum ada dan belum dimasukkan ke rencana mana pun:

- panduan singkat penggunaan aplikasi
- panduan konfigurasi basis data
- pengemasan `.exe` (`dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true --self-contained true`)

Dokumen pengujian dicakup oleh BE-6.
