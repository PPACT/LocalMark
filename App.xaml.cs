using System.Windows;
using LocalMark.Repository;

namespace LocalMark;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DbInitializer.Initialize();
    }
}
