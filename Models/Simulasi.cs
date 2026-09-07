using System;
using SunCost.Services;

namespace SunCost.Models;

/// <summary>Satu skenario pemasangan yang disimulasikan dan disimpan ke riwayat.</summary>
public class Simulasi
{
    public int Id { get; set; }
    public DateTime Tanggal { get; set; } = DateTime.Now;
    public string Catatan { get; set; } = "";

    public Lokasi Lokasi { get; set; } = new();
    public Atap Atap { get; set; } = new();
    public PanelSurya Panel { get; set; } = PanelSurya.Standar();
    public TarifListrik Tarif { get; set; } = TarifListrik.R1_1300VA();
    public DataIklim Iklim { get; set; } = DataIklim.Cadangan();

    public HasilSimulasi? Hasil { get; set; }

    /// <summary>Menjalankan perhitungan dan menyimpan keluarannya pada <see cref="Hasil"/>.</summary>
    public HasilSimulasi Hitung(KalkulatorPlts kalkulator)
    {
        Hasil = kalkulator.Hitung(this);
        return Hasil;
    }
}
