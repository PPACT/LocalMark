using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LocalMark.Helper;

public class TextAIAgent
{
    private readonly HttpClient _http;
    private readonly string _model;

    public TextAIAgent()
    {
        var settings = SettingsManager.Load();
        var key = settings.TextAi.ApiKey;
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException(
                "文本 AI 未配置。请在主界面点击「设置」按钮，填写 API Key。");

        _http = new HttpClient
        {
            BaseAddress = new Uri(settings.TextAi.Endpoint),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", key);
        _model = settings.TextAi.Model;
    }

    public async Task<string> ClassifyAsync(string text, List<string> labels, CancellationToken ct = default)
    {
        var labelList = string.Join("、", labels);
        var prompt = $"你是一个文本分类助手。请将以下文本归类到给定的标签之一。"
                     + $"可用标签：{labelList}。"
                     + $"只回复标签名称，不要回复其他内容。\n\n文本：{text}";

        var payload = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.1,
            max_tokens = 20
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);
        var answer = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()?.Trim() ?? "";

        return labels.FirstOrDefault(l =>
            string.Equals(l, answer, StringComparison.OrdinalIgnoreCase)
            || answer.Contains(l, StringComparison.OrdinalIgnoreCase)) ?? labels[0];
    }
}
