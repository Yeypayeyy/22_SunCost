using System;

namespace SunCost.Models;

/// <summary>Bidang atap yang akan dipasangi panel.</summary>
public class Atap
{
    /// <summary>Bagian luas atap yang benar-benar terpakai panel (sisanya jarak antar-larik dan jalur perawatan).</summary>
    public const double FaktorPemanfaatan = 0.75;

    public double LuasM2 { get; set; }
    public Arah Orientasi { get; set; } = Arah.Utara;
    public double KemiringanDerajat { get; set; } = 15;

    public double LuasEfektifM2 => LuasM2 * FaktorPemanfaatan;

    /// <summary>
    /// Faktor koreksi radiasi akibat orientasi dan kemiringan, relatif terhadap bidang optimal (1,0).
    /// Pendekatan kosinus: makin jauh azimut dari arah optimal, makin besar rugi radiasinya.
    /// </summary>
    public double FaktorOrientasi(Lokasi lokasi)
    {
        // Selisih sudut azimut terhadap arah optimal, 0..180 derajat.
        int beda = Math.Abs((int)Orientasi - (int)lokasi.ArahOptimal);
        if (beda > 4) beda = 8 - beda;
        double selisihAzimut = beda * 45.0;

        // Rugi azimut maksimum ~25% saat menghadap berlawanan; atap datar hampir tidak terpengaruh azimut.
        double bobotKemiringan = Math.Min(KemiringanDerajat, 40) / 40.0;
        double rugiAzimut = 0.25 * bobotKemiringan * (1 - Math.Cos(selisihAzimut * Math.PI / 180));

        // Kemiringan optimal di Indonesia mendekati |lintang|, tetapi minimal 10 derajat agar air hujan mencuci debu.
        double kemiringanOptimal = Math.Max(10, Math.Abs(lokasi.Latitude));
        double rugiKemiringan = 0.003 * Math.Abs(KemiringanDerajat - kemiringanOptimal);

        return Math.Clamp(1 - rugiAzimut - rugiKemiringan, 0.5, 1.0);
    }
}
