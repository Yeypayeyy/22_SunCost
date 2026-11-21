using System;

namespace SunCost.Models;

/// <summary>
/// Panel thin film (a-Si). Lebih tahan panas daripada kristalin: sebagian rugi panas pulih
/// lewat thermal annealing, sehingga koreksi suhunya tidak linear seperti class dasar.
/// </summary>
public class PanelThinFilm : PanelSurya
{
    /// <summary>Suhu sel saat annealing mulai terasa, derajat Celsius.</summary>
    private const double SuhuAnnealing = 40.0;

    public PanelThinFilm(string merk, double kapasitasWp, double efisiensi, double koefisienSuhu,
                         double luasPerPanel, double hargaPerUnit)
        : base(merk, kapasitasWp, efisiensi, koefisienSuhu, luasPerPanel, hargaPerUnit)
    {
    }

    public override string Jenis => "Thin film (a-Si)";

    /// <summary>Rumus kristalin, lalu ditambah pemulihan annealing yang dibatasi 3%.</summary>
    public override double hitungKoreksiSuhu(double suhuSel)
    {
        // ponytail: pemulihan annealing didekati linear 0,1% per derajat di atas 40 C dan dibatasi 3%.
        // Ganti dengan kurva dari datasheet panel kalau nanti ada data ukurnya.
        double annealing = Math.Min(0.03, Math.Max(0, suhuSel - SuhuAnnealing) * 0.001);
        return Math.Min(1.0, base.hitungKoreksiSuhu(suhuSel) + annealing);
    }
}
