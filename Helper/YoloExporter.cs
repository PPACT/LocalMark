using System.IO;
using System.Text;
using System.Text.Json;
using LocalMark.Model;

namespace LocalMark.Helper;

public class YoloExporter : IExportService
{
    public ExportFormat Format => ExportFormat.YOLO;

    public void Export(IEnumerable<SourceData> sources, IEnumerable<MarkResult> marks, string outputDir)
    {
        var srcList = sources.Where(s => s.DataType == 1 && s.IsMarked).ToList();
        var markDict = marks.Where(m => srcList.Any(s => s.Id == m.SourceId))
            .ToDictionary(m => m.SourceId);

        var catMap = new Dictionary<string, int>();
        int nextCatId = 0;

        var dir = Path.Combine(outputDir, $"yolo_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(dir);

        foreach (var s in srcList)
        {
            if (!File.Exists(s.Content)) continue;
            if (!markDict.TryGetValue(s.Id, out var m) || string.IsNullOrEmpty(m.BoxPosition)) continue;

            var (iw, ih) = GetImageSize(s.Content);
            if (iw == 0 || ih == 0) continue;

            var boxes = ParseBoxes(m.BoxPosition);
            if (boxes.Count == 0) continue;

            var lines = new List<string>();
            foreach (var box in boxes)
            {
                if (!catMap.TryGetValue(box.Label ?? m.LabelName, out var catId))
                {
                    catId = nextCatId++;
                    catMap[box.Label ?? m.LabelName] = catId;
                }
                var cx = (box.X + box.Width / 2) / iw;
                var cy = (box.Y + box.Height / 2) / ih;
                var nw = box.Width / iw;
                var nh = box.Height / ih;
                lines.Add($"{catId} {cx:F6} {cy:F6} {nw:F6} {nh:F6}");
            }

            var txtPath = Path.Combine(dir, Path.GetFileNameWithoutExtension(s.Content) + ".txt");
            File.WriteAllText(txtPath, string.Join("\n", lines));
        }

        // classes.txt
        var sb = new StringBuilder();
        foreach (var kv in catMap.OrderBy(x => x.Value))
            sb.AppendLine(kv.Key);
        File.WriteAllText(Path.Combine(dir, "classes.txt"), sb.ToString());
    }

    private static (int w, int h) GetImageSize(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var f = System.Windows.Media.Imaging.BitmapDecoder.Create(fs,
                System.Windows.Media.Imaging.BitmapCreateOptions.None,
                System.Windows.Media.Imaging.BitmapCacheOption.None).Frames[0];
            return (f.PixelWidth, f.PixelHeight);
        }
        catch { return (0, 0); }
    }

    private static List<Box> ParseBoxes(string json)
    {
        try { var list = JsonSerializer.Deserialize<List<Box>>(json); if (list != null) return list; }
        catch { }
        try { var single = JsonSerializer.Deserialize<Box>(json); if (single != null) return [single]; }
        catch { }
        return [];
    }

    private class Box { public double X { get; set; } public double Y { get; set; } public double Width { get; set; } public double Height { get; set; } public string? Label { get; set; } }
}
