using System.IO;
using System.Text.Json;
using LocalMark.Model;

namespace LocalMark.Helper;

public class CocoExporter : IExportService
{
    public ExportFormat Format => ExportFormat.COCO;

    public void Export(IEnumerable<SourceData> sources, IEnumerable<MarkResult> marks, string outputDir)
    {
        var srcList = sources.Where(s => s.DataType == 1 && s.IsMarked).ToList();
        var markDict = marks.Where(m => srcList.Any(s => s.Id == m.SourceId))
            .ToDictionary(m => m.SourceId);

        var categories = new List<object>();
        var catMap = new Dictionary<string, int>();
        var annotations = new List<object>();
        var images = new List<object>();
        int annoId = 1, imgId = 1;

        foreach (var s in srcList)
        {
            if (!File.Exists(s.Content)) continue;
            if (!markDict.TryGetValue(s.Id, out var m) || string.IsNullOrEmpty(m.BoxPosition)) continue;

            var (w, h) = GetImageSize(s.Content);
            if (w == 0 || h == 0) continue;

            images.Add(new { id = imgId, file_name = Path.GetFileName(s.Content), width = w, height = h });

            if (!catMap.TryGetValue(m.LabelName, out var catId))
            {
                catId = catMap.Count + 1;
                catMap[m.LabelName] = catId;
                categories.Add(new { id = catId, name = m.LabelName });
            }

            var boxes = ParseBoxes(m.BoxPosition);
            if (boxes.Count == 0) { imgId++; continue; }

            foreach (var box in boxes)
            {
                var (bx, by, bw, bh) = ClampBox(box.X, box.Y, box.Width, box.Height, w, h);
                annotations.Add(new
                {
                    id = annoId++, image_id = imgId, category_id = catId,
                    bbox = new[] { bx, by, bw, bh }, area = bw * bh, iscrowd = 0
                });
            }

            imgId++;
        }

        var coco = new { images, annotations, categories };
        var json = JsonSerializer.Serialize(coco, new JsonSerializerOptions { WriteIndented = true });
        var path = Path.Combine(outputDir, $"coco_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        File.WriteAllText(path, json);
    }

    private static (int w, int h) GetImageSize(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var frame = System.Windows.Media.Imaging.BitmapDecoder.Create(fs,
                System.Windows.Media.Imaging.BitmapCreateOptions.None,
                System.Windows.Media.Imaging.BitmapCacheOption.None).Frames[0];
            return (frame.PixelWidth, frame.PixelHeight);
        }
        catch { return (0, 0); }
    }

    private static List<Box> ParseBoxes(string json)
    {
        try
        {
            // 新格式: 多框数组
            var list = JsonSerializer.Deserialize<List<Box>>(json);
            if (list != null) return list;
        }
        catch { }
        try
        {
            // 兼容旧格式: 单框
            var single = JsonSerializer.Deserialize<Box>(json);
            if (single != null) return [single];
        }
        catch { }
        return [];
    }

    private static (double x, double y, double w, double h) ClampBox(
        double x, double y, double w, double h, int imgW, int imgH)
    {
        x = Math.Max(0, Math.Min(x, imgW - 1));
        y = Math.Max(0, Math.Min(y, imgH - 1));
        w = Math.Max(1, Math.Min(w, imgW - x));
        h = Math.Max(1, Math.Min(h, imgH - y));
        return (x, y, w, h);
    }

    private class Box { public double X { get; set; } public double Y { get; set; } public double Width { get; set; } public double Height { get; set; } }
}
