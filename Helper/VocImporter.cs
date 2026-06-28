using System.IO;
using System.Xml.Linq;
using LocalMark.Model;

namespace LocalMark.Helper;

public class VocImporter : IImportService
{
    public List<ImportResult> Import(string datasetPath)
    {
        var results = new List<ImportResult>();

        var annDir = FindSubDir(datasetPath, "Annotations");
        var imgDir = FindSubDir(datasetPath, "JPEGImages");
        if (annDir == null || imgDir == null) return results;

        foreach (var xmlFile in Directory.GetFiles(annDir, "*.xml"))
        {
            try
            {
                var xml = XDocument.Load(xmlFile).Root;
                if (xml == null) continue;

                var fileName = xml.Element("filename")?.Value ?? "";
                var imgPath = FindImageFile(imgDir, fileName);
                if (imgPath == null) continue;

                var boxes = new List<AnnotationBox>();
                foreach (var obj in xml.Elements("object"))
                {
                    var name = obj.Element("name")?.Value ?? "unknown";
                    var bndbox = obj.Element("bndbox");
                    if (bndbox == null) continue;

                    var xmin = int.Parse(bndbox.Element("xmin")?.Value ?? "0");
                    var ymin = int.Parse(bndbox.Element("ymin")?.Value ?? "0");
                    var xmax = int.Parse(bndbox.Element("xmax")?.Value ?? "0");
                    var ymax = int.Parse(bndbox.Element("ymax")?.Value ?? "0");

                    boxes.Add(new AnnotationBox
                    {
                        X = xmin, Y = ymin,
                        Width = xmax - xmin, Height = ymax - ymin,
                        Label = name
                    });
                }

                if (boxes.Count == 0) continue;

                results.Add(new ImportResult
                {
                    Source = new SourceData
                    {
                        DataType = 1, Content = imgPath,
                        SourceName = Path.GetFileName(imgPath),
                        IsMarked = true, IsHighlighted = true
                    },
                    Boxes = boxes
                });
            }
            catch { }
        }

        return results;
    }

    private static string? FindSubDir(string root, string name)
    {
        var d = Path.Combine(root, name); if (Directory.Exists(d)) return d;
        foreach (var sd in Directory.GetDirectories(root, name, SearchOption.AllDirectories)) return sd;
        return null;
    }

    private static string? FindImageFile(string dir, string fileName)
    {
        var full = Path.Combine(dir, fileName); if (File.Exists(full)) return full;
        foreach (var f in Directory.GetFiles(dir, fileName, SearchOption.AllDirectories)) return f;
        // 尝试不同扩展名
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        foreach (var ext in new[] { ".jpg", ".jpeg", ".png", ".bmp" })
        {
            var p = Path.Combine(dir, baseName + ext); if (File.Exists(p)) return p;
        }
        return null;
    }
}
