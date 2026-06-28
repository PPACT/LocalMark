using System.Text.Json;
using LocalMark.Model;
using LocalMark.Repository;

namespace LocalMark.Tests;

public class RepositoryTests : IDisposable
{
    private readonly string _dbPath;
    public RepositoryTests() { _dbPath = $"rt_{Guid.NewGuid():N}.db"; Environment.SetEnvironmentVariable("LOCALMARK_TEST_DB", _dbPath); DbInitializer.Initialize(); }
    public void Dispose() { try { File.Delete(_dbPath); } catch { } }

    [Fact] public void Insert_and_getall() { var r = new SourceRepo(); r.Insert(new SourceData { DataType = 1, Content = "/a.jpg", SourceName = "a.jpg" }); Assert.Single(r.GetAll()); }
    [Fact] public void GetUnmarked_filters() { var r = new SourceRepo(); r.Insert(new SourceData { DataType = 1, IsMarked = true }); r.Insert(new SourceData { DataType = 1, IsMarked = false }); Assert.Single(r.GetUnmarked()); }
    [Fact] public void Update_persists() { var r = new SourceRepo(); var id = r.Insert(new SourceData { DataType = 1, Content = "/old.jpg" }); var e = r.GetById(id)!; e.SourceName = "new.jpg"; r.Update(e); Assert.Equal("new.jpg", r.GetById(id)!.SourceName); }
    [Fact] public void Delete_removes() { var r = new SourceRepo(); var id = r.Insert(new SourceData { DataType = 0 }); r.Delete(id); Assert.Null(r.GetById(id)); }
    [Fact] public void Mark_Insert_and_GetBySourceId() { var sr = new SourceRepo(); var sid = sr.Insert(new SourceData { DataType = 1 }); var r = new MarkRepo(); var j = JsonSerializer.Serialize(new[] { new { x = 10, y = 10, w = 50, h = 50, label = "car" } }); r.Insert(new MarkResult { SourceId = sid, LabelName = "car", BoxPosition = j }); Assert.NotNull(r.GetBySourceId(sid)); }
}
