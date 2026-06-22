using System.IO;
using System.Text.Json;
using LocalMark.Model;

namespace LocalMark.Helper;

public static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string ExportTextResults(
        IEnumerable<SourceData> sources, IEnumerable<MarkResult> marks,
        string outputDir, string? dateSuffix = null)
    {
        var date = dateSuffix ?? DateTime.Now.ToString("yyyyMMdd");
        var path = Path.Combine(outputDir, $"文本标注_{date}.json");

        var list = sources.Where(s => s.DataType == 0).ToList();
        var markDict = marks.ToDictionary(m => m.SourceId);

        var records = list.Select(s => new TextRecord
        {
            Id = s.Id,
            SourceName = s.SourceName,
            Content = s.Content,
            Label = markDict.TryGetValue(s.Id, out var m) ? m.LabelName : null
        }).ToList();

        var distribution = records
            .Where(r => r.Label != null)
            .GroupBy(r => r.Label)
            .ToDictionary(g => g.Key!, g => g.Count());

        var output = new
        {
            exportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            type = "文本标注",
            summary = new
            {
                total = records.Count,
                marked = records.Count(r => r.Label != null),
                unmarked = records.Count(r => r.Label == null)
            },
            labelDistribution = distribution,
            records
        };

        File.WriteAllText(path, JsonSerializer.Serialize(output, Options));
        return path;
    }

    public static string ExportImageResults(
        IEnumerable<SourceData> sources, IEnumerable<MarkResult> marks,
        string outputDir, string? dateSuffix = null)
    {
        var date = dateSuffix ?? DateTime.Now.ToString("yyyyMMdd");
        var path = Path.Combine(outputDir, $"图片标注_{date}.json");

        var list = sources.Where(s => s.DataType == 1).ToList();
        var markDict = marks.ToDictionary(m => m.SourceId);

        var records = list.Select(s =>
        {
            markDict.TryGetValue(s.Id, out var m);
            return new ImageRecord
            {
                Id = s.Id,
                SourceName = s.SourceName,
                ImagePath = s.Content,
                Label = m?.LabelName,
                BoxPosition = ParseBox(m?.BoxPosition)
            };
        }).ToList();

        var distribution = records
            .Where(r => r.Label != null)
            .GroupBy(r => r.Label)
            .ToDictionary(g => g.Key!, g => g.Count());

        var output = new
        {
            exportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            type = "图片标注",
            summary = new
            {
                total = records.Count,
                marked = records.Count(r => r.Label != null),
                unmarked = records.Count(r => r.Label == null)
            },
            labelDistribution = distribution,
            records
        };

        File.WriteAllText(path, JsonSerializer.Serialize(output, Options));
        return path;
    }

    private static object? ParseBox(string? boxJson)
    {
        if (string.IsNullOrEmpty(boxJson)) return null;
        try { return JsonSerializer.Deserialize<object>(boxJson); }
        catch { return boxJson; }
    }

    private class TextRecord
    {
        public int Id { get; set; }
        public string SourceName { get; set; } = "";
        public string Content { get; set; } = "";
        public string? Label { get; set; }
    }

    private class ImageRecord
    {
        public int Id { get; set; }
        public string SourceName { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public string? Label { get; set; }
        public object? BoxPosition { get; set; }
    }
}
