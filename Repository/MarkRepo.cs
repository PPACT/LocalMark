using Dapper;
using Microsoft.Data.Sqlite;
using LocalMark.Model;

namespace LocalMark.Repository;

public class MarkRepo : IBaseRepo<MarkResult>
{
    private static SqliteConnection GetConn() => new(DbInitializer.GetConnectionString());

    public IEnumerable<MarkResult> GetAll()
    {
        using var conn = GetConn();
        return conn.Query<MarkResult>("SELECT Id, SourceId, LabelName, BoxPosition, MarkedAt FROM MarkResult");
    }

    public MarkResult? GetById(int id)
    {
        using var conn = GetConn();
        return conn.QueryFirstOrDefault<MarkResult>("SELECT Id, SourceId, LabelName, BoxPosition, MarkedAt FROM MarkResult WHERE Id = @Id", new { Id = id });
    }

    public MarkResult? GetBySourceId(int sourceId)
    {
        using var conn = GetConn();
        return conn.QueryFirstOrDefault<MarkResult>("SELECT Id, SourceId, LabelName, BoxPosition, MarkedAt FROM MarkResult WHERE SourceId = @SourceId", new { SourceId = sourceId });
    }

    public int Insert(MarkResult entity)
    {
        using var conn = GetConn();
        return conn.ExecuteScalar<int>(
            "INSERT INTO MarkResult (SourceId, LabelName, BoxPosition, MarkedAt) VALUES (@SourceId, @LabelName, @BoxPosition, @MarkedAt); SELECT last_insert_rowid();", entity);
    }

    public void Update(MarkResult entity)
    {
        using var conn = GetConn();
        conn.Execute("UPDATE MarkResult SET SourceId=@SourceId, LabelName=@LabelName, BoxPosition=@BoxPosition, MarkedAt=@MarkedAt WHERE Id=@Id", entity);
    }

    public void Delete(int id)
    {
        using var conn = GetConn();
        conn.Execute("DELETE FROM MarkResult WHERE Id = @Id", new { Id = id });
    }
}
