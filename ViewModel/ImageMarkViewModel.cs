using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalMark.Helper;
using LocalMark.Model;
using LocalMark.Repository;

namespace LocalMark.ViewModel;

public partial class ImageMarkViewModel : ObservableObject
{
    private readonly SourceRepo _sourceRepo = new();
    private readonly MarkRepo _markRepo = new();
    private readonly OllamaAgent _agent = new();

    private static readonly string ConfigPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Config", "LabelConfig.xml");

    [ObservableProperty] private string _imagePath = string.Empty;
    [ObservableProperty] private ObservableCollection<LabelItem> _labels = [];
    [ObservableProperty] private LabelItem? _selectedLabel;
    [ObservableProperty] private ObservableCollection<AnnotationBox> _boxes = [];
    [ObservableProperty] private AnnotationBox? _currentBox;
    [ObservableProperty] private bool _isAiRunning;
    [ObservableProperty] private string _aiStatus = string.Empty;

    public SourceData Source { get; }
    public event Action<ImageMarkViewModel>? MarkSaved;
    public event Action<List<DetectedObject>>? AiMarkCompleted;

    public ImageMarkViewModel(SourceData source)
    {
        Source = source;
        ImagePath = source.Content;
        LoadLabels();
        LoadExistingBoxes();
    }

    private void LoadLabels()
    {
        Labels = new ObservableCollection<LabelItem>(XmlHelper.LoadImageLabels(ConfigPath));
    }

    private void LoadExistingBoxes()
    {
        var existing = _markRepo.GetBySourceId(Source.Id);
        if (existing == null || string.IsNullOrEmpty(existing.BoxPosition)) return;

        try
        {
            var boxes = JsonSerializer.Deserialize<List<AnnotationBox>>(existing.BoxPosition);
            if (boxes != null)
                Boxes = new ObservableCollection<AnnotationBox>(boxes);
        }
        catch { } // 兼容旧单框格式，忽略
    }

    [RelayCommand]
    private async Task AiMark()
    {
        if (!File.Exists(ImagePath)) return;
        IsAiRunning = true;
        try
        {
            var labels = Labels.Select(l => l.Name).ToList();
            var results = await _agent.AnnotateImageAsync(ImagePath, labels);
            AiMarkCompleted?.Invoke(results);
        }
        finally { IsAiRunning = false; }
    }

    /// <summary>添加 AI 检测结果到标注列表</summary>
    public void AddDetections(List<DetectedObject> detections)
    {
        foreach (var d in detections)
            Boxes.Add(new AnnotationBox
            {
                X = d.X, Y = d.Y, Width = d.Width, Height = d.Height, Label = d.Label
            });
    }

    /// <summary>画布绘图新增框</summary>
    public void AddBox(double x, double y, double w, double h)
    {
        Boxes.Add(new AnnotationBox
        {
            X = x, Y = y, Width = w, Height = h,
            Label = SelectedLabel?.Name ?? ""
        });
    }

    /// <summary>清除指定框</summary>
    public void RemoveBox(AnnotationBox box) => Boxes.Remove(box);

    /// <summary>清空所有框</summary>
    [RelayCommand]
    private void ClearAllBoxes() => Boxes.Clear();

    [RelayCommand]
    private void Save()
    {
        if (Boxes.Count == 0) return;

        var boxJson = JsonSerializer.Serialize(Boxes);
        var existing = _markRepo.GetBySourceId(Source.Id);
        if (existing != null)
        {
            existing.LabelName = Boxes[0].Label;
            existing.BoxPosition = boxJson;
            existing.MarkedAt = DateTime.Now;
            _markRepo.Update(existing);
        }
        else
        {
            _markRepo.Insert(new MarkResult
            {
                SourceId = Source.Id,
                LabelName = Boxes[0].Label,
                BoxPosition = boxJson,
                MarkedAt = DateTime.Now
            });
        }

        Source.IsMarked = true;
        _sourceRepo.Update(Source);
        MarkSaved?.Invoke(this);
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
