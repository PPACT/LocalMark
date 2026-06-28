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
        var markDict = marks.ToDictionary(m => m.SourceId);

        var root = Path.Combine(outputDir, $"coco_{DateTime.Now:yyyyMMdd_HHmmss}");
        var imgDir = Path.Combine(root, "images");
        Directory.CreateDirectory(imgDir);

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

            var destName = Path.GetFileName(s.Content);
            var destPath = Path.Combine(imgDir, destName);
            File.Copy(s.Content, destPath, true);

            images.Add(new { id = imgId, file_name = destName, width = w, height = h });

            if (!catMap.TryGetValue(m.LabelName, out var catId))
            {
                catId = catMap.Count + 1;
                catMap[m.LabelName] = catId;
                categories.Add(new { id = catId, name = m.LabelName });
            }

            foreach (var box in ParseBoxes(m.BoxPosition))
            {
                var (bx, by, bw, bh) = ClampBox(box.X, box.Y, box.Width, box.Height, w, h);
                annotations.Add(new { id = annoId++, image_id = imgId, category_id = catId,
                    bbox = new[] { bx, by, bw, bh }, area = bw * bh, iscrowd = 0 });
            }
            imgId++;
        }

        File.WriteAllText(Path.Combine(root, "annotations.json"),
            JsonSerializer.Serialize(new { images, annotations, categories },
                new JsonSerializerOptions { WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
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

    private static List<Box> ParseBoxes(string json)
    {
        try { var l = JsonSerializer.Deserialize<List<Box>>(json); if (l != null) return l; } catch { }
        try { var s = JsonSerializer.Deserialize<Box>(json); if (s != null) return [s]; } catch { }
        return [];
    }

    private static (double, double, double, double) ClampBox(double x, double y, double w, double h, int iw, int ih)
    {
        x = Math.Max(0, Math.Min(x, iw - 1)); y = Math.Max(0, Math.Min(y, ih - 1));
        w = Math.Max(1, Math.Min(w, iw - x)); h = Math.Max(1, Math.Min(h, ih - y));
        return (x, y, w, h);
    }

    private class Box { public double X { get; set; } public double Y { get; set; } public double Width { get; set; } public double Height { get; set; } }
}
