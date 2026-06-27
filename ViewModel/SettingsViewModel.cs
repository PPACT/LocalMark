using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalMark.Helper;
using LocalMark.Model;
using Microsoft.Win32;

namespace LocalMark.ViewModel;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private string _outputDir = "";
    [ObservableProperty] private string _exportFormat = "LocalMark";
    [ObservableProperty] private string _ollamaHost = "";
    [ObservableProperty] private string _ollamaModel = "";
    [ObservableProperty] private string _textAiEndpoint = "";
    [ObservableProperty] private string _textAiApiKey = "";
    [ObservableProperty] private string _textAiModel = "";

    public List<string> FormatOptions { get; } = ["LocalMark", "COCO", "YOLO", "VOC"];

    public SettingsViewModel()
    {
        var s = SettingsManager.Load();
        OutputDir = s.OutputDir;
        ExportFormat = s.ExportFormat;
        OllamaHost = s.Ollama.Host;
        OllamaModel = s.Ollama.Model;
        TextAiEndpoint = s.TextAi.Endpoint;
        TextAiApiKey = s.TextAi.ApiKey;
        TextAiModel = s.TextAi.Model;
    }

    [RelayCommand]
    private void BrowseOutputDir()
    {
        var dlg = new OpenFolderDialog { Title = "选择标注结果存放目录" };
        if (dlg.ShowDialog() == true)
            OutputDir = dlg.FolderName;
    }

    [RelayCommand]
    private void Save()
    {
        var s = new AppSettings
        {
            OutputDir = OutputDir.Trim(),
            ExportFormat = ExportFormat,
            Ollama = new OllamaConfig { Host = OllamaHost.Trim(), Model = OllamaModel.Trim() },
            TextAi = new TextAiConfig { Endpoint = TextAiEndpoint.Trim(), ApiKey = TextAiApiKey.Trim(), Model = TextAiModel.Trim() }
        };
        SettingsManager.Save(s);
        CloseWindow();
    }

    [RelayCommand]
    private void CloseWindow()
    {
        System.Windows.Application.Current.Windows
            .OfType<System.Windows.Window>()
            .FirstOrDefault(w => w.DataContext == this)?
            .Close();
    }
}
