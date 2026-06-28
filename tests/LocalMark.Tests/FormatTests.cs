using System.IO;
using LocalMark.Helper;

namespace LocalMark.Tests;

public class FormatTests
{
    [Fact]
    public void Detect_COCO_by_annotations_json()
    {
        var dir = Path.Combine(Path.GetTempPath(), "tf_coco_" + Guid.NewGuid());
        var sub = Path.Combine(dir, "annotations"); Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "instances_train.json"), "{}");
        try { Assert.Equal("COCO", FormatConverter.DetectFormat(dir)); }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void Detect_YOLO_by_labels_dir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "tf_yolo_" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(dir, "labels", "train"));
        try { Assert.Equal("YOLO", FormatConverter.DetectFormat(dir)); }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void Detect_VOC_by_Annotations_dir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "tf_voc_" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(dir, "Annotations"));
        try { Assert.Equal("VOC", FormatConverter.DetectFormat(dir)); }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void Detect_unknown_for_empty_folder()
    {
        var dir = Path.Combine(Path.GetTempPath(), "tf_empty_" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try { Assert.Equal("Unknown", FormatConverter.DetectFormat(dir)); }
        finally { Directory.Delete(dir, true); }
    }
}
