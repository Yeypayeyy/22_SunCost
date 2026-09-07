namespace SunCost.Models;

/// <summary>Titik lokasi atap yang disimulasikan.</summary>
public class Lokasi
{
    public string Nama { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>Belahan bumi menentukan arah optimal panel (utara/selatan).</summary>
    public Arah ArahOptimal => Latitude < 0 ? Arah.Utara : Arah.Selatan;

    public override string ToString() => Nama;
}
