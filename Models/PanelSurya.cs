namespace SunCost.Models;

/// <summary>Spesifikasi satu tipe modul surya beserta harga pasangnya.</summary>
public class PanelSurya
{
    public string Model { get; set; } = "";
    public int DayaWp { get; set; }
    public double EfisiensiPersen { get; set; }
    public double LuasPerPanelM2 { get; set; }

    /// <summary>Biaya terpasang per Wp (panel, inverter, struktur, jasa) dalam rupiah.</summary>
    public decimal HargaPerWp { get; set; }

    /// <summary>Penurunan daya per derajat di atas 25 C, dalam persen. Nilai umum silikon: 0,4.</summary>
    public double KoefisienSuhuPersen { get; set; } = 0.4;

    /// <summary>Preset yang dipakai kalau pengguna belum memilih panel tertentu.</summary>
    public static PanelSurya Standar() => new()
    {
        Model = "Monokristalin 550 Wp",
        DayaWp = 550,
        EfisiensiPersen = 21.0,
        LuasPerPanelM2 = 2.6,
        HargaPerWp = 14_000m
    };

    public int JumlahPanelMuat(double luasEfektifM2) => (int)(luasEfektifM2 / LuasPerPanelM2);
}
