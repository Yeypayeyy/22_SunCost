using System;

namespace SunCost.Models;

/// <summary>Tarif tenaga listrik PLN untuk satu golongan pelanggan.</summary>
public class TarifListrikPLN
{
    private string golongan;        // contoh: "R-1/TR 1.300 VA"
    private double tarifPerKWh;     // rupiah
    private DateTime tanggalBerlaku;

    public TarifListrikPLN(string golongan, double tarifPerKWh, DateTime tanggalBerlaku)
    {
        this.golongan = golongan;
        this.tarifPerKWh = tarifPerKWh;
        this.tanggalBerlaku = tanggalBerlaku;
    }

    public double getTarifPerKWh() => tarifPerKWh;

    /// <summary>Biaya listrik dalam rupiah untuk pemakaian sejumlah kWh.</summary>
    public double hitungBiaya(double kWh) => kWh * tarifPerKWh;
}
