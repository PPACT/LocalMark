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

    [ObservableProperty]
    private string _imagePath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<LabelItem> _labels = [];

    [ObservableProperty]
    private LabelItem? _selectedLabel;

    [ObservableProperty]
    private double _boxX;

    [ObservableProperty]
    private double _boxY;

    [ObservableProperty]
    private double _boxWidth;

    [ObservableProperty]
    private double _boxHeight;

    [ObservableProperty]
    private bool _isAiRunning;

    [ObservableProperty]
    private string _aiStatus = string.Empty;

    public SourceData Source { get; }

    public event Action<ImageMarkViewModel>? MarkSaved;
    public event Action<List<DetectedObject>>? AiMarkCompleted;

    public ImageMarkViewModel(SourceData source)
    {
        Source = source;
        ImagePath = source.Content;
        LoadLabels();

        var existing = _markRepo.GetBySourceId(source.Id);
        if (existing != null)
        {
            var label = Labels.FirstOrDefault(l => l.Name == existing.LabelName);
            if (label != null) SelectedLabel = label;

            if (!string.IsNullOrEmpty(existing.BoxPosition))
            {
                var box = JsonSerializer.Deserialize<Box>(existing.BoxPosition);
                if (box != null)
                {
                    BoxX = box.X;
                    BoxY = box.Y;
                    BoxWidth = box.Width;
                    BoxHeight = box.Height;
                }
            }
        }
    }

    private void LoadLabels()
    {
        Labels = new ObservableCollection<LabelItem>(
            XmlHelper.LoadImageLabels(ConfigPath));
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
        finally
        {
            IsAiRunning = false;
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (SelectedLabel == null) return;

        var boxJson = JsonSerializer.Serialize(new Box
        {
            X = BoxX,
            Y = BoxY,
            Width = BoxWidth,
            Height = BoxHeight
        });

        var existing = _markRepo.GetBySourceId(Source.Id);
        if (existing != null)
        {
            existing.LabelName = SelectedLabel.Name;
            existing.BoxPosition = boxJson;
            existing.MarkedAt = DateTime.Now;
            _markRepo.Update(existing);
        }
        else
        {
            _markRepo.Insert(new MarkResult
            {
                SourceId = Source.Id,
                LabelName = SelectedLabel.Name,
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

    private class Box
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}
