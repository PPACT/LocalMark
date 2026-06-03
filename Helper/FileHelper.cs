using System.IO;
using System.Linq;

namespace LocalMark.Helper;

/// <summary>
/// 本地文件读取辅助
/// </summary>
public static class FileHelper
{
    /// <summary>
    /// 读取文本文件全部内容
    /// </summary>
    public static string ReadTextFile(string filePath)
    {
        return File.ReadAllText(filePath);
    }

    /// <summary>
    /// 验证图片文件路径是否有效（存在且为常见图片格式）
    /// </summary>
    public static bool IsValidImagePath(string filePath)
    {
        if (!File.Exists(filePath)) return false;

        var ext = Path.GetExtension(filePath).ToLower();
        return new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" }.Contains(ext);
    }

    /// <summary>
    /// 获取目录下所有文本文件 (.txt)
    /// </summary>
    public static string[] GetTextFiles(string directoryPath)
    {
        return Directory.GetFiles(directoryPath, "*.txt");
    }

    /// <summary>
    /// 获取目录下所有图片文件
    /// </summary>
    public static string[] GetImageFiles(string directoryPath)
    {
        var extensions = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif" };
        return extensions.SelectMany(ext => Directory.GetFiles(directoryPath, ext)).ToArray();
    }
}
