# Use Case Diagram SunCost

SunCost tidak punya login. Aplikasi dipakai satu pengguna di laptopnya sendiri, dan seluruh data
tersimpan lokal di `%LOCALAPPDATA%\SunCost\suncost.db`. Karena itu hanya ada satu aktor utama,
ditambah Open-Meteo sebagai aktor sistem eksternal.

## Diagram (Mermaid)

Mermaid belum punya jenis diagram use case, jadi diagram di bawah memakai flowchart: aktor di
kiri dan kanan, use case berbentuk elips di dalam batas sistem.

```mermaid
flowchart LR
    pengguna(["👤 Pengguna<br/>(pemilik rumah)"])
    openmeteo(["🌐 Open-Meteo<br/>Archive API"])

    subgraph SunCost
        uc1([Membuat simulasi baru])
        uc2([Melihat hasil energi])
        uc3([Melihat hasil finansial & lingkungan])
        uc4([Melihat riwayat simulasi])
        uc5([Membandingkan 2–3 skenario])
        uc6([Menghapus skenario])
        uc7([Melihat asumsi & tim])
        inc1([Mengambil data radiasi lokasi])
        inc2([Menyimpan skenario ke SQLite])
    end

    pengguna --- uc1
    pengguna --- uc2
    pengguna --- uc3
    pengguna --- uc4
    pengguna --- uc5
    pengguna --- uc6
    pengguna --- uc7

    uc1 -. "«include»" .-> inc1
    uc1 -. "«include»" .-> inc2
    inc1 --- openmeteo
```

## Daftar use case

| Use case | Halaman | Yang terjadi |
|---|---|---|
| Membuat simulasi baru | Simulasi Baru | Pengguna mengisi lokasi, luas atap, orientasi, kemiringan, panel, biaya per Wp, dan tarif PLN, lalu menekan Hitung Kelayakan. |
| Mengambil data radiasi lokasi «include» | (latar belakang) | Radiasi 12 bulan dan suhu rata-rata diambil dari Open-Meteo. Kalau ada cache di bawah 30 hari, cache yang dipakai. Kalau API gagal, dipakai data cadangan dan muncul banner peringatan. |
| Menyimpan skenario ke SQLite «include» | (latar belakang) | Input dan hasil simulasi tersimpan sebagai satu baris `skenario`. |
| Melihat hasil energi | Hasil, tab Energi | Produksi tahunan, rata-rata bulanan, grafik 12 bulan, dan asumsi perhitungan. |
| Melihat hasil finansial & lingkungan | Hasil, tab Finansial & Lingkungan | Biaya, hemat per bulan, balik modal, arus kas 25 tahun, dan CO₂ yang dihindari. |
| Melihat riwayat simulasi | Riwayat, Beranda | Daftar skenario tersimpan, terbaru di atas. Skenario bisa dibuka ulang. |
| Membandingkan 2–3 skenario | Riwayat | Kartu berdampingan dengan penanda balik modal tercepat. |
| Menghapus skenario | Riwayat | Hapus setelah dialog konfirmasi. |
| Melihat asumsi & tim | Tentang | Tim, rumus, konstanta, dan sumber data. |

## Impor ke draw.io

1. Buka draw.io, lalu **Arrange → Insert → Advanced → Mermaid**.
2. Tempel blok mermaid di atas (tanpa pagar ```), lalu klik **Insert**.
