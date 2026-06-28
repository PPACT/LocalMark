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
        var markDict = marks.ToDictionary(m => m.SourceId);
        if (srcList.Count == 0) return;

        var root = Path.Combine(outputDir, $"yolo_{DateTime.Now:yyyyMMdd_HHmmss}");
        var imgDir = Path.Combine(root, "images");
        var lblDir = Path.Combine(root, "labels");
        Directory.CreateDirectory(imgDir);
        Directory.CreateDirectory(lblDir);

        var catMap = new Dictionary<string, int>();
        int nextCat = 0;
        var fileNames = new List<string>();

        foreach (var s in srcList)
        {
            if (!File.Exists(s.Content)) continue;
            if (!markDict.TryGetValue(s.Id, out var m) || string.IsNullOrEmpty(m.BoxPosition)) continue;

            var (iw, ih) = GetImageSize(s.Content);
            if (iw == 0 || ih == 0) continue;

            var baseName = Path.GetFileNameWithoutExtension(s.Content);
            var ext = Path.GetExtension(s.Content);
            var destImg = Path.Combine(imgDir, baseName + ext);
            File.Copy(s.Content, destImg, true);

            var lines = new List<string>();
            foreach (var box in ParseBoxes(m.BoxPosition))
            {
                var lbl = box.Label ?? m.LabelName;
                if (!catMap.TryGetValue(lbl, out var cid)) { cid = nextCat++; catMap[lbl] = cid; }
                var cx = (box.X + box.Width / 2) / iw;
                var cy = (box.Y + box.Height / 2) / ih;
                var nw = box.Width / iw;
                var nh = box.Height / ih;
                lines.Add($"{cid} {cx:F6} {cy:F6} {nw:F6} {nh:F6}");
            }

            File.WriteAllText(Path.Combine(lblDir, baseName + ".txt"), string.Join("\n", lines));
            fileNames.Add(baseName + ext);
        }

        // data.yaml
        var ordered = catMap.OrderBy(x => x.Value).ToList();
        var yaml = new StringBuilder();
        yaml.AppendLine($"path: {root.Replace('\\', '/')}");
        yaml.AppendLine("train: train.txt");
        yaml.AppendLine("val: val.txt");
        yaml.AppendLine($"nc: {ordered.Count}");
        yaml.AppendLine("names:");
        foreach (var kv in ordered) yaml.AppendLine($"  {kv.Value}: {kv.Key}");
        File.WriteAllText(Path.Combine(root, "data.yaml"), yaml.ToString());

        // train/val split (80/20)
        var split = (int)(fileNames.Count * 0.8);
        File.WriteAllText(Path.Combine(root, "train.txt"), string.Join("\n", fileNames.Take(split)));
        File.WriteAllText(Path.Combine(root, "val.txt"), string.Join("\n", fileNames.Skip(split)));
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

    private class Box { public double X { get; set; } public double Y { get; set; } public double Width { get; set; } public double Height { get; set; } public string? Label { get; set; } }
}
