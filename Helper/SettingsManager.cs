using System.IO;
using System.Text.Json;
using LocalMark.Model;

namespace LocalMark.Helper;

public static class SettingsManager
{
    private static readonly string SettingsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Config", "appsettings.json");

    private static AppSettings? _cached;

    public static AppSettings Load()
    {
        if (_cached != null) return _cached;

        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _cached = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            else
            {
                _cached = new AppSettings();
            }
        }
        catch
        {
            _cached = new AppSettings();
        }

        return _cached;
    }

    public static void Save(AppSettings settings)
    {
        _cached = settings;
        var dir = Path.GetDirectoryName(SettingsPath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }

    public static void Reload() => _cached = null;
}
