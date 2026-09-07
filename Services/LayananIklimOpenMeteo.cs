using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using SunCost.Models;

namespace SunCost.Services;

/// <summary>
/// Membaca rata-rata iklim satu tahun terakhir dari Open-Meteo (gratis, tanpa API key).
/// shortwave_radiation_sum (MJ/m2/hari) dikonversi ke Peak Sun Hours, dan
/// temperature_2m_mean dipakai untuk koreksi penurunan efisiensi akibat suhu.
/// </summary>
public class LayananIklimOpenMeteo : ILayananIklim
{
    private const string Endpoint = "https://archive-api.open-meteo.com/v1/archive";
    private const double MjPerM2KeKwh = 1 / 3.6;

    private readonly HttpClient _http;

    public LayananIklimOpenMeteo(HttpClient? http = null) =>
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(20) };

    public async Task<DataIklim> AmbilAsync(Lokasi lokasi, CancellationToken batal = default)
    {
        var sampai = DateTime.UtcNow.Date.AddDays(-7); // arsip tertinggal beberapa hari
        var dari = sampai.AddYears(-1);

        string url = $"{Endpoint}?latitude={Angka(lokasi.Latitude)}&longitude={Angka(lokasi.Longitude)}" +
                     $"&start_date={dari:yyyy-MM-dd}&end_date={sampai:yyyy-MM-dd}" +
                     "&daily=shortwave_radiation_sum,temperature_2m_mean&timezone=auto";

        var balasan = await _http.GetFromJsonAsync<BalasanOpenMeteo>(url, batal);
        var harian = balasan?.Daily;
        if (harian?.Shortwave_radiation_sum is null || harian.Temperature_2m_mean is null)
            return DataIklim.Cadangan();

        double? radiasi = RataRata(harian.Shortwave_radiation_sum);
        double? suhu = RataRata(harian.Temperature_2m_mean);
        if (radiasi is null || suhu is null) return DataIklim.Cadangan();

        return new DataIklim
        {
            PeakSunHours = radiasi.Value * MjPerM2KeKwh,
            SuhuRataRataC = suhu.Value
        };
    }

    // Hari tanpa pengukuran dikirim sebagai null; dilewati supaya tidak menarik rata-rata ke bawah.
    private static double? RataRata(double?[] nilai)
    {
        var ada = nilai.Where(v => v.HasValue).Select(v => v!.Value).ToArray();
        return ada.Length == 0 ? null : ada.Average();
    }

    private static string Angka(double v) => v.ToString(CultureInfo.InvariantCulture);

    private class BalasanOpenMeteo
    {
        public HarianOpenMeteo? Daily { get; set; }
    }

    private class HarianOpenMeteo
    {
        public double?[]? Shortwave_radiation_sum { get; set; }
        public double?[]? Temperature_2m_mean { get; set; }
    }
}
