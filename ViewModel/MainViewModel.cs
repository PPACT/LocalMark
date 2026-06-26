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

    [ObservableProperty] private ObservableCollection<SourceData> _sources = [];
    [ObservableProperty] private SourceData? _selectedSource;
    [ObservableProperty] private bool _showUnmarkedOnly;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private ObservableCollection<object> _results = [];

    private bool _isAllSelected;
    public bool IsAllSelected
    {
        get => _isAllSelected;
        set { if (SetProperty(ref _isAllSelected, value))
            foreach (var s in Sources) s.IsSelected = value; }
    }

    public MainViewModel(SourceRepo sourceRepo, MarkRepo markRepo)
    {
        _sourceRepo = sourceRepo;
        _markRepo = markRepo;
        RefreshSources();
    }

    partial void OnShowUnmarkedOnlyChanged(bool value) => RefreshSources();
    partial void OnSearchTextChanged(string value) => RefreshSources();
    partial void OnSelectedTabIndexChanged(int value) { if (value == 1) RefreshResults(); }

    [RelayCommand]
    private void RefreshSources()
    {
        var items = ShowUnmarkedOnly ? _sourceRepo.GetUnmarked() : _sourceRepo.GetAll();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var kw = SearchText.Trim();
            items = items.Where(s => s.SourceName.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || s.Content.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }
        Sources = new ObservableCollection<SourceData>(items);
        _isAllSelected = false; OnPropertyChanged(nameof(IsAllSelected));
    }

    private void RefreshResults()
    {
        var sources = _sourceRepo.GetAll().ToList();
        var marks = _markRepo.GetAll().ToList();
        Results = new ObservableCollection<object>(
            sources.Where(s => s.IsMarked).Select(s =>
            {
                var m = marks.FirstOrDefault(x => x.SourceId == s.Id);
                return (object)new { FileName = s.SourceName, Type = s.DataType == 0 ? "文本" : "图片",
                    Label = m?.LabelName ?? "—", MarkedAt = m?.MarkedAt?.ToString("yyyy-MM-dd HH:mm") ?? "—",
                    Content = s.DataType == 0 ? s.Content : s.SourceName };
            }));
    }

    // ===== Import =====

    [RelayCommand] private void ImportTextFile()
    {
        var dlg = new OpenFileDialog { Filter = "文本文件|*.txt", Multiselect = true };
        if (dlg.ShowDialog() != true) return;
        var existing = _sourceRepo.GetAll().Where(s => s.DataType == 0).ToList();
        var fileContents = dlg.FileNames.Select(p => new { Path = p, Content = FileHelper.ReadTextFile(p) }).ToList();
        var dups = fileContents.Where(f => existing.Any(e => e.Content == f.Content)).Select(f => Path.GetFileName(f.Path)!).ToList();
        var news = fileContents.Where(f => !existing.Any(e => e.Content == f.Content)).ToList();
        var action = dups.Count > 0 ? ShowDup(dups) : DuplicateAction.Skip;
        if (action == DuplicateAction.Cancel) return;
        if (action == DuplicateAction.Overwrite)
        { foreach (var fc in fileContents.Where(f => existing.Any(e => e.Content == f.Content)))
            { var d = existing.First(e => e.Content == fc.Content); MarkOverwrite(d); existing.Remove(d); }
          news.AddRange(fileContents.Where(f => !news.Any(n => n.Path == f.Path) && dups.Contains(Path.GetFileName(f.Path)))); }
        foreach (var fc in news) _sourceRepo.Insert(new SourceData {
            DataType = 0, Content = fc.Content, SourceName = Path.GetFileName(fc.Path), IsMarked = false, IsHighlighted = true });
        Flash();
    }

    [RelayCommand] private void ImportImageFiles()
    {
        var dlg = new OpenFileDialog { Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif", Multiselect = true };
        if (dlg.ShowDialog() != true) return;
        var existing = _sourceRepo.GetAll().Where(s => s.DataType == 1).ToList();
        var dups = dlg.FileNames.Where(f => existing.Any(e => e.Content == f)).Select(f => Path.GetFileName(f)!).ToList();
        var newPaths = dlg.FileNames.Where(f => !existing.Any(e => e.Content == f)).ToList();
        var action = dups.Count > 0 ? ShowDup(dups) : DuplicateAction.Skip;
        if (action == DuplicateAction.Cancel) return;
        if (action == DuplicateAction.Overwrite)
        { foreach (var p in dlg.FileNames.Where(f => existing.Any(e => e.Content == f)))
            { var d = existing.First(e => e.Content == p); MarkOverwrite(d); existing.Remove(d); }
          newPaths.AddRange(dlg.FileNames.Where(f => !newPaths.Contains(f) && dups.Contains(Path.GetFileName(f)))); }
        foreach (var p in newPaths) _sourceRepo.Insert(new SourceData {
            DataType = 1, Content = p, SourceName = Path.GetFileName(p), IsMarked = false, IsHighlighted = true });
        Flash();
    }

    [RelayCommand] private void ImportFolder()
    {
        var dlg = new OpenFolderDialog { Title = "选择素材文件夹" };
        if (dlg.ShowDialog() != true) return;
        var tf = FileHelper.GetTextFiles(dlg.FolderName); var imf = FileHelper.GetImageFiles(dlg.FolderName);
        if (tf.Length == 0 && imf.Length == 0) { MessageBox.Show("所选文件夹内没有支持的文件。"); return; }
        var n = 0; if (tf.Length > 0) n += ImportBatch(tf, 0, FileHelper.ReadTextFile); if (imf.Length > 0) n += ImportBatch(imf, 1, f => f);
        if (n > 0) Flash();
    }

    private int ImportBatch(string[] files, int dt, Func<string, string> read)
    {
        var existing = _sourceRepo.GetAll().Where(s => s.DataType == dt).ToList();
        var items = files.Select(f => new { Path = f, Content = read(f) }).ToList();
        var dups = items.Where(x => existing.Any(e => e.Content == x.Content)).Select(x => Path.GetFileName(x.Path)!).ToList();
        var news = items.Where(x => !existing.Any(e => e.Content == x.Content)).ToList();
        var action = dups.Count > 0 ? ShowDup(dups) : DuplicateAction.Skip;
        if (action == DuplicateAction.Cancel) return 0;
        if (action == DuplicateAction.Overwrite)
        { foreach (var x in items.Where(x => existing.Any(e => e.Content == x.Content)))
            { var d = existing.First(e => e.Content == x.Content); MarkOverwrite(d); existing.Remove(d); }
          news.AddRange(items.Where(x => !news.Any(n => n.Path == x.Path) && dups.Contains(Path.GetFileName(x.Path)!))); }
        foreach (var x in news) _sourceRepo.Insert(new SourceData {
            DataType = dt, Content = x.Content, SourceName = Path.GetFileName(x.Path), IsMarked = false, IsHighlighted = true });
        return news.Count;
    }

    private void MarkOverwrite(SourceData dup) { var m = _markRepo.GetBySourceId(dup.Id); if (m != null) _markRepo.Delete(m.Id); _sourceRepo.Delete(dup.Id); }

    // ===== Clear =====

    [RelayCommand] private void ClearMarked() => ShowUnmarkedOnly = true;
    [RelayCommand] private void ClearUnmarked() { var ids = Sources.Where(s => !s.IsMarked).Select(s => s.Id).ToList(); foreach (var id in ids) _sourceRepo.Delete(id); RefreshSources(); }
    [RelayCommand] private void ClearAll() { var ids = Sources.Where(s => !s.IsMarked).Select(s => s.Id).ToList(); foreach (var id in ids) _sourceRepo.Delete(id); ShowUnmarkedOnly = true; }
    [RelayCommand] private void DeleteAllResults() { var ids = _sourceRepo.GetAll().Where(s => s.IsMarked).Select(s => s.Id).ToList(); foreach (var id in ids) { var m = _markRepo.GetBySourceId(id); if (m != null) _markRepo.Delete(m.Id); _sourceRepo.Delete(id); } RefreshSources(); RefreshResults(); }

    // ===== Mark queue =====

    [RelayCommand]
    private async Task OpenMarkViewAsync()
    {
        var queue = Sources.Where(s => s.IsSelected).ToList();
        if (queue.Count == 0) { if (SelectedSource != null) queue.Add(SelectedSource); else return; }
        var aborted = false;
        for (int i = 0; i < queue.Count && !aborted; i++)
        {
            var s = queue[i]; var rem = queue.Count - i;
            Window w = s.DataType switch { 0 => new TextMarkView { Title = "文本标注" }, 1 => new ImageMarkView { Title = "图片标注" }, _ => throw new() };
            if (rem > 1) w.Title += $" ({i + 1}/{queue.Count})";
            if (s.DataType == 0) { var vm = new TextMarkViewModel(s); vm.MarkSaved += _ => RefreshSources(); w.DataContext = vm; }
            else { var vm = new ImageMarkViewModel(s); vm.MarkSaved += _ => RefreshSources(); w.DataContext = vm; }
            w.Closing += (_, e) => { if (s.IsMarked || rem <= 1) return; if (MessageBox.Show($"还有 {rem - 1} 个未标注，是否退出？","退出标注",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes) aborted=true; else e.Cancel=true; };
            w.Owner = Application.Current.MainWindow; w.ShowDialog();
        }
    }

    // ===== Export =====

    [RelayCommand] private void ExportToJson()
    {
        var st = SettingsManager.Load();
        var od = !string.IsNullOrWhiteSpace(st.OutputDir) ? st.OutputDir : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exports");
        if (!Directory.Exists(od)) Directory.CreateDirectory(od);
        var sources = _sourceRepo.GetAll().ToList(); var marks = _markRepo.GetAll().ToList(); var paths = new List<string>();
        if (sources.Any(s => s.DataType == 0)) paths.Add(JsonHelper.ExportTextResults(sources, marks, od));
        if (sources.Any(s => s.DataType == 1)) paths.Add(JsonHelper.ExportImageResults(sources, marks, od));
        MessageBox.Show(paths.Count > 0 ? $"导出完成 ({paths.Count} 个文件):\n\n{string.Join("\n", paths)}" : "没有可导出的数据。");
    }

    // ===== Settings =====

    [RelayCommand] private void OpenSettings() { new SettingsView { DataContext = new SettingsViewModel(), Owner = Application.Current.MainWindow }.ShowDialog(); }

    // ===== Helpers =====

    static DuplicateAction ShowDup(List<string> d) { var w = new DuplicateDialog(d); w.ShowDialog(); return w.Result; }
    void Flash() { RefreshSources(); if (Sources.Count == 0) return; _ = Task.Delay(2200).ContinueWith(_ => Application.Current.Dispatcher.Invoke(() => { foreach (var s in Sources) s.IsHighlighted = false; })); }
}
