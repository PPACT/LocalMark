using System.IO;
using LocalMark.Model;

namespace LocalMark.Helper;

public class YoloImporter : IImportService
{
    public List<ImportResult> Import(string datasetPath)
    {
        var results = new List<ImportResult>();

        var lblDir = FindSubDir(datasetPath, "labels");
        var imgDir = FindSubDir(datasetPath, "images");
        if (lblDir == null || imgDir == null) return results;

        // 读取 classes.txt 或 data.yaml
        var classNames = ReadClassNames(datasetPath);

        foreach (var txtFile in Directory.GetFiles(lblDir, "*.txt"))
        {
            var baseName = Path.GetFileNameWithoutExtension(txtFile);
            var imgPath = FindImageFile(imgDir, baseName);
            if (imgPath == null) continue;

            var boxes = new List<AnnotationBox>();
            foreach (var line in File.ReadLines(txtFile))
            {
                var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5) continue;
                if (!int.TryParse(parts[0], out var clsId)) continue;
                if (!float.TryParse(parts[1], out var cx)) continue;
                if (!float.TryParse(parts[2], out var cy)) continue;
                if (!float.TryParse(parts[3], out var nw)) continue;
                if (!float.TryParse(parts[4], out var nh)) continue;

                var (iw, ih) = GetImageSize(imgPath);
                if (iw == 0 || ih == 0) continue;

                // YOLO 归一化坐标 → 图像素坐标
                var w = nw * iw; var h = nh * ih;
                var x = cx * iw - w / 2; var y = cy * ih - h / 2;

                boxes.Add(new AnnotationBox
                {
                    X = (int)Math.Max(0, x), Y = (int)Math.Max(0, y),
                    Width = (int)w, Height = (int)h,
                    Label = clsId < classNames.Count ? classNames[clsId] : $"class_{clsId}"
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

        return results;
    }

    private static List<string> ReadClassNames(string root)
    {
        // 优先 data.yaml
        var yaml = Path.Combine(root, "data.yaml");
        if (File.Exists(yaml))
        {
            var names = new List<string>();
            bool inNames = false;
            foreach (var line in File.ReadLines(yaml))
            {
                if (line.Trim().StartsWith("names:")) { inNames = true; continue; }
                if (inNames && line.Contains(':'))
                {
                    var val = line.Split(':', 2)[1].Trim();
                    if (!string.IsNullOrEmpty(val)) names.Add(val);
                }
                else if (inNames && !line.Contains(':')) break;
            }
            if (names.Count > 0) return names;
        }
        // classes.txt
        var cls = Path.Combine(root, "classes.txt");
        if (File.Exists(cls)) return File.ReadLines(cls).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        return [];
    }

    private static string? FindSubDir(string root, string name)
    {
        var d = Path.Combine(root, name); if (Directory.Exists(d)) return d;
        foreach (var sd in Directory.GetDirectories(root, name, SearchOption.AllDirectories))
            return sd;
        return null;
    }

    private static string? FindImageFile(string dir, string baseName)
    {
        foreach (var ext in new[] { ".jpg", ".jpeg", ".png", ".bmp" })
        {
            var p = Path.Combine(dir, baseName + ext); if (File.Exists(p)) return p;
        }
        return null;
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
