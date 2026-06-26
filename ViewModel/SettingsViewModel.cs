using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalMark.Helper;
using LocalMark.Model;
using Microsoft.Win32;

namespace LocalMark.ViewModel;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private string _outputDir = "";

    public SettingsViewModel()
    {
        OutputDir = SettingsManager.Load().OutputDir;
    }

    [RelayCommand]
    private void BrowseOutputDir()
    {
        var dlg = new OpenFolderDialog { Title = "选择标注结果存放目录" };
        if (dlg.ShowDialog() == true) OutputDir = dlg.FolderName;
    }

    [RelayCommand]
    private void Save()
    {
        SettingsManager.Save(new AppSettings { OutputDir = OutputDir.Trim() });
        CloseWindow();
    }

    [RelayCommand]
    private void CloseWindow()
    {
        System.Windows.Application.Current.Windows
            .OfType<System.Windows.Window>()
            .FirstOrDefault(w => w.DataContext == this)?.Close();
    }
}
