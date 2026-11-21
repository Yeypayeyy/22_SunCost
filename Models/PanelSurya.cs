using System;

namespace SunCost.Models;

/// <summary>
/// Spesifikasi satu modul surya. Class dasar: tiap teknologi sel (monokristalin, thin film, ...)
/// mewarisi data dan rumus di sini, lalu menimpa bagian yang memang berbeda secara fisik.
/// </summary>
public abstract class PanelSurya
{
    /// <summary>Suhu uji standar (STC) panel, derajat Celsius.</summary>
    protected const double SuhuReferensi = 25.0;

    // Encapsulation: data panel tertutup dari luar. Subclass boleh membaca (protected),
    // kode di luar hierarki hanya lewat method publik di bawah.
    protected string merk;
    protected double kapasitasWp;     // daya puncak satu panel
    protected double efisiensi;       // persen
    protected double koefisienSuhu;   // penurunan daya (persen) per derajat di atas 25 C
    protected double luasPerPanel;    // m2
    protected double hargaPerUnit;    // rupiah

    protected PanelSurya(string merk, double kapasitasWp, double efisiensi, double koefisienSuhu,
                         double luasPerPanel, double hargaPerUnit)
    {
        this.merk = merk;
        this.kapasitasWp = kapasitasWp;
        this.efisiensi = efisiensi;
        this.koefisienSuhu = koefisienSuhu;
        this.luasPerPanel = luasPerPanel;
        this.hargaPerUnit = hargaPerUnit;
    }

    /// <summary>Nama teknologi sel, wajib diisi tiap turunan.</summary>
    public abstract string Jenis { get; }

    public string Merk => merk;

    /// <summary>Jumlah panel utuh yang muat pada luas atap tertentu.</summary>
    public int hitungJumlahPanel(double luasAtap) =>
        luasPerPanel > 0 ? (int)Math.Floor(luasAtap / luasPerPanel) : 0;

    /// <summary>Kapasitas total dalam Wp.</summary>
    public double hitungKapasitasTotal(int jumlah) => jumlah * kapasitasWp;

    /// <summary>
    /// Faktor pengali daya akibat panas sel: 1,0 pada suhu referensi, turun linear sebesar
    /// koefisien suhu per derajat. Panel kristalin memakai rumus ini; teknologi lain menimpanya.
    /// </summary>
    public virtual double hitungKoreksiSuhu(double suhuSel) =>
        Math.Max(0.3, 1 - Math.Max(0, suhuSel - SuhuReferensi) * koefisienSuhu / 100);
}
