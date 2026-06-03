using System.IO;
using System.Text.Json;
using LocalMark.Model;

namespace LocalMark.Helper;

/// <summary>
/// JSON 序列化导出
/// </summary>
public static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, Options);
    }

    public static void ExportToFile<T>(T obj, string filePath)
    {
        var json = Serialize(obj);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// 导出标注结果（素材 + 标注 联合数据）
    /// </summary>
    public static string ExportMarkedData(IEnumerable<SourceData> sources, IEnumerable<MarkResult> marks, string filePath)
    {
        var data = sources.Where(s => s.IsMarked).Select(s =>
        {
            var mark = marks.FirstOrDefault(m => m.SourceId == s.Id);
            return new
            {
                SourceId = s.Id,
                DataType = s.DataType == 0 ? "Text" : "Image",
                Content = s.Content,
                Label = mark?.LabelName ?? "",
                BoxPosition = mark?.BoxPosition
            };
        });

        var json = Serialize(data);
        File.WriteAllText(filePath, json);
        return json;
    }
}
