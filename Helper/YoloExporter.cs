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

            var box = ParseBox(m.BoxPosition);
            if (box == null) continue;

            if (!catMap.TryGetValue(m.LabelName, out var catId))
            {
                catId = nextCatId++;
                catMap[m.LabelName] = catId;
            }

            var cx = (box.X + box.Width / 2) / iw;
            var cy = (box.Y + box.Height / 2) / ih;
            var nw = box.Width / iw;
            var nh = box.Height / ih;

            var txtPath = Path.Combine(dir, Path.GetFileNameWithoutExtension(s.Content) + ".txt");
            File.WriteAllText(txtPath, $"{catId} {cx:F6} {cy:F6} {nw:F6} {nh:F6}");
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

    private static Box? ParseBox(string j)
    {
        try { return JsonSerializer.Deserialize<Box>(j); }
        catch { return null; }
    }

    private class Box { public double X { get; set; } public double Y { get; set; } public double Width { get; set; } public double Height { get; set; } }
}
