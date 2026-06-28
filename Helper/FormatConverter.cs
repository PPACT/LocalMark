using System.IO;
using LocalMark.Model;

namespace LocalMark.Helper;

/// <summary>
/// 格式互转引擎：COCO ↔ VOC ↔ YOLO
/// 内部通过 ImportResult 桥接，importer 读 → exporter 写
/// </summary>
public static class FormatConverter
{
    public static int Convert(string inputDir, string outputDir, string? targetFormat = null)
    {
        // 1. 自动检测源格式并导入
        var (importer, srcFormat) = DetectImporter(inputDir);
        if (importer == null) return 0;

        var results = importer.Import(inputDir);
        if (results.Count == 0) return 0;

        // 2. 构建 SourceData + MarkResult 列表
        var sources = new List<SourceData>();
        var marks = new List<MarkResult>();
        int nextId = 1;

        foreach (var r in results)
        {
            r.Source.Id = nextId;
            sources.Add(r.Source);

            if (r.Boxes.Count > 0)
            {
                marks.Add(new MarkResult
                {
                    Id = nextId,
                    SourceId = nextId,
                    LabelName = r.Boxes[0].Label,
                    BoxPosition = System.Text.Json.JsonSerializer.Serialize(r.Boxes),
                    MarkedAt = DateTime.Now
                });
            }
            nextId++;
        }

        // 3. 选择目标格式导出
        var tf = targetFormat ?? (srcFormat == "COCO" ? "YOLO" : "COCO");
        var exporter = CreateExporter(tf);
        if (exporter == null) return 0;

        exporter.Export(sources, marks, outputDir);
        return results.Count;
    }

    public static string DetectFormat(string path)
    {
        bool HasFile(string n) => Directory.EnumerateFiles(path, n, SearchOption.AllDirectories).Any();
        bool HasDir(string n) => Directory.EnumerateDirectories(path, n, SearchOption.AllDirectories).Any();

        if (HasFile("annotations.json") || HasFile("instances_default.json")) return "COCO";
        if (HasDir("labels") || HasFile("data.yaml")) return "YOLO";
        if (HasDir("Annotations") || HasDir("annotations")) return "VOC";
        return "Unknown";
    }

    private static (IImportService?, string) DetectImporter(string path)
    {
        var fmt = DetectFormat(path);
        return fmt switch
        {
            "COCO" => ((IImportService?)new CocoImporter(), fmt),
            "YOLO" => ((IImportService?)new YoloImporter(), fmt),
            "VOC" => ((IImportService?)new VocImporter(), fmt),
            _ => (null, fmt)
        };
    }

    private static IExportService? CreateExporter(string format) => format switch
    {
        "COCO" => new CocoExporter(),
        "YOLO" => new YoloExporter(),
        "VOC" => new VocExporter(),
        _ => null
    };
}
