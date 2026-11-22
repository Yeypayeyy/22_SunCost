using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SunCost.Models;

namespace SunCost.Services;

/// <summary>
/// Mengambil data iklim satu tahun terakhir dari Open-Meteo Archive API (gratis, tanpa API key).
/// shortwave_radiation_sum (MJ/m2/hari) dibagi 3,6 menjadi Peak Sun Hours (kWh/m2/hari).
/// Data yang pernah diambil untuk koordinat yang sama dan belum lebih dari 30 hari dibaca dari database.
/// </summary>
public class LayananCuacaAPI
{
    /// <summary>Batas umur data radiasi tersimpan sebelum diambil ulang dari API.</summary>
    public static readonly TimeSpan UmurCache = TimeSpan.FromDays(30);

    // Satu HttpClient untuk seluruh aplikasi supaya socket tidak habis. Batas waktu diatur per permintaan.
    private static readonly HttpClient httpClient = new() { Timeout = Timeout.InfiniteTimeSpan };

    private string baseUrl;
    private int timeoutDetik;
    private readonly RepositoriSimulasi? cache;

    /// <summary>Tanpa <paramref name="cache"/>, setiap pemanggilan langsung ke API.</summary>
    public LayananCuacaAPI(RepositoriSimulasi? cache = null,
                           string baseUrl = "https://archive-api.open-meteo.com/v1/archive", int timeoutDetik = 20)
    {
        this.cache = cache;
        this.baseUrl = baseUrl;
        this.timeoutDetik = timeoutDetik;
    }

    /// <summary>Bentuk sinkron sesuai class diagram. Jangan dipanggil dari thread UI; pakai versi async.</summary>
    public DataRadiasi ambilDataRadiasi(Lokasi lokasi) => ambilDataRadiasiAsync(lokasi).GetAwaiter().GetResult();

    /// <summary>
    /// Rata-rata Peak Sun Hours per bulan dan suhu rata-rata setahun terakhir. Kalau API tidak dapat
    /// dihubungi, dikembalikan nilai cadangan rata-rata Indonesia supaya simulasi tetap berjalan.
    /// </summary>
    public async Task<DataRadiasi> ambilDataRadiasiAsync(Lokasi lokasi)
    {
        if (BacaCache(lokasi) is { } tersimpan)
            return tersimpan;

        var sampai = DateTime.UtcNow.Date.AddDays(-7); // arsip tertinggal beberapa hari
        var dari = sampai.AddYears(-1).AddDays(1);
        string[] koordinat = lokasi.getKoordinat().Split(',');
        string url = $"{baseUrl}?latitude={koordinat[0]}&longitude={koordinat[1]}" +
                     $"&start_date={dari:yyyy-MM-dd}&end_date={sampai:yyyy-MM-dd}" +
                     "&daily=shortwave_radiation_sum,temperature_2m_mean&timezone=auto";
        try
        {
            using var batas = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutDetik));
            string json = await httpClient.GetStringAsync(url, batas.Token).ConfigureAwait(false);
            using var dokumen = JsonDocument.Parse(json);
            var harian = dokumen.RootElement.GetProperty("daily");
            var waktu = harian.GetProperty("time");
            var radiasi = harian.GetProperty("shortwave_radiation_sum");
            var suhu = harian.GetProperty("temperature_2m_mean");

            var totalBulan = new double[12];
            var hariBulan = new int[12];
            double totalSuhu = 0;
            int hariSuhu = 0;

            for (int i = 0; i < waktu.GetArrayLength(); i++)
            {
                // Hari tanpa pengukuran dikirim sebagai null; dilewati supaya tidak menarik rata-rata ke bawah.
                if (radiasi[i].ValueKind == JsonValueKind.Number)
                {
                    int bulan = DateTime.ParseExact(waktu[i].GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture).Month - 1;
                    totalBulan[bulan] += radiasi[i].GetDouble() / 3.6;
                    hariBulan[bulan]++;
                }
                if (suhu[i].ValueKind == JsonValueKind.Number)
                {
                    totalSuhu += suhu[i].GetDouble();
                    hariSuhu++;
                }
            }

            if (hariBulan.Any(h => h == 0) || hariSuhu == 0)
                return DataCadangan();

            var bulanan = Enumerable.Range(0, 12).Select(b => totalBulan[b] / hariBulan[b]).ToList();
            return new DataRadiasi(bulanan, totalSuhu / hariSuhu, "Open-Meteo Archive API", DateTime.Now);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException
                                      or KeyNotFoundException or InvalidOperationException)
        {
            return DataCadangan();
        }
    }

    /// <summary>True kalau server API dapat dijangkau (apa pun status HTTP selain galat server).</summary>
    public bool cekKoneksi()
    {
        try
        {
            using var batas = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutDetik));
            using var permintaan = new HttpRequestMessage(HttpMethod.Head, baseUrl);
            using var balasan = httpClient.Send(permintaan, batas.Token);
            return (int)balasan.StatusCode < 500;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// Data radiasi tersimpan yang masih berlaku, atau null. Database yang gagal dibaca tidak boleh
    /// menghentikan simulasi, jadi kegagalan diperlakukan sebagai "tidak ada cache".
    /// </summary>
    private DataRadiasi? BacaCache(Lokasi lokasi)
    {
        if (cache is null) return null;
        try
        {
            return cache.cariDataRadiasi(lokasi.Latitude, lokasi.Longitude, DateTime.Now - UmurCache);
        }
        catch (Exception e) when (e is SqliteException or InvalidOperationException or FormatException or ArgumentException)
        {
            return null;
        }
    }

    private static DataRadiasi DataCadangan() =>
        new(Enumerable.Repeat(4.5, 12).ToList(), 27.0, "Cadangan (rata-rata Indonesia)", DateTime.Now);
}
