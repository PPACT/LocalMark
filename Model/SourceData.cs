namespace LocalMark.Model;

/// <summary>
/// 素材实体
/// </summary>
public class SourceData
{
    public int Id { get; set; }
    public int DataType { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsMarked { get; set; }
}
