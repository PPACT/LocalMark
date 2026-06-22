using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LocalMark.Model;

/// <summary>
/// 素材实体
/// </summary>
public partial class SourceData : ObservableObject
{
    public int Id { get; set; }
    public int DataType { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsMarked { get; set; }

    [ObservableProperty]
    [property: JsonIgnore]
    private bool _isSelected;
}
