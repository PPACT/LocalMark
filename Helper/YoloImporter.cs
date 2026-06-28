using System.IO;
using System.Text.Json;
using LocalMark.Model;

namespace LocalMark.Helper;

public class YoloImporter : IImportService
{
    public List<ImportResult> Import(string datasetPath)
    {
        var results = new List<ImportResult>();

        // 收集所有 labels/ 子目录（labels/train, labels/val, labels/）
        var lblDirs = new List<string>();
        var labelsRoot = FindDir(datasetPath, "labels");
        if (labelsRoot != null)
        {
            lblDirs.Add(labelsRoot);
            lblDirs.AddRange(Directory.GetDirectories(labelsRoot));
        }

        // 收集所有 images/ 子目录
        var imgDirs = new List<string>();
        var imagesRoot = FindDir(datasetPath, "images");
        if (imagesRoot != null)
        {
            imgDirs.Add(imagesRoot);
            imgDirs.AddRange(Directory.GetDirectories(imagesRoot));
        }

        var classNames = ReadClassNames(datasetPath);

        foreach (var lblDir in lblDirs)
        {
            foreach (var txtFile in Directory.GetFiles(lblDir, "*.txt"))
            {
                var baseName = Path.GetFileNameWithoutExtension(txtFile);
                var imgPath = FindImage(imgDirs, baseName);
                if (imgPath == null) continue;

                var (iw, ih) = GetImageSize(imgPath);
                if (iw == 0 || ih == 0) continue;

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

                    var w = nw * iw; var h = nh * ih;
                    var x = cx * iw - w / 2; var y = cy * ih - h / 2;
                    boxes.Add(new AnnotationBox
                    {
                        X = Math.Max(0, x), Y = Math.Max(0, y),
                        Width = w, Height = h,
                        Label = clsId < classNames.Count ? classNames[clsId] : $"class_{clsId}"
                    });
                }

                if (boxes.Count == 0) continue;
                results.Add(new ImportResult
                {
                    Source = new SourceData { DataType = 1, Content = imgPath,
                        SourceName = Path.GetFileName(imgPath), IsMarked = true, IsHighlighted = true },
                    Boxes = boxes
                });
            }
        }
        return results;
    }

    private static List<string> ReadClassNames(string root)
    {
        var cls = Path.Combine(root, "classes.txt");
        if (File.Exists(cls)) return File.ReadLines(cls).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        foreach (var f in Directory.GetFiles(root, "classes.txt", SearchOption.AllDirectories))
            if (File.Exists(f)) return File.ReadLines(f).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        return [];
    }

    private static string? FindDir(string root, string name)
    {
        var d = Path.Combine(root, name); if (Directory.Exists(d)) return d;
        foreach (var sd in Directory.GetDirectories(root, name, SearchOption.AllDirectories)) return sd;
        return null;
    }

    private static string? FindImage(List<string> dirs, string baseName)
    {
        foreach (var dir in dirs)
            foreach (var ext in new[] { ".jpg", ".jpeg", ".png", ".bmp" })
            { var p = Path.Combine(dir, baseName + ext); if (File.Exists(p)) return p; }
        return null;
    }

    private static (int, int) GetImageSize(string p)
    {
        try { using var fs = new FileStream(p, FileMode.Open, FileAccess.Read);
            var f = System.Windows.Media.Imaging.BitmapDecoder.Create(fs,
                System.Windows.Media.Imaging.BitmapCreateOptions.None,
                System.Windows.Media.Imaging.BitmapCacheOption.None).Frames[0];
            return (f.PixelWidth, f.PixelHeight); } catch { return (0, 0); }
    }
}
