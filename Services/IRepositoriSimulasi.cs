using System.Collections.Generic;
using SunCost.Models;

namespace SunCost.Services;

/// <summary>Kontrak penyimpanan skenario. Setiap database punya implementasinya sendiri.</summary>
public interface IRepositoriSimulasi
{
    string Jenis { get; }
    bool simpan(SimulasiSkenario s);
    List<SimulasiSkenario> ambilSemua();
    SimulasiSkenario? ambilById(string id);
    bool hapus(string id);
}