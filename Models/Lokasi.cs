using System.Globalization;

namespace SunCost.Models;

/// <summary>Titik lokasi beserta bidang atap yang akan dipasangi panel.</summary>
public class Lokasi
{
    /// <summary>Bagian luas atap yang benar-benar terpakai panel; sisanya jarak antar-larik dan jalur perawatan.</summary>
    private const double FaktorPemanfaatan = 0.75;

    private string nama;
    private double latitude;
    private double longitude;
    private double luasAtap;    // m2
    private double orientasi;   // azimut dalam derajat: 0 = utara, 90 = timur, 180 = selatan
    private double kemiringan;  // derajat terhadap bidang datar

    public Lokasi(string nama, double latitude, double longitude, double luasAtap, double orientasi, double kemiringan)
    {
        this.nama = nama;
        this.latitude = latitude;
        this.longitude = longitude;
        this.luasAtap = luasAtap;
        this.orientasi = orientasi;
        this.kemiringan = kemiringan;
    }

    public bool validasi() =>
        !string.IsNullOrWhiteSpace(nama)
        && latitude is >= -90 and <= 90
        && longitude is >= -180 and <= 180
        && luasAtap > 0
        && orientasi is >= 0 and < 360
        && kemiringan is >= 0 and <= 90;

    /// <summary>Koordinat "lat,lon" bertitik desimal, siap dipakai sebagai parameter API cuaca.</summary>
    public string getKoordinat() =>
        latitude.ToString("0.####", CultureInfo.InvariantCulture) + "," +
        longitude.ToString("0.####", CultureInfo.InvariantCulture);

    public double hitungLuasEfektif() => luasAtap * FaktorPemanfaatan;
}
