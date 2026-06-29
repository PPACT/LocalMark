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

        var root = Path.Combine(outputDir, "yolo", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        var imgTrain = Path.Combine(root, "images", "train");
        var imgVal = Path.Combine(root, "images", "val");
        var lblTrain = Path.Combine(root, "labels", "train");
        var lblVal = Path.Combine(root, "labels", "val");
        Directory.CreateDirectory(imgTrain); Directory.CreateDirectory(imgVal);
        Directory.CreateDirectory(lblTrain); Directory.CreateDirectory(lblVal);

        var catMap = new Dictionary<string, int>();
        int nextCat = 0;
        var fileNames = new List<string>();
        var split = (int)(srcList.Count * 0.8);

        for (int idx = 0; idx < srcList.Count; idx++)
        {
            var s = srcList[idx];
            var isTrain = idx < split;
            if (!File.Exists(s.Content)) continue;
            if (!markDict.TryGetValue(s.Id, out var m) || string.IsNullOrEmpty(m.BoxPosition)) continue;

            var (iw, ih) = GetImageSize(s.Content);
            if (iw == 0 || ih == 0) continue;

            var ext = Path.GetExtension(s.Content);
            var destName = $"{Path.GetFileNameWithoutExtension(s.Content)}{ext}";
            var destImg = Path.Combine(isTrain ? imgTrain : imgVal, destName);
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

            var lblDest = Path.Combine(isTrain ? lblTrain : lblVal,
                Path.GetFileNameWithoutExtension(s.Content) + ".txt");
            File.WriteAllText(lblDest, string.Join("\n", lines));
            fileNames.Add(destName);
        }

        // classes.txt
        var ordered = catMap.OrderBy(x => x.Value).ToList();
        File.WriteAllText(Path.Combine(root, "classes.txt"),
            string.Join("\n", ordered.Select(kv => kv.Key)));
    }

    private static (int, int) GetImageSize(string path)
    {
        try { using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var f = System.Windows.Media.Imaging.BitmapDecoder.Create(fs,
                System.Windows.Media.Imaging.BitmapCreateOptions.None,
                System.Windows.Media.Imaging.BitmapCacheOption.None).Frames[0];
            return (f.PixelWidth, f.PixelHeight); } catch { return (0, 0); }
    }

    private static List<Box> ParseBoxes(string json)
    {
        try { var l = JsonSerializer.Deserialize<List<Box>>(json); if (l != null) return l; } catch { }
        try { var s = JsonSerializer.Deserialize<Box>(json); if (s != null) return [s]; } catch { }
        return [];
    }

    private class Box { public double X { get; set; } public double Y { get; set; } public double Width { get; set; } public double Height { get; set; } public string? Label { get; set; } }
}
