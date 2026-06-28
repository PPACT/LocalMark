using System.IO;
using System.Text.Json;
using LocalMark.Model;

namespace LocalMark.Helper;

public class QcIssue
{
    public int SourceId { get; set; }
    public string FileName { get; set; } = "";
    public string Type { get; set; } = "";
    public string Detail { get; set; } = "";
}

public static class QualityChecker
{
    public static List<QcIssue> Check(IEnumerable<SourceData> sources, IEnumerable<MarkResult> marks)
    {
        var issues = new List<QcIssue>();
        var markDict = marks.ToDictionary(m => m.SourceId);
        var srcList = sources.Where(s => s.DataType == 1).ToList();

        foreach (var s in srcList)
        {
            var name = !string.IsNullOrEmpty(s.SourceName) ? s.SourceName : Path.GetFileName(s.Content);

            if (!s.IsMarked)
            {
                issues.Add(new QcIssue { SourceId = s.Id, FileName = name, Type = "漏标", Detail = "未标注任何框" });
                continue;
            }

            if (!markDict.TryGetValue(s.Id, out var m) || string.IsNullOrEmpty(m.BoxPosition))
            {
                issues.Add(new QcIssue { SourceId = s.Id, FileName = name, Type = "空标注", Detail = "标注记录为空" });
                continue;
            }

            var boxes = ParseBoxes(m.BoxPosition);
            if (boxes.Count == 0)
            {
                issues.Add(new QcIssue { SourceId = s.Id, FileName = name, Type = "空标注", Detail = "标注记录为空" });
                continue;
            }

            var (iw, ih) = GetImageSize(s.Content);

            foreach (var box in boxes)
            {
                if (iw > 0 && (box.X < 0 || box.Y < 0 || box.X + box.Width > iw || box.Y + box.Height > ih))
                    issues.Add(new QcIssue { SourceId = s.Id, FileName = name, Type = "越界",
                        Detail = $"框 [{box.X:F0},{box.Y:F0}] 超出图片范围 ({iw}x{ih})" });

                if (box.Width < 5 || box.Height < 5)
                    issues.Add(new QcIssue { SourceId = s.Id, FileName = name, Type = "过小",
                        Detail = $"框尺寸 ({box.Width:F0}x{box.Height:F0}) 小于 5px 阈值" });

                if (string.IsNullOrEmpty(box.Label) || box.Label == "unknown")
                    issues.Add(new QcIssue { SourceId = s.Id, FileName = name, Type = "缺标签",
                        Detail = "框缺少有效标签名" });
            }
        }

        return issues;
    }

    private static List<AnnotationBox> ParseBoxes(string json)
    {
        try { var l = JsonSerializer.Deserialize<List<AnnotationBox>>(json); if (l != null) return l; } catch { }
        return [];
    }

    private static (int, int) GetImageSize(string path)
    {
        try { using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var f = System.Windows.Media.Imaging.BitmapDecoder.Create(fs,
                System.Windows.Media.Imaging.BitmapCreateOptions.None,
                System.Windows.Media.Imaging.BitmapCacheOption.None).Frames[0];
            return (f.PixelWidth, f.PixelHeight); }
        catch { return (0, 0); }
    }
}
