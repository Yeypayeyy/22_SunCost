# 0002: Basis data tetap SQLite lokal

- **Status:** diterima
- **Tanggal:** 2026-11-22
- **Pemutus:** Muhammad Farrel Al Ghazy

## Konteks

SunCost sempat memiliki dua arah basis data sekaligus: SQLite lokal (BE-1, skema ERD enam tabel)
dan PostgreSQL lewat Npgsql di branch `538308`. Sebelum BE-2 sampai BE-5 dikerjakan, mesin basis
data harus diputuskan, karena seluruh kode penyimpanan bergantung padanya.

Kriteria utamanya: **orang lain (dosen, asisten penilai) bisa menjalankan aplikasi semudah
mungkin**, sesuai worksheet Modul 10: *"Kemas aplikasi sehingga proses instalasi dan proses
konfigurasi menjadi sederhana."*

Data yang disimpan SunCost adalah riwayat simulasi pribadi tiap pengguna. Tidak ada fitur
berbagi atau kolaborasi, sehingga data tidak perlu dipakai bersama antar-komputer.

## Keputusan

SunCost memakai **SQLite** lewat `Microsoft.Data.Sqlite`. Seluruh basis data adalah satu berkas
`%LOCALAPPDATA%\SunCost\suncost.db` yang dibuat otomatis saat aplikasi pertama dibuka.

## Alternatif yang ditimbang

| Alternatif | Langkah penilai | Alasan ditolak |
|---|---|---|
| PostgreSQL di Supabase lewat Npgsql | Atur variabel lingkungan berisi connection string | Butuh internet; proyek gratis di-*pause* setelah ~seminggu tanpa aktivitas; port 5432 sering diblokir jaringan kampus; koneksi langsung paket gratis hanya IPv6 |
| PostgreSQL di Supabase lewat `supabase-csharp` (REST) | Ekstrak, klik dua kali | Butuh internet dan proyek aktif; kunci ikut di `.exe` sehingga keamanan bergantung penuh pada Row Level Security; pemisahan riwayat per komputer tidak benar-benar terjamin |
| PostgreSQL di Neon / layanan cloud lain | Ekstrak, klik dua kali (dengan berkas konfigurasi) | Butuh internet; port 5432; kerja ulang seluruh lapisan penyimpanan |
| PostgreSQL lokal | Pasang server PostgreSQL, jalankan skrip skema, buat role | Langkah instalasi paling panjang dan paling rawan salah bagi penilai |

Semua alternatif PostgreSQL juga mengharuskan BE-1, yang sudah selesai dan teruji dalam SQLite,
dikerjakan ulang.

## Konsekuensi

**Positif**

- Penilai cukup mengekstrak dan mengklik dua kali `SunCost.exe`. Tidak ada instalasi, password,
  server, atau port.
- Aplikasi tetap berjalan tanpa internet. Hanya data radiasi Open-Meteo yang butuh internet, dan
  saat gagal aplikasi memakai data cadangan sambil menampilkan banner peringatan.
- BE-1 tetap dipakai. Tidak ada kerja ulang.
- Riwayat otomatis terpisah per komputer, karena berkasnya memang terpisah.

**Negatif**

- Data tidak bisa dilihat bersama dari laptop lain. Untuk demo, gunakan satu laptop atau isi
  ulang skenario contoh.
- SQLite mematikan foreign key secara bawaan. Sudah ditangani di BE-1 dengan
  `PRAGMA foreign_keys = ON` pada setiap koneksi.
- Pengemasan `.exe` satu berkas harus menyertakan library native SQLite (`e_sqlite3.dll`).
  Tambahkan `-p:IncludeNativeLibrariesForSelfExtract=true` pada `dotnet publish`.

## Panduan konfigurasi basis data (Modul 10)

Dengan keputusan ini, panduan konfigurasi basis data cukup menjelaskan:

- letak berkas: `%LOCALAPPDATA%\SunCost\suncost.db`;
- **cadangan:** salin berkas tersebut;
- **reset:** hapus berkas tersebut, dan aplikasi membuat basis data kosong yang baru saat dibuka lagi;
- **melihat isi:** buka berkasnya dengan DB Browser for SQLite;
- skema dan relasinya mengacu ke [ERD.md](../ERD.md).

## Dampak ke pekerjaan

- Kode PostgreSQL (`IRepositoriSimulasi`, `RepositoriSimulasiPostgres`, `FabrikRepositori`, paket
  `Npgsql`) sudah dikeluarkan dari `main` saat branch `538308` dirapikan. Jangan ditambahkan lagi.
- BE-2, BE-4, dan BE-5 dikerjakan di atas `Services/RepositoriSimulasi.cs` yang sudah ada, dengan
  pola migrasi kolom (`ALTER TABLE ... ADD COLUMN`) yang dipakai BE-1.
