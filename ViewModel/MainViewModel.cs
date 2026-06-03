using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalMark.Helper;
using LocalMark.Model;
using LocalMark.Repository;
using LocalMark.View;
using Microsoft.Win32;

namespace LocalMark.ViewModel;

public partial class MainViewModel : ObservableObject
{
    private readonly SourceRepo _sourceRepo = new();
    private readonly MarkRepo _markRepo = new();

    [ObservableProperty]
    private ObservableCollection<SourceData> _sources = [];

    [ObservableProperty]
    private SourceData? _selectedSource;

    [ObservableProperty]
    private bool _showUnmarkedOnly;

    private static readonly string ConfigPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Config", "LabelConfig.xml");

    public MainViewModel()
    {
        RefreshSources();
    }

    partial void OnShowUnmarkedOnlyChanged(bool value)
    {
        RefreshSources();
    }

    [RelayCommand]
    private void RefreshSources()
    {
        var items = ShowUnmarkedOnly ? _sourceRepo.GetUnmarked() : _sourceRepo.GetAll();
        Sources = new ObservableCollection<SourceData>(items);
    }

    [RelayCommand]
    private void ImportTextFile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "文本文件|*.txt",
            Multiselect = true
        };
        if (dlg.ShowDialog() != true) return;

        foreach (var filePath in dlg.FileNames)
        {
            var content = FileHelper.ReadTextFile(filePath);
            _sourceRepo.Insert(new SourceData
            {
                DataType = 0,
                Content = content,
                IsMarked = false
            });
        }
        RefreshSources();
    }

    [RelayCommand]
    private void ImportImageFiles()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
            Multiselect = true
        };
        if (dlg.ShowDialog() != true) return;

        foreach (var filePath in dlg.FileNames)
        {
            _sourceRepo.Insert(new SourceData
            {
                DataType = 1,
                Content = filePath,
                IsMarked = false
            });
        }
        RefreshSources();
    }

    [RelayCommand]
    private void OpenMarkView()
    {
        if (SelectedSource == null) return;

        Window window = SelectedSource.DataType switch
        {
            0 => new TextMarkView(),
            1 => new ImageMarkView(),
            _ => throw new InvalidOperationException("Unknown data type")
        };

        if (SelectedSource.DataType == 0)
        {
            var vm = new TextMarkViewModel(SelectedSource);
            vm.MarkSaved += _ => RefreshSources();
            window.DataContext = vm;
        }
        else
        {
            var vm = new ImageMarkViewModel(SelectedSource);
            vm.MarkSaved += _ => RefreshSources();
            window.DataContext = vm;
        }

        window.Owner = System.Windows.Application.Current.MainWindow;
        window.ShowDialog();
    }

    [RelayCommand]
    private void ExportToJson()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "JSON 文件|*.json",
            FileName = "标注结果.json"
        };
        if (dlg.ShowDialog() != true) return;

        var sources = _sourceRepo.GetAll().ToList();
        var marks = _markRepo.GetAll().ToList();
        JsonHelper.ExportMarkedData(sources, marks, dlg.FileName);
    }
}
