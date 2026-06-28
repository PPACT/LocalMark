using System.Text.Json;
using LocalMark.Helper;
using LocalMark.Model;

namespace LocalMark.Tests;

public class QualityCheckerTests
{
    [Fact]
    public void Reports_missing_when_not_marked()
    {
        var sources = new[] { new SourceData { Id = 1, DataType = 1, Content = "test.jpg", SourceName = "test.jpg" } };
        var issues = QualityChecker.Check(sources, []);
        Assert.Contains(issues, i => i.Type == "漏标");
    }

    [Fact]
    public void No_issues_for_valid_box()
    {
        var json = JsonSerializer.Serialize(new[] { new AnnotationBox { X = 10, Y = 10, Width = 100, Height = 100, Label = "car" } });
        var src = new[] { new SourceData { Id = 1, DataType = 1, Content = "test.jpg", SourceName = "test.jpg", IsMarked = true } };
        var mrk = new[] { new MarkResult { Id = 1, SourceId = 1, LabelName = "car", BoxPosition = json } };
        Assert.Empty(QualityChecker.Check(src, mrk));
    }

    [Fact]
    public void Reports_empty_when_box_position_null()
    {
        var src = new[] { new SourceData { Id = 1, DataType = 1, Content = "test.jpg", SourceName = "test.jpg", IsMarked = true } };
        var mrk = new[] { new MarkResult { Id = 1, SourceId = 1, LabelName = "car", BoxPosition = "" } };
        Assert.Contains(QualityChecker.Check(src, mrk), i => i.Type == "空标注");
    }

    [Fact]
    public void Reports_small_for_tiny_box()
    {
        var json = JsonSerializer.Serialize(new[] { new AnnotationBox { X = 0, Y = 0, Width = 2, Height = 2, Label = "car" } });
        var src = new[] { new SourceData { Id = 1, DataType = 1, Content = "test.jpg", SourceName = "test.jpg", IsMarked = true } };
        var mrk = new[] { new MarkResult { Id = 1, SourceId = 1, LabelName = "car", BoxPosition = json } };
        Assert.Contains(QualityChecker.Check(src, mrk), i => i.Type == "过小");
    }
}
