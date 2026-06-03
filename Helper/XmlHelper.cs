using System.Xml.Linq;
using LocalMark.Model;

namespace LocalMark.Helper;

/// <summary>
/// 读取 XML 标签配置
/// </summary>
public static class XmlHelper
{
    public static List<LabelItem> LoadTextLabels(string configPath)
    {
        var doc = XDocument.Load(configPath);
        return doc.Descendants("TextLabels")
                  .Elements("Label")
                  .Select(e => new LabelItem { Name = e.Value, Type = "Text" })
                  .ToList();
    }

    public static List<LabelItem> LoadImageLabels(string configPath)
    {
        var doc = XDocument.Load(configPath);
        return doc.Descendants("ImageLabels")
                  .Elements("Label")
                  .Select(e => new LabelItem { Name = e.Value, Type = "Image" })
                  .ToList();
    }

    public static List<LabelItem> LoadAllLabels(string configPath)
    {
        var labels = LoadTextLabels(configPath);
        labels.AddRange(LoadImageLabels(configPath));
        return labels;
    }
}
