# Panduan Konfigurasi Basis Data

SunCost memakai **SQLite**: satu berkas lokal tanpa server. Pengguna tidak perlu memasang atau
mengatur apa pun. Alasan pemilihannya ada di [ADR 0002](decisions/0002-database-sqlite.md).

## Ringkasan

| Hal | Nilai |
|---|---|
| Mesin | SQLite 3, lewat paket NuGet `Microsoft.Data.Sqlite` 8.0.10 |
| Lokasi berkas | `%LOCALAPPDATA%\SunCost\suncost.db` (contoh: `C:\Users\<nama>\AppData\Local\SunCost\suncost.db`) |
| Dibuat oleh | `Services/RepositoriSimulasi.cs`, saat aplikasi pertama kali membuka basis data |
| Server, akun, password | tidak ada |
| Koneksi internet | tidak diperlukan untuk basis data |

## Apa yang terjadi saat aplikasi dibuka

Konstruktor `RepositoriSimulasi` menjalankan langkah berikut setiap kali dipanggil. Semuanya aman
diulang dan tidak pernah menghapus data pengguna:

1. Membuat folder `%LOCALAPPDATA%\SunCost` bila belum ada.
2. Menjalankan DDL enam tabel dengan `CREATE TABLE IF NOT EXISTS`: `lokasi`, `panel_surya`,
   `tarif_listrik_pln`, `data_radiasi`, `radiasi_bulanan`, `skenario`. Rinciannya ada di
   [ERD.md](ERD.md).
3. Menambah kolom yang belum ada pada basis data versi lama dengan `ALTER TABLE ... ADD COLUMN`.
   Baris lama tetap terbaca, dengan kolom input bernilai `NULL`.
4. Menyisipkan data awal dengan `INSERT OR IGNORE` dan id tetap, sehingga tidak tergandakan:
   - 3 panel: Monokristalin 420 Wp, Monokristalin 550 Wp, Thin film a-Si 110 Wp
   - 4 golongan tarif PLN: R-1/900 VA, R-1/1.300 VA, R-1/2.200 VA, R-2/3.500–5.500 VA

Setiap koneksi menjalankan `PRAGMA foreign_keys = ON;`, karena SQLite mematikan foreign key
secara bawaan dan pengaturannya berlaku per koneksi.

## Isi basis data

| Tabel | Isi | Ditulis oleh |
|---|---|---|
| `panel_surya`, `tarif_listrik_pln` | Data master untuk dropdown Simulasi Baru | data awal, dan `simpan()` untuk panel atau tarif manual |
| `lokasi` | Atap pada satu skenario: koordinat, luas, orientasi, kemiringan | `simpan()` |
| `data_radiasi`, `radiasi_bulanan` | Peak Sun Hours 12 bulan dan suhu rata-rata dari Open-Meteo | `simpan()` |
| `skenario` | Hasil simulasi beserta input finansialnya | `simpan()` |

`data_radiasi` sekaligus berfungsi sebagai **cache**. Data API untuk koordinat yang sama dan belum
berumur 30 hari dibaca dari sini, bukan dari internet. Data cadangan tidak pernah dipakai ulang.

## Melihat isi basis data

Tutup aplikasi dulu, lalu buka berkasnya dengan salah satu alat berikut:

- [DB Browser for SQLite](https://sqlitebrowser.org/) (gratis, berbasis GUI): *Open Database*, lalu pilih `suncost.db`.
- Ekstensi VS Code "SQLite Viewer".
- `sqlite3` di terminal:

```powershell
sqlite3 "$env:LOCALAPPDATA\SunCost\suncost.db" ".tables"
```

Contoh query untuk melihat riwayat lengkap dengan lokasinya:

```sql
SELECT s.nama, l.nama AS lokasi, s.kapasitas_wp / 1000.0 AS kwp,
       s.produksi_tahunan, s.payback_period
FROM skenario s LEFT JOIN lokasi l ON l.id = s.lokasi_id
ORDER BY s.tanggal DESC;
```

## Mencadangkan, memulihkan, dan mengosongkan

Tutup aplikasi sebelum melakukan langkah mana pun.

```powershell
# cadangkan
Copy-Item "$env:LOCALAPPDATA\SunCost\suncost.db" "$HOME\Desktop\suncost-cadangan.db"
```

```powershell
# pulihkan
Copy-Item "$HOME\Desktop\suncost-cadangan.db" "$env:LOCALAPPDATA\SunCost\suncost.db" -Force
```

```powershell
# kosongkan seluruh riwayat (berkas dibuat ulang otomatis, lengkap dengan data awal)
Remove-Item "$env:LOCALAPPDATA\SunCost\suncost.db"
```

## Untuk pengembang

- Semua akses basis data lewat `RepositoriSimulasi`. Halaman tidak pernah menulis SQL.
- Query selalu berparameter (`$id`, `$nama`, ...). Teks SQL hanya berupa konstanta.
- Konstruktor menerima jalur berkas lain, misalnya `new RepositoriSimulasi(@"C:\temp\uji.db")`.
  `Services/SelfCheck.cs` memakai cara ini untuk menguji migrasi dan penyimpanan di berkas
  sementara, tanpa menyentuh data pengguna.
- Mengubah skema: tambahkan tabel baru ke konstanta `Skema` dengan `IF NOT EXISTS`, dan kolom baru
  ke `KolomTambahanSkenario`. Jangan menambah kolom `NOT NULL` ke tabel yang sudah berisi.

## Masalah yang mungkin muncul

| Gejala | Penyebab dan solusi |
|---|---|
| Riwayat menampilkan "Riwayat tidak dapat dimuat" | Folder `%LOCALAPPDATA%\SunCost` tidak bisa ditulis, atau berkas sedang dibuka program lain. Tutup DB Browser atau aplikasi lain, lalu buka ulang SunCost. |
| `database is locked` | Ada dua jendela SunCost atau satu alat SQLite yang sedang menulis. Tutup salah satunya. |
| Dropdown panel atau tarif kosong | Tidak akan terjadi: kalau basis data tidak terbaca, dropdown memakai data awal yang sama dari kode. |
| Ingin mulai dari nol | Hapus `suncost.db` (lihat di atas). |
