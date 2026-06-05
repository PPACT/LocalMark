using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LocalMark.Model;

namespace LocalMark.Helper;

public class OllamaAgent
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OllamaAgent(string host = "http://localhost:11434", string model = "qwen2.5vl:7b")
    {
        _http = new HttpClient { BaseAddress = new Uri(host), Timeout = TimeSpan.FromMinutes(3) };
        _model = model;
    }

    public async Task<List<DetectedObject>> AnnotateImageAsync(
        string imagePath, List<string> labels, CancellationToken ct = default)
    {
        var imageBytes = await File.ReadAllBytesAsync(imagePath, ct);
        var (resizedBytes, scale) = ResizeImage(imageBytes, 1024);

        var imageB64 = Convert.ToBase64String(resizedBytes);
        var prompt = BuildPrompt(labels);

        var payload = new
        {
            model = _model,
            prompt,
            images = new[] { imageB64 },
            format = "json",
            stream = false
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("/api/generate", content, ct);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<OllamaResponse>(responseBody);

        if (result == null) return [];

        var detections = TryParseJson(result.Response, labels)
                         ?? FallbackParse(result.Response, labels);

        return detections.Select(d =>
        {
            d.X = d.X / scale;
            d.Y = d.Y / scale;
            d.Width = d.Width / scale;
            d.Height = d.Height / scale;
            return d;
        }).ToList();
    }

    private static (byte[] imageBytes, double scale) ResizeImage(byte[] original, int maxDimension)
    {
        using var inStream = new MemoryStream(original);
        var decoder = BitmapDecoder.Create(inStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
        var frame = decoder.Frames[0];
        var w = frame.PixelWidth;
        var h = frame.PixelHeight;
        var max = Math.Max(w, h);

        if (max <= maxDimension) return (original, 1.0);

        var scale = maxDimension / (double)max;
        var transform = new ScaleTransform(scale, scale);
        var scaled = new TransformedBitmap(frame, transform);

        var encoder = new JpegBitmapEncoder { QualityLevel = 85 };
        encoder.Frames.Add(BitmapFrame.Create(scaled));
        using var outStream = new MemoryStream();
        encoder.Save(outStream);
        return (outStream.ToArray(), scale);
    }

    private static string BuildPrompt(List<string> labels)
    {
        var labelList = string.Join("、", labels);
        return "逐一检查这张图片中的所有目标，每个目标独立标注一个框。"
               + "使用以下" + labels.Count + "个标签："
               + labelList + "。"
               + "标注规则：每个目标单独画框（不可合并），可见部分超过50%才标注。"
               + "输出JSON数组，格式："
               + @"[{""label"": ""标签名"", ""bbox_2d"": [x1,y1,x2,y2]}]。"
               + "确保 x1<x2 且 y1<y2。";
    }

    private static List<DetectedObject>? TryParseJson(string text, List<string> validLabels)
    {
        // 格式1: [{ "label": "...", "bbox_2d": [...] }]
        try
        {
            var raw = JsonSerializer.Deserialize<List<RawDetection>>(text);
            if (raw is { Count: > 0 })
                return raw.Select(r => ToDetectedObject(r, validLabels)).ToList();
        }
        catch { }

        // 格式2: { "机动车": [{"bbox_2d": [...]}], "行人": [...] }
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, List<BboxOnly>>>(text);
            if (dict is { Count: > 0 })
            {
                var results = new List<DetectedObject>();
                foreach (var (label, boxes) in dict)
                    foreach (var box in boxes)
                    {
                        var rd = new RawDetection { Label = label, Bbox2D = box.Bbox2D };
                        results.Add(ToDetectedObject(rd, validLabels));
                    }
                return results;
            }
        }
        catch { }

        return null;
    }

    private static List<DetectedObject> FallbackParse(string text, List<string> validLabels)
    {
        var results = new List<DetectedObject>();
        var matches = Regex.Matches(text, @"\{[^{}]*?""label""[^{}]*?""bbox_2d""[^{}]*?\[[^\]]*\][^{}]*?\}");
        foreach (Match m in matches)
        {
            var obj = m.Value;
            if (!obj.EndsWith('}')) obj += '}';
            try
            {
                var d = JsonSerializer.Deserialize<RawDetection>(obj);
                if (d != null) results.Add(ToDetectedObject(d, validLabels));
            }
            catch { }
        }
        return results;
    }

    private static DetectedObject ToDetectedObject(RawDetection raw, List<string> validLabels)
    {
        var x1 = raw.Bbox2D[0];
        var y1 = raw.Bbox2D[1];
        var x2 = raw.Bbox2D[2];
        var y2 = raw.Bbox2D[3];

        if (x1 > x2) (x1, x2) = (x2, x1);
        if (y1 > y2) (y1, y2) = (y2, y1);

        var label = validLabels.FirstOrDefault(l =>
            string.Equals(l, raw.Label, StringComparison.OrdinalIgnoreCase)) ?? raw.Label;

        return new DetectedObject
        {
            Label = label,
            X = x1,
            Y = y1,
            Width = x2 - x1,
            Height = y2 - y1
        };
    }

    private class RawDetection
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("bbox_2d")]
        public double[] Bbox2D { get; set; } = [];
    }

    private class BboxOnly
    {
        [JsonPropertyName("bbox_2d")]
        public double[] Bbox2D { get; set; } = [];
    }

    private class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }
}
