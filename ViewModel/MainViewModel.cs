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

    private bool _isAllSelected;
    public bool IsAllSelected
    {
        get => _isAllSelected;
        set
        {
            if (SetProperty(ref _isAllSelected, value))
            {
                foreach (var s in Sources) s.IsSelected = value;
            }
        }
    }

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
        _isAllSelected = false;
        OnPropertyChanged(nameof(IsAllSelected));
    }

    [RelayCommand]
    private void ClearMarked()
    {
        var ids = Sources.Where(s => s.IsMarked).Select(s => s.Id).ToList();
        if (ids.Count == 0) return;
        foreach (var id in ids)
        {
            var mark = _markRepo.GetBySourceId(id);
            if (mark != null) _markRepo.Delete(mark.Id);
            _sourceRepo.Delete(id);
        }
        RefreshSources();
    }

    [RelayCommand]
    private void ClearUnmarked()
    {
        var ids = Sources.Where(s => !s.IsMarked).Select(s => s.Id).ToList();
        if (ids.Count == 0) return;
        foreach (var id in ids) _sourceRepo.Delete(id);
        RefreshSources();
    }

    [RelayCommand]
    private void ClearAll()
    {
        if (Sources.Count == 0) return;
        foreach (var s in Sources)
        {
            var mark = _markRepo.GetBySourceId(s.Id);
            if (mark != null) _markRepo.Delete(mark.Id);
            _sourceRepo.Delete(s.Id);
        }
        ResetSequences();
        RefreshSources();
    }

    private static void ResetSequences()
    {
        using var conn = new Microsoft.Data.Sqlite.SqliteConnection(
            Repository.DbInitializer.GetConnectionString());
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM sqlite_sequence WHERE name IN ('SourceData', 'MarkResult')";
        cmd.ExecuteNonQuery();
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

        var existing = _sourceRepo.GetAll().Where(s => s.DataType == 0).ToList();
        var fileContents = dlg.FileNames
            .Select(p => new { Path = p, Content = FileHelper.ReadTextFile(p) })
            .ToList();

        var dups = fileContents
            .Where(f => existing.Any(e => e.Content == f.Content))
            .Select(f => Path.GetFileName(f.Path)!)
            .ToList();

        var news = fileContents
            .Where(f => !existing.Any(e => e.Content == f.Content))
            .ToList();

        var action = dups.Count > 0 ? ShowDuplicateDialog(dups) : DuplicateAction.Skip;

        if (action == DuplicateAction.Cancel) return;

        if (action == DuplicateAction.Overwrite)
        {
            foreach (var fc in fileContents.Where(f => existing.Any(e => e.Content == f.Content)))
            {
                var dup = existing.First(e => e.Content == fc.Content);
                var mark = _markRepo.GetBySourceId(dup.Id);
                if (mark != null) _markRepo.Delete(mark.Id);
                _sourceRepo.Delete(dup.Id);
                // 移除已删除的引用，防止后续重复匹配
                existing.Remove(dup);
            }
            // 将覆盖的文件也加入待导入列表
            news.AddRange(fileContents.Where(f =>
                !news.Any(n => n.Path == f.Path) &&
                dups.Contains(Path.GetFileName(f.Path))));
        }

        foreach (var fc in news)
        {
            _sourceRepo.Insert(new SourceData
            {
                DataType = 0, Content = fc.Content, SourceName = Path.GetFileName(fc.Path),
                IsMarked = false, IsHighlighted = true
            });
        }

        RefreshSourcesAndFlash();
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

        var existing = _sourceRepo.GetAll().Where(s => s.DataType == 1).ToList();
        var dups = dlg.FileNames
            .Where(f => existing.Any(e => e.Content == f))
            .Select(f => Path.GetFileName(f)!)
            .ToList();

        var newPaths = dlg.FileNames
            .Where(f => !existing.Any(e => e.Content == f))
            .ToList();

        var action = dups.Count > 0 ? ShowDuplicateDialog(dups) : DuplicateAction.Skip;

        if (action == DuplicateAction.Cancel) return;

        if (action == DuplicateAction.Overwrite)
        {
            foreach (var path in dlg.FileNames.Where(f => existing.Any(e => e.Content == f)))
            {
                var dup = existing.First(e => e.Content == path);
                var mark = _markRepo.GetBySourceId(dup.Id);
                if (mark != null) _markRepo.Delete(mark.Id);
                _sourceRepo.Delete(dup.Id);
                existing.Remove(dup);
            }
            newPaths.AddRange(dlg.FileNames.Where(f =>
                !newPaths.Contains(f) && dups.Contains(Path.GetFileName(f))));
        }

        foreach (var path in newPaths)
        {
            _sourceRepo.Insert(new SourceData
            {
                DataType = 1, Content = path,
                SourceName = Path.GetFileName(path),
                IsMarked = false, IsHighlighted = true
            });
        }

        RefreshSourcesAndFlash();
    }

    private static DuplicateAction ShowDuplicateDialog(List<string> dupNames)
    {
        var dlg = new DuplicateDialog(dupNames);
        dlg.ShowDialog();
        return dlg.Result;
    }

    private void RefreshSourcesAndFlash()
    {
        RefreshSources();
        if (Sources.Count == 0) return;
        _ = Task.Delay(2200).ContinueWith(_ =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var s in Sources) s.IsHighlighted = false;
            });
        });
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
        var settings = SettingsManager.Load();
        var outputDir = !string.IsNullOrWhiteSpace(settings.OutputDir)
            ? settings.OutputDir
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exports");

        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        var sources = _sourceRepo.GetAll().ToList();
        var marks = _markRepo.GetAll().ToList();

        var countText = sources.Count(s => s.DataType == 0);
        var countImage = sources.Count(s => s.DataType == 1);
        var paths = new List<string>();

        if (countText > 0)
            paths.Add(JsonHelper.ExportTextResults(sources, marks, outputDir));
        if (countImage > 0)
            paths.Add(JsonHelper.ExportImageResults(sources, marks, outputDir));

        if (paths.Count == 0)
        {
            System.Windows.MessageBox.Show("没有可导出的数据，请先导入素材。", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        else
        {
            System.Windows.MessageBox.Show(
                $"导出完成 ({paths.Count} 个文件):\n\n{string.Join("\n", paths)}",
                "导出成功",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var vm = new SettingsViewModel();
        var window = new SettingsView
        {
            DataContext = vm,
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    [RelayCommand]
    private void BatchAiMark()
    {
        var selected = Sources.Where(s => s.IsSelected).ToList();
        if (selected.Count == 0)
        {
            System.Windows.MessageBox.Show("请先勾选要标注的素材。", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        var vm = new BatchAiMarkViewModel(selected);
        vm.BatchCompleted += () =>
        {
            RefreshSources();
        };

        var window = new BatchAiMarkView
        {
            DataContext = vm,
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
    }
}
