namespace LocalMark.Model;

public class AppSettings
{
    public string OutputDir { get; set; } = "";
    public OllamaConfig Ollama { get; set; } = new();
    public TextAiConfig TextAi { get; set; } = new();
}

public class OllamaConfig
{
    public string Host { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "qwen2.5vl:7b";
}

public class TextAiConfig
{
    public string Endpoint { get; set; } = "https://api.deepseek.com";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "deepseek-chat";
}
