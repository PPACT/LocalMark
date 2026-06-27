using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalMark.Helper;
using LocalMark.Model;
using LocalMark.Repository;

namespace LocalMark.ViewModel;

public partial class BatchAiMarkViewModel : ObservableObject
{
    private readonly SourceRepo _sourceRepo = new();
    private readonly MarkRepo _markRepo = new();
    private readonly OllamaAgent _ollama = new();
    private TextAIAgent? _textAgent;
    private CancellationTokenSource? _cts;

    private static readonly string ConfigPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Config", "LabelConfig.xml");

    private readonly List<LabelItem> _textLabels;
    private readonly List<LabelItem> _imageLabels;

    public List<SourceData> Sources { get; }

    private int _progressValue;
    public int ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    [ObservableProperty] private double _displayProgressValue;
    [ObservableProperty] private string _currentItem = "";
    [ObservableProperty] private string _statusText = "就绪";
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private ObservableCollection<BatchResult> _results = [];

    public event Action? BatchCompleted;

    public BatchAiMarkViewModel(List<SourceData> sources)
    {
        Sources = sources;
        _textLabels = XmlHelper.LoadTextLabels(ConfigPath);
        _imageLabels = XmlHelper.LoadImageLabels(ConfigPath);

        // 打开即显示任务列表
        Results = new ObservableCollection<BatchResult>(
            Sources.Select((s, i) => new BatchResult
            {
                Index = i + 1,
                DataType = s.DataType == 0 ? "文本" : "图片",
                Content = s.Content,
                Status = "等待处理"
            }));
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        if (IsRunning) return;
        if (Sources.Count == 0)
        {
            StatusText = "没有待处理的素材";
            return;
        }

        _cts = new CancellationTokenSource();
        IsRunning = true;
        IsCompleted = false;

        var total = Sources.Count;
        var alreadyDone = Results.Count(r =>
            r.Status is "成功" or "失败" or "跳过" or "无检出");
        var processed = alreadyDone;

        ProgressValue = (int)(processed * 100.0 / total);
        DisplayProgressValue = ProgressValue;

        // 启动假进度动画
        var animCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
        var animTask = AnimateProgressAsync(animCts.Token, total);

        for (int i = 0; i < Sources.Count; i++)
        {
            if (_cts.IsCancellationRequested) break;

            var source = Sources[i];
            var result = Results[i];

            // 跳过已完成项
            if (result.Status is "成功" or "失败" or "跳过" or "无检出")
                continue;

            CurrentItem = source.DataType == 0
                ? $"[文本] {Truncate(source.Content, 40)}"
                : $"[图片] {Path.GetFileName(source.Content)}";
            StatusText = $"处理中 ({processed + 1}/{total})";
            result.Status = "处理中...";

            try
            {
                if (source.DataType == 0)
                    await ProcessText(source, result, _cts.Token);
                else
                    await ProcessImage(source, result, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                result.Status = "已取消";
                break;
            }
            catch (Exception ex)
            {
                result.Status = "失败";
                result.ErrorMessage = ex.Message;
            }

            processed++;
            ProgressValue = (int)(processed * 100.0 / total);
        }

        animCts.Cancel();
        try { await animTask; } catch (OperationCanceledException) { }

        var cancelled = _cts.IsCancellationRequested;
        if (!cancelled)
            DisplayProgressValue = 100;

        IsRunning = false;
        IsCompleted = !cancelled;
        StatusText = cancelled
            ? $"已取消 ({processed}/{total})"
            : $"完成 ({processed}/{total})";

        if (!cancelled)
            BatchCompleted?.Invoke();
    }

    private async Task AnimateProgressAsync(CancellationToken ct, int total)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(200, ct);
            var real = ProgressValue;
            var target = Math.Min(real + 2, 99);
            DisplayProgressValue += (target - DisplayProgressValue) * 0.12 + 0.3;
            DisplayProgressValue = Math.Min(DisplayProgressValue, target);
        }
    }

    private async Task ProcessText(SourceData source, BatchResult result, CancellationToken ct)
    {
        if (_textAgent == null)
        {
            try { _textAgent = new TextAIAgent(); }
            catch (InvalidOperationException ex)
            {
                result.Status = "跳过";
                result.ErrorMessage = ex.Message;
                return;
            }
        }

        var label = await _textAgent.ClassifyAsync(
            source.Content, _textLabels.Select(l => l.Name).ToList(), ct);
        result.ResultLabel = label;
        result.Status = "成功";

        SaveMark(source, label, null);
    }

    private async Task ProcessImage(SourceData source, BatchResult result, CancellationToken ct)
    {
        var detections = await _ollama.AnnotateImageAsync(
            source.Content, _imageLabels.Select(l => l.Name).ToList(), ct);

        if (detections.Count == 0)
        {
            result.Status = "无检出";
            result.ErrorMessage = "AI 未检测到任何目标";
            return;
        }

        var best = detections[0];
        result.ResultLabel = best.Label;
        result.Status = $"成功 ({detections.Count}个目标)";

        var boxJson = System.Text.Json.JsonSerializer.Serialize(new[]
        {
            new { x = best.X, y = best.Y, w = best.Width, h = best.Height, label = best.Label }
        });

        SaveMark(source, best.Label, boxJson);
    }

    private void SaveMark(SourceData source, string label, string? boxJson)
    {
        var existing = _markRepo.GetBySourceId(source.Id);
        if (existing != null)
        {
            existing.LabelName = label;
            existing.BoxPosition = boxJson;
            existing.MarkedAt = DateTime.Now;
            _markRepo.Update(existing);
        }
        else
        {
            _markRepo.Insert(new MarkResult
            {
                SourceId = source.Id,
                LabelName = label,
                BoxPosition = boxJson,
                MarkedAt = DateTime.Now
            });
        }

        source.IsMarked = true;
        _sourceRepo.Update(source);
    }

    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
        StatusText = "正在取消...";
    }

    [RelayCommand]
    private void CloseWindow()
    {
        _cts?.Cancel();
        System.Windows.Application.Current.Windows
            .OfType<System.Windows.Window>()
            .FirstOrDefault(w => w.DataContext == this)?
            .Close();
    }

    private static string Truncate(string text, int maxLen)
        => text.Length <= maxLen ? text : text[..maxLen] + "...";
}

public partial class BatchResult : ObservableObject
{
    public int Index { get; set; }
    public string DataType { get; set; } = "";
    public string Content { get; set; } = "";

    [ObservableProperty] private string _resultLabel = "";
    [ObservableProperty] private string _status = "";
    public string? ErrorMessage { get; set; }

    partial void OnStatusChanged(string value) => OnPropertyChanged(nameof(Summary));
    partial void OnResultLabelChanged(string value) => OnPropertyChanged(nameof(Summary));

    public string Summary =>
        ErrorMessage != null
            ? $"{Status}: {ErrorMessage}"
            : string.IsNullOrEmpty(ResultLabel) ? Status : $"{Status} → {ResultLabel}";
}
