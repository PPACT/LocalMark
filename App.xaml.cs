using System.Windows;
using DryIoc;
using LocalMark.Repository;
using LocalMark.ViewModel;

namespace LocalMark;

public partial class App : Application
{
    public static IContainer Container { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DbInitializer.Initialize();

        Container = new Container(rules => rules.WithDefaultReuse(Reuse.Singleton));
        Container.Register<SourceRepo>();
        Container.Register<MarkRepo>();
        Container.Register<MainViewModel>();
        Container.Register<SettingsViewModel>();

        var main = Container.Resolve<MainViewModel>();
        var window = new MainWindow { DataContext = main };
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Container.Dispose();
        base.OnExit(e);
    }
}
