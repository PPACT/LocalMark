using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using LocalMark.Model;

namespace LocalMark.Helper;

public class CocoImporter : IImportService
{
    public List<ImportResult> Import(string datasetPath)
    {
        var results = new List<ImportResult>();

        // 查找 annotations.json（可能在根目录或子目录）
        var jsonPath = Directory.GetFiles(datasetPath, "annotations.json", SearchOption.AllDirectories)
            .FirstOrDefault();
        if (jsonPath == null) return results;

        var coco = JsonSerializer.Deserialize<CocoRoot>(File.ReadAllText(jsonPath));
        if (coco?.images == null || coco.annotations == null) return results;

        var imgDir = Path.GetDirectoryName(jsonPath);
        if (!Directory.Exists(imgDir)) imgDir = datasetPath;

        // 尝试 images/ 子目录
        var imagesSubDir = Path.Combine(imgDir!, "images");
        if (Directory.Exists(imagesSubDir)) imgDir = imagesSubDir;

        var catMap = (coco.categories ?? []).ToDictionary(c => c.id, c => c.name);

        foreach (var img in coco.images)
        {
            var imgPath = FindImageFile(imgDir!, img.file_name);
            if (imgPath == null) continue;

            var anns = coco.annotations
                .Where(a => a.image_id == img.id)
                .Select(a => new AnnotationBox
                {
                    X = a.bbox[0], Y = a.bbox[1],
                    Width = a.bbox[2], Height = a.bbox[3],
                    Label = catMap.TryGetValue(a.category_id, out var name) ? name : "unknown"
                }).ToList();

            results.Add(new ImportResult
            {
                Source = new SourceData
                {
                    DataType = 1, Content = imgPath,
                    SourceName = img.file_name,
                    IsMarked = anns.Count > 0, IsHighlighted = true
                },
                Boxes = anns
            });
        }

        return results;
    }

    private static string? FindImageFile(string dir, string fileName)
    {
        var full = Path.Combine(dir, fileName);
        if (File.Exists(full)) return full;
        // 搜索子目录
        foreach (var f in Directory.GetFiles(dir, fileName, SearchOption.AllDirectories))
            return f;
        return null;
    }

    private class CocoRoot
    {
        [JsonPropertyName("images")] public List<CocoImage>? images { get; set; }
        [JsonPropertyName("annotations")] public List<CocoAnnotation>? annotations { get; set; }
        [JsonPropertyName("categories")] public List<CocoCategory>? categories { get; set; }
    }

    private class CocoImage
    {
        [JsonPropertyName("id")] public int id { get; set; }
        [JsonPropertyName("file_name")] public string file_name { get; set; } = "";
    }

    private class CocoAnnotation
    {
        [JsonPropertyName("id")] public int id { get; set; }
        [JsonPropertyName("image_id")] public int image_id { get; set; }
        [JsonPropertyName("category_id")] public int category_id { get; set; }
        [JsonPropertyName("bbox")] public double[] bbox { get; set; } = [];
    }

    private class CocoCategory
    {
        [JsonPropertyName("id")] public int id { get; set; }
        [JsonPropertyName("name")] public string name { get; set; } = "";
    }
}
