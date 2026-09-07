using System.Globalization;

namespace SunCost.Models;

/// <summary>
/// Format angka Indonesia yang didefinisikan eksplisit, bukan lewat CultureInfo("id-ID"),
/// supaya hasilnya sama di mesin mana pun (termasuk mode globalization-invariant).
/// </summary>
public static class FormatId
{
    public static readonly NumberFormatInfo Angka = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
        NumberGroupSizes = new[] { 3 }
    };
}
