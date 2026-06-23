using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalMark.Helper;
using LocalMark.Model;
using LocalMark.Repository;

namespace LocalMark.ViewModel;

public partial class TextMarkViewModel : ObservableObject
{
    private readonly SourceRepo _sourceRepo = new();
    private readonly MarkRepo _markRepo = new();

    private static readonly string ConfigPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Config", "LabelConfig.xml");

    [ObservableProperty]
    private string _sourceContent = string.Empty;

    [ObservableProperty]
    private ObservableCollection<LabelItem> _labels = [];

    [ObservableProperty]
    private LabelItem? _selectedLabel;

    public SourceData Source { get; }

    public event Action<TextMarkViewModel>? MarkSaved;

    public TextMarkViewModel(SourceData source)
    {
        Source = source;
        SourceContent = source.Content;
        LoadLabels();

        // 加载已有标注
        var existing = _markRepo.GetBySourceId(source.Id);
        if (existing != null)
        {
            var label = Labels.FirstOrDefault(l => l.Name == existing.LabelName);
            if (label != null) SelectedLabel = label;
        }
    }

    private void LoadLabels()
    {
        Labels = new ObservableCollection<LabelItem>(
            XmlHelper.LoadTextLabels(ConfigPath));
    }

    [RelayCommand]
    private void Save()
    {
        if (SelectedLabel == null) return;

        var existing = _markRepo.GetBySourceId(Source.Id);
        if (existing != null)
        {
            existing.LabelName = SelectedLabel.Name;
            existing.MarkedAt = DateTime.Now;
            _markRepo.Update(existing);
        }
        else
        {
            _markRepo.Insert(new MarkResult
            {
                SourceId = Source.Id,
                LabelName = SelectedLabel.Name,
                MarkedAt = DateTime.Now
            });
        }

        // 标记素材为已标注
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
