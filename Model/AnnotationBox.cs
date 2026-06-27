namespace LocalMark.Model;

/// <summary>
/// 单个标注框（Canvas 坐标系）
/// </summary>
public partial class AnnotationBox : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string Label { get; set; } = string.Empty;
}
