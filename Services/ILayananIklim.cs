using System.Threading;
using System.Threading.Tasks;
using SunCost.Models;

namespace SunCost.Services;

/// <summary>Sumber data radiasi dan suhu untuk sebuah lokasi.</summary>
public interface ILayananIklim
{
    Task<DataIklim> AmbilAsync(Lokasi lokasi, CancellationToken batal = default);
}
