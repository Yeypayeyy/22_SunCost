using System.Collections.Generic;
using SunCost.Models;

namespace SunCost.Services;

/// <summary>Penyimpanan riwayat simulasi.</summary>
public interface IRiwayatRepository
{
    int Simpan(Simulasi simulasi);
    IReadOnlyList<Simulasi> AmbilSemua();
    void Hapus(int id);
}
