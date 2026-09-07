namespace SunCost.Models;

/// <summary>Rangkuman iklim satu lokasi, hasil pembacaan API cuaca.</summary>
public class DataIklim
{
    /// <summary>Peak Sun Hours: radiasi harian (kWh/m2) yang setara jam penyinaran pada 1.000 W/m2.</summary>
    public double PeakSunHours { get; set; }

    /// <summary>Suhu udara rata-rata harian, dipakai untuk koreksi penurunan efisiensi.</summary>
    public double SuhuRataRataC { get; set; }

    /// <summary>Nilai cadangan saat API tidak dapat dihubungi: rata-rata kasar wilayah Indonesia.</summary>
    public static DataIklim Cadangan() => new() { PeakSunHours = 4.5, SuhuRataRataC = 27.0 };
}
