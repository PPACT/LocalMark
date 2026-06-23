namespace LocalMark.Model;

/// <summary>
/// 标注结果实体
/// </summary>
public class MarkResult
{
    public int Id { get; set; }
    public int SourceId { get; set; }
    public string LabelName { get; set; } = string.Empty;
    public string? BoxPosition { get; set; }
    public DateTime? MarkedAt { get; set; }
}
