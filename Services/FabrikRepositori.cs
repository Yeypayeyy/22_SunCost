using System;

namespace SunCost.Services;

public static class FabrikRepositori
{
    /// <summary>PostgreSQL bila variabel lingkungan SUNCOST_PG terisi, selain itu SQLite lokal.</summary>
    public static IRepositoriSimulasi Buat()
    {
        string? koneksi = Environment.GetEnvironmentVariable("SUNCOST_PG");
        return string.IsNullOrWhiteSpace(koneksi)
            ? new RepositoriSimulasi()
            : new RepositoriSimulasiPostgres(koneksi);
    }
}