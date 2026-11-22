<#
Pengujian black box SunCost (docs/specs/pengujian.md) lewat Windows UI Automation.

Menjalankan aplikasi seperti pengguna: mengisi form, klik tombol, membaca teks di layar, lalu mencatat
setiap kasus uji Sesuai/Tidak Sesuai. Kondisi tanpa internet disimulasikan hanya untuk proses SunCost
(HTTPS_PROXY ke port yang tidak melayani), jaringan laptop tidak disentuh.

Basis data pengguna dicadangkan dulu, diganti basis data kosong selama pengujian, lalu dikembalikan.
Jangan memakai mouse selama pengujian berjalan (sekitar 3 menit): kasus centang Riwayat memakai klik mouse.

Jalankan dari folder proyek (Windows PowerShell 5.1, karena butuh UIAutomationClient):
    dotnet build
    powershell -ExecutionPolicy Bypass -File tests/uji-blackbox.ps1
#>
param(
  [string]$Exe = (Get-ChildItem "$PSScriptRoot\..\bin\Debug" -Recurse -Filter SunCost.exe | Select-Object -First 1).FullName,
  [string]$OutJson = "$env:TEMP\suncost-hasil-uji.json"
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type @"
using System; using System.Runtime.InteropServices;
public static class W {
  [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h, int x, int y, int w, int hh, bool r);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
}
"@
$A = [System.Windows.Automation.AutomationElement]
$T = [System.Windows.Automation.TreeScope]
$CT = [System.Windows.Automation.ControlType]
$hasil = [ordered]@{}
function Catat($no, [bool]$ok, $catatan) {
  $hasil["$no"] = @{ ok = $ok; catatan = "$catatan" }
  Write-Output ("{0,2} {1} {2}" -f $no, $(if ($ok) { 'LULUS' } else { 'GAGAL' }), $catatan)
}
function Kondisi($prop, $nilai) { New-Object System.Windows.Automation.PropertyCondition($prop, $nilai) }
function Ada($id, $root = $script:win) {
  $e = $root.FindFirst($T::Descendants, (Kondisi $A::AutomationIdProperty $id))
  if ($e -and -not $e.Current.IsOffscreen) { return $e } else { return $null }
}
function Cari($id, $root = $script:win) {
  for ($i = 0; $i -lt 80; $i++) { $e = Ada $id $root; if ($e) { return $e }; Start-Sleep -Milliseconds 250 }
  throw "tidak ketemu: $id"
}
function Nama($nama) { @($script:win.FindAll($T::Descendants, (Kondisi $A::NameProperty $nama)) | Where-Object { -not $_.Current.IsOffscreen }) }
function Teks($e) { if (-not $e) { return '' }; (@($e.Current.Name) + @($e.FindAll($T::Descendants, [System.Windows.Automation.Condition]::TrueCondition) | ForEach-Object { $_.Current.Name })) -join ' | ' }
function Isi($id, $teks) { (Cari $id).GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($teks); Start-Sleep -Milliseconds 150 }
function Butir($id) {
  $cb = Cari $id
  $cb.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand(); Start-Sleep -Milliseconds 300
  $items = @($cb.FindAll($T::Children, (Kondisi $A::ControlTypeProperty $CT::ListItem)))
  $cb.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Collapse()
  return $items
}
function Pilih($id, $i) {
  $cb = Cari $id
  $cb.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand(); Start-Sleep -Milliseconds 300
  $items = @($cb.FindAll($T::Children, (Kondisi $A::ControlTypeProperty $CT::ListItem)))
  $items[$i].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
  $cb.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Collapse(); Start-Sleep -Milliseconds 200
}
function Nav($i) {
  $items = @((Cari 'Nav').FindAll($T::Children, [System.Windows.Automation.Condition]::TrueCondition))
  $items[$i].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
  Start-Sleep -Milliseconds 1200
}
function Tekan($e) { $e.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); Start-Sleep -Milliseconds 500 }
function Klik($e) {
  $r = $e.Current.BoundingRectangle
  [W]::SetForegroundWindow($script:hwnd) | Out-Null; Start-Sleep -Milliseconds 150
  [W]::SetCursorPos([int]($r.X + $r.Width / 2), [int]($r.Y + $r.Height / 2)) | Out-Null
  [W]::mouse_event(2, 0, 0, 0, [IntPtr]::Zero); [W]::mouse_event(4, 0, 0, 0, [IntPtr]::Zero)
  Start-Sleep -Milliseconds 500
}
function TungguHasil {
  $tab = Cari 'TabFinansial'
  for ($i = 0; $i -lt 120 -and -not $tab.Current.IsEnabled; $i++) { Start-Sleep -Milliseconds 500 }
  Start-Sleep -Milliseconds 800
  return $tab.Current.IsEnabled
}
function KeAngka($s) { [double](($s -replace '[^0-9,-]', '') -replace ',', '.') }
function NilaiSetelah($teks, $label) { $m = [regex]::Match($teks, '= ' + [regex]::Escape($label) + ', Nilai = ([^}|]*?) \}'); if ($m.Success) { $m.Groups[1].Value } else { '' } }
function Baris { @((Cari 'Tabel').FindAll($T::Descendants, (Kondisi $A::ControlTypeProperty $CT::DataItem))) }
function Mulai {
  $script:proc = Start-Process $Exe -PassThru
  for ($i = 0; $i -lt 60 -and $script:proc.MainWindowHandle -eq 0; $i++) { Start-Sleep -Milliseconds 500; $script:proc.Refresh() }
  $script:hwnd = $script:proc.MainWindowHandle
  [W]::MoveWindow($script:hwnd, 0, 0, 1360, 900, $true) | Out-Null
  Start-Sleep -Seconds 2
  $script:win = $A::FromHandle($script:hwnd)
}
function Selesai { Stop-Process $script:proc -Force; Start-Sleep -Seconds 1 }
function IsiForm($nama, $lokasi, $luas, $panel, $biaya, $tagihan) {
  Nav 0; Nav 1
  Pilih 'LokasiBox' $lokasi; Isi 'NamaBox' $nama; Isi 'LuasBox' $luas; Pilih 'PanelBox' $panel
  Isi 'BiayaBox' $biaya; Isi 'TagihanBox' $tagihan; Start-Sleep -Milliseconds 500
}
$db = "$env:LOCALAPPDATA\SunCost\suncost.db"
$teksCadangan = 'Open-Meteo tidak dapat dihubungi. Angka di bawah memakai data cadangan rata-rata Indonesia (4,5 kWh/m²/hari) — kurang akurat untuk lokasi ini.'
$cadangan = Join-Path $env:TEMP "suncost-sebelum-uji.db"
# SunCost yang masih terbuka mengunci berkas basis data; tutup dulu sebelum dicadangkan.
$terbuka = @(Get-Process SunCost -ErrorAction SilentlyContinue)
if ($terbuka.Count -gt 0) {
  Write-Output "Menutup $($terbuka.Count) jendela SunCost yang masih terbuka..."
  $terbuka | Stop-Process -Force; $terbuka | Wait-Process -Timeout 10 -ErrorAction SilentlyContinue
}
if (Test-Path $db) {
  Copy-Item $db $cadangan -Force
  # Windows bisa melepas kunci berkas sesaat setelah proses berhenti, jadi coba hapus beberapa kali.
  for ($i = 1; Test-Path $db; $i++) {
    try { Remove-Item $db -Force -ErrorAction Stop }
    catch [System.IO.IOException] {
      if ($i -ge 20) { throw "suncost.db masih dikunci program lain. Tutup SunCost atau DB Browser, lalu jalankan ulang." }
      Start-Sleep -Milliseconds 500
    }
  }
}
try {

# ================================================================ sesi 1: database baru, online
Mulai

# 28. basis data dibuat otomatis, dropdown terisi
$okDb = Test-Path $db
Nav 1
$panel = Butir 'PanelBox'; $tarif = Butir 'TarifBox'
$teksTarif = ($tarif | ForEach-Object { $_.Current.Name }) -join '; '
Catat 28 ($okDb -and $panel.Count -eq 3 -and $tarif.Count -eq 4) "suncost.db dibuat: $okDb; panel $($panel.Count), tarif $($tarif.Count)"
# 12. dropdown tarif
Catat 12 ($tarif.Count -eq 4 -and $teksTarif -match 'Rp') $teksTarif

# 1. Beranda kosong
Nav 0
$judul = Ada 'KosongJudul'
Catat 1 ($judul -and $judul.Current.Name -eq 'Belum ada simulasi') "tampil: $(if ($judul) { $judul.Current.Name })"

# 20. Riwayat kosong
Nav 2
$kosong = @(Nama 'Belum ada simulasi tersimpan').Count -ge 1
$tombolMulai = Nama 'Mulai Simulasi'
Catat 20 ($kosong -and -not (Ada 'Tabel') -and $tombolMulai.Count -ge 1) "keadaan kosong: $([bool]$kosong), tombol Mulai Simulasi: $($tombolMulai.Count)"

# 4. Sidebar
$cek = @()
Nav 0; $cek += [bool]((Ada 'Kosong') -or (Ada 'RiwayatTerakhir'))
Nav 1; $cek += [bool](Ada 'NamaBox')
Nav 2; $cek += [bool]((@(Nama 'Riwayat Simulasi').Count -ge 1) -or (Ada 'Tabel'))
Nav 3; $cek += [bool](Ada 'Tim')
Catat 4 (-not ($cek -contains $false)) "Beranda/Simulasi/Riwayat/Tentang: $($cek -join '/')"

# 27. Tentang
$konst = Teks (Ada 'Konstanta'); $sumber = Teks (Ada 'Sumber')
Catat 27 ($konst -match '0,78' -and $konst -match '0,5% per tahun' -and $sumber -match '0,87 kg/kWh' -and (Teks (Ada 'Tim')) -match 'Sri Wahyuni') "PR 0,78, degradasi 0,5%, emisi 0,87 kg/kWh, tim tampil"

# 3. Mulai Simulasi dari Beranda
Nav 0
Tekan (Nama 'Mulai Simulasi')[0]
Catat 3 ([bool](Ada 'NamaBox')) "halaman Simulasi Baru terbuka"

# 5. Lokasi, jendela tetap responsif
Nav 0; Nav 1
$awal = (Cari 'KoordinatTeks').Current.Name
Pilih 'LokasiBox' 7   # Surabaya
$sw = [Diagnostics.Stopwatch]::StartNew(); $maks = 0
for ($i = 0; $i -lt 6; $i++) {
  $t0 = $sw.ElapsedMilliseconds; $null = (Cari 'KoordinatTeks').Current.Name; $maks = [math]::Max($maks, $sw.ElapsedMilliseconds - $t0)
  Start-Sleep -Milliseconds 300
}
$akhir = (Cari 'KoordinatTeks').Current.Name
Catat 5 ($akhir -ne $awal -and $maks -lt 1000) "koordinat '$awal' -> '$akhir'; respons UI terlama ${maks} ms selama pengambilan data"

# 6. Estimasi langsung 40 m2, mono 550
Isi 'NamaBox' 'Uji'; Isi 'BiayaBox' '15.000'; Isi 'LuasBox' '40'; Pilih 'PanelBox' 1
$est = Teks (Ada 'Estimasi'); $kap = (Ada 'KapasitasTeks').Current.Name
Catat 6 ((NilaiSetelah $est 'Jumlah panel') -eq '11 unit × 550 Wp' -and (NilaiSetelah $est 'Estimasi biaya instalasi') -eq 'Rp 90.750.000') "kapasitas $kap; jumlah panel $(NilaiSetelah $est 'Jumlah panel'); luas terpakai $(NilaiSetelah $est 'Luas terpakai'); biaya $(NilaiSetelah $est 'Estimasi biaya instalasi')"

# 7. Thin film
Pilih 'PanelBox' 2
$est2 = Teks (Ada 'Estimasi'); $kap2 = (Ada 'KapasitasTeks').Current.Name
Catat 7 ((NilaiSetelah $est2 'Jumlah panel') -match '110 Wp' -and $kap2 -ne $kap -and (NilaiSetelah $est2 'Koreksi suhu') -ne (NilaiSetelah $est 'Koreksi suhu')) "kapasitas $kap -> $kap2; $(NilaiSetelah $est2 'Jumlah panel'); koreksi suhu $(NilaiSetelah $est 'Koreksi suhu') -> $(NilaiSetelah $est2 'Koreksi suhu')"
Pilih 'PanelBox' 1

# 8. Nama kosong
Isi 'NamaBox' ''; Isi 'TagihanBox' '300'
$h = Cari 'Hitung'; try { if ($h.Current.IsEnabled) { Tekan $h } } catch [System.Windows.Automation.ElementNotEnabledException] { }  # sudah nonaktif lebih dulu
$g = Ada 'NamaGalat'; $hitungAktif = (Cari 'Hitung').Current.IsEnabled
Catat 8 ($g -and $g.Current.Name -eq 'Nama skenario wajib diisi.' -and -not $hitungAktif -and [bool](Ada 'NamaBox')) "pesan: $(if ($g) { $g.Current.Name }); Hitung aktif: $hitungAktif; tetap di halaman Simulasi"

# 9. Luas 0
Isi 'NamaBox' 'Uji'; Isi 'LuasBox' '0'
$g = Ada 'LuasGalat'; $banner = Ada 'BannerTeks'; $hitungAktif = (Cari 'Hitung').Current.IsEnabled
Catat 9 ($g -and $g.Current.Name -eq 'Luas atap harus lebih dari 0 m².' -and $banner -and -not $hitungAktif) "pesan: $(if ($g) { $g.Current.Name }); banner: $(if ($banner) { $banner.Current.Name }); Hitung aktif: $hitungAktif"

# 10. Biaya kosong
Isi 'LuasBox' '40'; Isi 'BiayaBox' ''
$g = Ada 'BiayaGalat'; $hitungAktif = (Cari 'Hitung').Current.IsEnabled
Catat 10 ($g -and $g.Current.Name -eq 'Biaya per Wp harus diisi.' -and -not $hitungAktif) "pesan: $(if ($g) { $g.Current.Name }); Hitung aktif: $hitungAktif"

# 11. Tagihan 0
Isi 'BiayaBox' '15.000'; Isi 'TagihanBox' '0'
$g = Ada 'TagihanGalat'
Catat 11 ($g -and $g.Current.Name -eq 'Tagihan harus lebih dari 0 kWh.') "pesan: $(if ($g) { $g.Current.Name })"
Isi 'TagihanBox' '300'

# 14. Tarif manual kosong
(Cari 'TarifManualCek').GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle(); Start-Sleep -Milliseconds 400
Isi 'TarifManualBox' ''
$h = Cari 'Hitung'; try { if ($h.Current.IsEnabled) { Tekan $h } } catch [System.Windows.Automation.ElementNotEnabledException] { }  # sudah nonaktif lebih dulu
$g = Ada 'TarifManualGalat'; $hitungAktif = (Cari 'Hitung').Current.IsEnabled
Catat 14 ($g -and $g.Current.Name -eq 'Tarif per kWh harus lebih dari 0.' -and -not $hitungAktif -and [bool](Ada 'NamaBox')) "pesan: $(if ($g) { $g.Current.Name }); Hitung aktif: $hitungAktif; tetap di halaman Simulasi"

# 13. Tarif manual 2.000 (hasil dibandingkan dengan tarif PLN 1.444,70 pada kasus 15)
Isi 'TarifManualBox' '2.000'
$dropdownAktif = (Cari 'TarifBox').Current.IsEnabled; $panelManual = [bool](Ada 'TarifManualBox')
Isi 'NamaBox' 'Uji Tarif Manual'
Tekan (Cari 'Hitung'); $okManual = TungguHasil
(Cari 'TabFinansial').GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select(); Start-Sleep -Milliseconds 600
$hematManual = KeAngka (NilaiSetelah (Teks (Ada 'KpiFinansial')) 'Hemat per bulan')

# 15. Hitung valid (tarif PLN R-1/1.300 VA, input lain sama)
IsiForm 'Rumah Surabaya' 7 '40' 1 '15.000' '300'
Tekan (Cari 'Hitung'); $memuat = [bool](Ada 'BannerMemuat'); $okHasil = TungguHasil
$ringkasan = (Cari 'Ringkasan').Current.Name
Catat 15 ($okHasil -and $ringkasan -match 'Rumah Surabaya') "halaman Hasil: '$ringkasan'; keadaan memuat terlihat: $memuat"

# 16. Tab Energi
$prod = (Cari 'ProduksiTahunan').Current.Name
$batang = @((Cari 'Batang').FindAll($T::Children, [System.Windows.Automation.Condition]::TrueCondition)).Count
$asumsi = Teks (Ada 'Asumsi')
Catat 16 ($prod -match '\d' -and $batang -eq 12 -and $asumsi -match '0,78' -and (Ada 'RataBulanan') -and (Ada 'SetaraKonsumsi')) "produksi $prod kWh/tahun, rata-rata $((Ada 'RataBulanan').Current.Name), setara $((Ada 'SetaraKonsumsi').Current.Name), $batang batang grafik, PR 0,78 tampil"

# 17. Tab Finansial & Lingkungan
(Cari 'TabFinansial').GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select(); Start-Sleep -Milliseconds 600
$kpi = Teks (Ada 'KpiFinansial')
$hematPln = KeAngka (NilaiSetelah $kpi 'Hemat per bulan')
$pohon = (Ada 'SetaraPohon').Current.Name; $km = (Ada 'SetaraKmMobil').Current.Name; $emisi = (Ada 'EmisiTahunan').Current.Name
Catat 17 ($kpi -match 'Biaya instalasi' -and $kpi -match 'Keuntungan bersih 25 tahun' -and (Ada 'RentangArusKas').Current.Name -match 'hingga' -and $pohon -match 'pohon' -and $km -match 'km') "balik modal $(NilaiSetelah $kpi 'Balik modal'); untung 25 thn $(NilaiSetelah $kpi 'Keuntungan bersih 25 tahun'); $emisi; $pohon; $km"

$rasio = if ($hematPln -gt 0) { $hematManual / $hematPln } else { 0 }
Catat 13 ($okManual -and -not $dropdownAktif -and $panelManual -and [math]::Abs($rasio - 2000 / 1444.70) -lt 0.01) ([string]::Format([Globalization.CultureInfo]'id-ID', "dropdown golongan nonaktif: {0}; hemat/bulan tarif 2.000 = Rp {1:N0} vs tarif 1.444,70 = Rp {2:N0} (rasio {3:N3}, seharusnya {4:N3})", (-not $dropdownAktif), $hematManual, $hematPln, $rasio, (2000 / 1444.70)))

# data untuk Beranda dan Riwayat
IsiForm 'Rumah Kupang' 11 '50' 0 '15.000' '450'; Tekan (Cari 'Hitung'); $null = TungguHasil
IsiForm 'Rumah Sleman' 0 '40' 1 '15.000' '350'; Tekan (Cari 'Hitung'); $null = TungguHasil

# 2. Beranda berisi simulasi
Nav 0
$kartu = Teks (Ada 'RiwayatTerakhir')
$jumlahKartu = @([regex]::Matches($kartu, 'Rumah \w+') | ForEach-Object Value | Select-Object -Unique).Count
Catat 2 ($jumlahKartu -eq 3 -and $kartu -match 'Rumah Sleman' -and $kartu -match 'kWp' -and $kartu -notmatch '\| - \|') "$jumlahKartu kartu: $((@([regex]::Matches($kartu, 'Rumah \w+') | ForEach-Object Value | Select-Object -Unique)) -join ', ')"

# 21. Daftar riwayat
Nav 2
$baris = Baris; $teksTabel = Teks (Cari 'Tabel')
Catat 21 ($baris.Count -eq 4 -and $teksTabel -match 'm²' -and $teksTabel -match 'kWp' -and $teksTabel -notmatch '\| - \|' -and ($baris[0] | ForEach-Object { Teks $_ }) -match 'Rumah Sleman') "$($baris.Count) baris, terbaru di atas: $(((Teks $baris[0]) -split ' \| ' | Where-Object { $_ -and $_ -notmatch 'SunCost\.|Column|Item:' } | Select-Object -Unique) -join ', ')"

# 22. Lihat hasil dari riwayat
Tekan (Nama 'Lihat hasil skenario')[1]
$okLihat = TungguHasil
$ring = (Cari 'Ringkasan').Current.Name
(Cari 'TabFinansial').GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select(); Start-Sleep -Milliseconds 500
Catat 22 ($okLihat -and $ring -match 'Rumah Kupang' -and (Ada 'KpiFinansial')) "terbuka: '$ring'; tab Finansial bisa dibuka"

# 23. Bandingkan 3 skenario
Nav 0; Nav 2
$cb = @((Cari 'Tabel').FindAll($T::Descendants, (Kondisi $A::ControlTypeProperty $CT::CheckBox)))
for ($i = 0; $i -lt 3; $i++) { Klik $cb[$i] }
$tombol = Cari 'Bandingkan'; $aktif = $tombol.Current.IsEnabled
Tekan $tombol
$banding = Teks (Ada 'KartuPerbandingan')
Catat 23 ($aktif -and $banding -match 'Balik modal tercepat' -and ([regex]::Matches($banding, 'Untung 25 thn \| Rp')).Count -eq 3) "tombol '$($tombol.Current.Name)' aktif: $aktif; penanda balik modal tercepat dan untung 25 thn tampil di 3 kartu"

# 24. Skenario keempat ditolak
Klik $cb[3]
$petunjuk = (Cari 'Petunjuk').Current.Name
$status4 = $cb[3].GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Current.ToggleState
Catat 24 ($petunjuk -match 'Maksimal 3' -and "$status4" -eq 'Off') "pesan: $petunjuk; centang keempat: $status4"

# 25. Hapus lalu Batal
$sebelum = (Baris).Count
Tekan (Nama 'Hapus skenario')[0]
$overlay = [bool](Ada 'PesanHapus'); $pesan = (Ada 'PesanHapus').Current.Name
Tekan (Cari 'BatalHapus')
Catat 25 ($overlay -and -not (Ada 'PesanHapus') -and (Baris).Count -eq $sebelum) "dialog: '$pesan'; setelah Batal jumlah baris tetap $sebelum"

# 26. Hapus lalu konfirmasi (skenario "Uji Tarif Manual", baris terakhir)
$hapusBtn = Nama 'Hapus skenario'
Tekan $hapusBtn[$hapusBtn.Count - 1]
Tekan (Nama 'Hapus' | Where-Object { $_.Current.ControlType -eq $CT::Button })[0]
$setelah = (Baris).Count; $status = (Cari 'Status').Current.Name
Selesai

# ================================================================ sesi 2: dibuka ulang, tanpa internet
# Internet dimatikan hanya untuk proses SunCost lewat proxy ke port yang tidak melayani.
$env:HTTPS_PROXY = 'http://127.0.0.1:9'; $env:HTTP_PROXY = 'http://127.0.0.1:9'
Mulai
Remove-Item Env:HTTPS_PROXY, Env:HTTP_PROXY

Nav 2
$teksTabel = Teks (Cari 'Tabel')
Catat 26 ($setelah -eq 3 -and (Baris).Count -eq 3 -and $teksTabel -notmatch 'Uji Tarif Manual') "pesan: '$status'; setelah dibuka ulang $((Baris).Count) baris, 'Uji Tarif Manual' tidak ada"

# 18. Data cadangan: kota yang belum pernah disimulasikan
IsiForm 'Rumah Medan' 10 '40' 1 '15.000' '300'
Tekan (Cari 'Hitung'); $okC = TungguHasil
$bannerC = @(Nama $teksCadangan)[0]
Catat 18 ($okC -and $bannerC) "hasil tampil: $okC; banner: $(if ($bannerC) { $bannerC.Current.Name })"

# 19. Cache: kota yang sudah disimulasikan hari ini
IsiForm 'Rumah Surabaya 2' 7 '40' 1 '15.000' '300'
$sw = [Diagnostics.Stopwatch]::StartNew()
Tekan (Cari 'Hitung'); $okCache = TungguHasil; $ms = $sw.ElapsedMilliseconds
$bannerCache = @(Nama $teksCadangan)[0]
$prod2 = (Cari 'ProduksiTahunan').Current.Name
Catat 19 ($okCache -and -not $bannerCache -and $prod2 -eq $prod) "hasil dalam $ms ms tanpa banner cadangan; produksi $prod2 sama dengan simulasi online ($prod)"
Selesai

}
finally {
  Get-Process SunCost -ErrorAction SilentlyContinue | Stop-Process -Force
  Remove-Item Env:HTTPS_PROXY, Env:HTTP_PROXY -ErrorAction SilentlyContinue
  Start-Sleep -Seconds 1
  if (Test-Path $cadangan) { Copy-Item $cadangan $db -Force; Remove-Item $cadangan } else { Remove-Item $db -ErrorAction SilentlyContinue }
}

$hasil | ConvertTo-Json -Depth 3 | Set-Content -Encoding UTF8 $OutJson
$lulus = @($hasil.Values | Where-Object { $_.ok }).Count
Write-Output "`n$lulus dari $($hasil.Count) kasus otomatis Sesuai. Hasil: $OutJson"
