using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using LocalMark.Model;

namespace LocalMark.Helper;

public class VocExporter : IExportService
{
    public ExportFormat Format => ExportFormat.VOC;

    public void Export(IEnumerable<SourceData> sources, IEnumerable<MarkResult> marks, string outputDir)
    {
        var srcList = sources.Where(s => s.DataType == 1 && s.IsMarked).ToList();
        var markDict = marks.ToDictionary(m => m.SourceId);
        if (srcList.Count == 0) return;

        var root = Path.Combine(outputDir, $"voc_{DateTime.Now:yyyyMMdd_HHmmss}");
        var imgDir = Path.Combine(root, "JPEGImages");
        var annDir = Path.Combine(root, "Annotations");
        Directory.CreateDirectory(imgDir);
        Directory.CreateDirectory(annDir);

        foreach (var s in srcList)
        {
            if (!File.Exists(s.Content)) continue;
            if (!markDict.TryGetValue(s.Id, out var m) || string.IsNullOrEmpty(m.BoxPosition)) continue;

            var (iw, ih) = GetImageSize(s.Content);
            if (iw == 0 || ih == 0) continue;

            var baseName = Path.GetFileNameWithoutExtension(s.Content);
            var ext = Path.GetExtension(s.Content);
            File.Copy(s.Content, Path.Combine(imgDir, baseName + ext), true);

            var xmlObjects = ParseBoxes(m.BoxPosition).Select(box =>
            {
                var xmin = (int)Math.Max(0, Math.Min(box.X, iw - 1));
                var ymin = (int)Math.Max(0, Math.Min(box.Y, ih - 1));
                var xmax = (int)Math.Max(1, Math.Min(box.X + box.Width, iw));
                var ymax = (int)Math.Max(1, Math.Min(box.Y + box.Height, ih));
                return new XElement("object",
                    new XElement("name", box.Label ?? m.LabelName),
                    new XElement("bndbox",
                        new XElement("xmin", xmin), new XElement("ymin", ymin),
                        new XElement("xmax", xmax), new XElement("ymax", ymax)));
            });

            var xml = new XElement("annotation",
                new XElement("folder", "JPEGImages"),
                new XElement("filename", baseName + ext),
                new XElement("path", Path.Combine(imgDir, baseName + ext)),
                new XElement("source", new XElement("database", "LocalMark")),
                new XElement("size",
                    new XElement("width", iw), new XElement("height", ih),
                    new XElement("depth", 3)),
                xmlObjects);

            File.WriteAllText(Path.Combine(annDir, baseName + ".xml"), xml.ToString());
        }
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
