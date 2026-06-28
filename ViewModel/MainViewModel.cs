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
    private readonly SourceRepo _sourceRepo;
    private readonly MarkRepo _markRepo;

    [ObservableProperty]
    private ObservableCollection<SourceData> _sources = [];

    [ObservableProperty]
    private SourceData? _selectedSource;

    [ObservableProperty]
    private bool _showUnmarkedOnly;

    [ObservableProperty]
    private string _searchText = "";

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

    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private BatchAiMarkViewModel? _batchVm;
    [ObservableProperty] private bool _isBatching;

    public MainViewModel(SourceRepo sourceRepo, MarkRepo markRepo)
    {
        _sourceRepo = sourceRepo;
        _markRepo = markRepo;
        RefreshSources();
    }

    partial void OnShowUnmarkedOnlyChanged(bool value) => RefreshSources();
    partial void OnSearchTextChanged(string value) => RefreshSources();

    [RelayCommand]
    private void RefreshSources()
    {
        var items = ShowUnmarkedOnly ? _sourceRepo.GetUnmarked() : _sourceRepo.GetAll();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var kw = SearchText.Trim();
            items = items.Where(s =>
                s.SourceName.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || s.Content.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }
        Sources = new ObservableCollection<SourceData>(items);
        _isAllSelected = false;
        OnPropertyChanged(nameof(IsAllSelected));
    }

    [RelayCommand]
    private void ClearMarked()
    {
        // 隐藏已标注 → 仅显示未标注（不删数据）
        ShowUnmarkedOnly = true;
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
        // 已标注的保留在后台（新界面可见），未标注的从 DB 删除
        var unmarkedIds = Sources.Where(s => !s.IsMarked).Select(s => s.Id).ToList();
        foreach (var id in unmarkedIds) _sourceRepo.Delete(id);
        ShowUnmarkedOnly = true;
    }

    [RelayCommand]
    private void DeleteAllResults()
    {
        var markedIds = _sourceRepo.GetAll()
            .Where(s => s.IsMarked).Select(s => s.Id).ToList();
        if (markedIds.Count == 0) return;

        foreach (var id in markedIds)
        {
            var mark = _markRepo.GetBySourceId(id);
            if (mark != null) _markRepo.Delete(mark.Id);
            _sourceRepo.Delete(id);
        }
        RefreshSources();
        RefreshResults();
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

    [RelayCommand]
    private void ImportFolder()
    {
        var dlg = new OpenFolderDialog { Title = "选择素材文件夹" };
        if (dlg.ShowDialog() != true) return;

        var textFiles = FileHelper.GetTextFiles(dlg.FolderName);
        var imageFiles = FileHelper.GetImageFiles(dlg.FolderName);

        if (textFiles.Length == 0 && imageFiles.Length == 0)
        {
            System.Windows.MessageBox.Show("所选文件夹内没有支持的文件。", "提示");
            return;
        }

        var totalImported = 0;
        if (textFiles.Length > 0)
            totalImported += ImportFilesInternal(textFiles, 0, f => FileHelper.ReadTextFile(f));
        if (imageFiles.Length > 0)
            totalImported += ImportFilesInternal(imageFiles, 1, f => f);

        if (totalImported > 0)
            RefreshSourcesAndFlash();
    }

    private int ImportFilesInternal(string[] files, int dataType,
        Func<string, string> readContent)
    {
        var existing = _sourceRepo.GetAll().Where(s => s.DataType == dataType).ToList();
        var items = files.Select(f => new { Path = f, Content = readContent(f) }).ToList();

        var dups = items
            .Where(x => existing.Any(e => e.Content == x.Content))
            .Select(x => Path.GetFileName(x.Path)!)
            .ToList();
        var news = items
            .Where(x => !existing.Any(e => e.Content == x.Content))
            .ToList();

        var action = dups.Count > 0 ? ShowDuplicateDialog(dups) : DuplicateAction.Skip;
        if (action == DuplicateAction.Cancel) return 0;

        if (action == DuplicateAction.Overwrite)
        {
            foreach (var x in items.Where(x => existing.Any(e => e.Content == x.Content)))
            {
                var dup = existing.First(e => e.Content == x.Content);
                var mark = _markRepo.GetBySourceId(dup.Id);
                if (mark != null) _markRepo.Delete(mark.Id);
                _sourceRepo.Delete(dup.Id);
                existing.Remove(dup);
            }
            news.AddRange(items.Where(x =>
                !news.Any(n => n.Path == x.Path) && dups.Contains(Path.GetFileName(x.Path)!)));
        }

        foreach (var x in news)
            _sourceRepo.Insert(new SourceData
            {
                DataType = dataType, Content = x.Content,
                SourceName = Path.GetFileName(x.Path),
                IsMarked = false, IsHighlighted = true
            });

        return news.Count;
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
    private async Task OpenMarkViewAsync()
    {
        var queue = Sources.Where(s => s.IsSelected).ToList();
        if (queue.Count == 0)
        {
            if (SelectedSource != null)
                queue.Add(SelectedSource);
            else
                return;
        }

        var aborted = false;
        for (int i = 0; i < queue.Count && !aborted; i++)
        {
            var source = queue[i];
            var remaining = queue.Count - i;

            Window window = source.DataType switch
            {
                0 => new TextMarkView { Title = "文本标注" },
                1 => new ImageMarkView { Title = "图片标注" },
                _ => throw new InvalidOperationException("Unknown data type")
            };

            if (remaining > 1) window.Title += $" ({i + 1}/{queue.Count})";

            if (source.DataType == 0)
            {
                var vm = new TextMarkViewModel(source);
                vm.MarkSaved += _ => RefreshSources();
                window.DataContext = vm;
            }
            else
            {
                var vm = new ImageMarkViewModel(source);
                vm.MarkSaved += _ => RefreshSources();
                window.DataContext = vm;
            }

            window.Closing += (_, e) =>
            {
                if (source.IsMarked) return;
                if (remaining <= 1) return;
                var r = MessageBox.Show(
                    $"还有 {remaining - 1} 个未标注，是否退出？\n（已标注的会保留，未标注的保持原状）",
                    "退出标注", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                    aborted = true;
                else
                    e.Cancel = true;
            };

            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }
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
        var paths = new List<string>();

        switch (settings.ExportFormat)
        {
            case "COCO":
                new CocoExporter().Export(sources, marks, outputDir);
                paths.Add("coco_*/ (images/ + annotations.json)");
                break;
            case "YOLO":
                new YoloExporter().Export(sources, marks, outputDir);
                paths.Add("yolo_*/ (images/ + labels/ + data.yaml)");
                break;
            case "VOC":
                new VocExporter().Export(sources, marks, outputDir);
                paths.Add("voc_*/ (JPEGImages/ + Annotations/)");
                break;
            default:
                if (sources.Any(s => s.DataType == 0))
                    paths.Add(JsonHelper.ExportTextResults(sources, marks, outputDir));
                if (sources.Any(s => s.DataType == 1))
                    paths.Add(JsonHelper.ExportImageResults(sources, marks, outputDir));
                break;
        }

        MessageBox.Show(paths.Count > 0
            ? $"导出完成:\n{string.Join("\n", paths)}"
            : "没有可导出的数据。", paths.Count > 0 ? "导出成功" : "提示");
    }

    [RelayCommand]
    private void CloseBatchPanel()
    {
        BatchVm?.CancelCommand.Execute(null);
        BatchVm = null;
        IsBatching = false;
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        if (value == 1) RefreshResults();
    }

    [ObservableProperty]
    private ObservableCollection<object> _results = [];

    private void RefreshResults()
    {
        var sources = _sourceRepo.GetAll().ToList();
        var marks = _markRepo.GetAll().ToList();
        Results = new ObservableCollection<object>(
            sources.Where(s => s.IsMarked).Select(s =>
            {
                var m = marks.FirstOrDefault(x => x.SourceId == s.Id);
                return (object)new
                {
                    FileName = s.SourceName,
                    Type = s.DataType == 0 ? "文本" : "图片",
                    Label = m?.LabelName ?? "—",
                    MarkedAt = m?.MarkedAt?.ToString("yyyy-MM-dd HH:mm") ?? "—",
                    Content = s.DataType == 0 ? s.Content : s.SourceName
                };
            }));
    }

    [RelayCommand]
    private void ConvertFormat()
    {
        var inDlg = new OpenFolderDialog { Title = "选择源数据集文件夹" };
        if (inDlg.ShowDialog() != true) return;

        var srcFmt = FormatConverter.DetectFormat(inDlg.FolderName);
        if (srcFmt == "Unknown")
        { MessageBox.Show("无法识别源数据集格式。"); return; }

        var outDlg = new OpenFolderDialog { Title = "选择输出目录" };
        if (outDlg.ShowDialog() != true) return;

        var count = FormatConverter.Convert(inDlg.FolderName, outDlg.FolderName);
        MessageBox.Show(count > 0
            ? $"转换完成: {count} 条标注 ({srcFmt} → 目标格式)"
            : "转换失败，请检查源数据集。");
    }

    [RelayCommand]
    private void ImportDataset()
    {
        var dlg = new OpenFolderDialog { Title = "选择数据集文件夹（COCO / YOLO / VOC）" };
        if (dlg.ShowDialog() != true) return;

        IImportService importer;
        var path = dlg.FolderName;

        // 自动检测格式
        if (Directory.GetFiles(path, "annotations.json", SearchOption.AllDirectories).Any())
            importer = new CocoImporter();
        else if (Directory.GetDirectories(path, "labels", SearchOption.AllDirectories).Any()
              || File.Exists(Path.Combine(path, "data.yaml")))
            importer = new YoloImporter();
        else if (Directory.GetDirectories(path, "Annotations", SearchOption.AllDirectories).Any())
            importer = new VocImporter();
        else
        {
            MessageBox.Show("无法识别数据集格式。请选择包含 annotations.json / labels/ / Annotations/ 的文件夹。", "提示");
            return;
        }

        var imported = importer.Import(path);
        if (imported.Count == 0)
        { MessageBox.Show("未找到可导入的标注数据。", "提示"); return; }

        foreach (var r in imported)
        {
            var sid = _sourceRepo.Insert(r.Source);
            if (r.Boxes.Count > 0)
            {
                var boxJson = System.Text.Json.JsonSerializer.Serialize(r.Boxes);
                _markRepo.Insert(new MarkResult
                {
                    SourceId = sid,
                    LabelName = r.Boxes[0].Label,
                    BoxPosition = boxJson,
                    MarkedAt = DateTime.Now
                });
            }
        }

        RefreshSourcesAndFlash();
        MessageBox.Show($"导入完成: {imported.Count} 条素材。打开标注窗口即可回显。", "导入成功");
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

        BatchVm = new BatchAiMarkViewModel(selected);
        BatchVm.BatchCompleted += () =>
        {
            IsBatching = false;
            RefreshSources();
        };
        IsBatching = true;
    }
}
