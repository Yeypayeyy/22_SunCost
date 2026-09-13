using System;

namespace SunCost.Models;

/// <summary>Spesifikasi satu tipe modul surya.</summary>
public class PanelSurya
{
    private string merk;
    private double kapasitasWp;     // daya puncak satu panel
    private double efisiensi;       // persen
    private double koefisienSuhu;   // penurunan daya (persen) per derajat di atas 25 C
    private double luasPerPanel;    // m2
    private double hargaPerUnit;    // rupiah

    public PanelSurya(string merk, double kapasitasWp, double efisiensi, double koefisienSuhu,
                      double luasPerPanel, double hargaPerUnit)
    {
        this.merk = merk;
        this.kapasitasWp = kapasitasWp;
        this.efisiensi = efisiensi;
        this.koefisienSuhu = koefisienSuhu;
        this.luasPerPanel = luasPerPanel;
        this.hargaPerUnit = hargaPerUnit;
    }

    /// <summary>Dibaca KalkulatorEnergi untuk koreksi suhu.</summary>
    public double KoefisienSuhu => koefisienSuhu;

    /// <summary>Jumlah panel utuh yang muat pada luas atap tertentu.</summary>
    public int hitungJumlahPanel(double luasAtap) =>
        luasPerPanel > 0 ? (int)Math.Floor(luasAtap / luasPerPanel) : 0;

    /// <summary>Kapasitas total dalam Wp.</summary>
    public double hitungKapasitasTotal(int jumlah) => jumlah * kapasitasWp;
}
