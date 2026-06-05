using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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
        var imageB64 = Convert.ToBase64String(await File.ReadAllBytesAsync(imagePath, ct));
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

        return TryParseJson(result.Response, labels)
               ?? FallbackParse(result.Response, labels);
    }

    private static string BuildPrompt(List<string> labels)
    {
        var labelList = string.Join("、", labels);
        return "分析这张图片。你只能使用以下" + labels.Count + "个标签来标注物体："
               + labelList + "。只标注清晰可见、距离适中的主要目标（不超过10个）。"
               + "对每个目标输出JSON数组，每个元素格式："
               + @"{""label"": ""标签名"", ""bbox_2d"": [x1,y1,x2,y2]}。"
               + "确保 x1<x2 且 y1<y2。";
    }

    private static List<DetectedObject>? TryParseJson(string text, List<string> validLabels)
    {
        try
        {
            var raw = JsonSerializer.Deserialize<List<RawDetection>>(text);
            if (raw == null || raw.Count == 0) return null;
            return raw.Select(r => ToDetectedObject(r, validLabels)).ToList();
        }
        catch
        {
            return null;
        }
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

    private class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }
}
