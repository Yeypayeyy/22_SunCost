namespace SunCost.Models;

/// <summary>
/// Panel silikon monokristalin, jenis yang paling umum dipasang di atap rumah.
/// Rugi panasnya linear, jadi rumus koreksi suhu dari class dasar sudah tepat.
/// </summary>
public class PanelMonokristal : PanelSurya
{
    public PanelMonokristal(string merk, double kapasitasWp, double efisiensi, double koefisienSuhu,
                            double luasPerPanel, double hargaPerUnit)
        : base(merk, kapasitasWp, efisiensi, koefisienSuhu, luasPerPanel, hargaPerUnit)
    {
    }

    public override string Jenis => "Monokristalin";
}
