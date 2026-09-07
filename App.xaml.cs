using System.Windows;
using SunCost.Services;

namespace SunCost;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        SelfCheck.Jalankan(); // hanya aktif di build DEBUG
    }
}
