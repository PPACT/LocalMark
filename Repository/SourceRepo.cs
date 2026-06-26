using Dapper;
using Microsoft.Data.Sqlite;
using LocalMark.Model;

namespace LocalMark.Repository;

public class SourceRepo : IBaseRepo<SourceData>
{
    private static SqliteConnection GetConn() => new(DbInitializer.GetConnectionString());

    public IEnumerable<SourceData> GetAll()
    {
        using var conn = GetConn();
        return conn.Query<SourceData>("SELECT Id, DataType, Content, SourceName, IsMarked FROM SourceData");
    }

    public SourceData? GetById(int id)
    {
        using var conn = GetConn();
        return conn.QueryFirstOrDefault<SourceData>(
            "SELECT Id, DataType, Content, SourceName, IsMarked FROM SourceData WHERE Id = @Id", new { Id = id });
    }

    public int Insert(SourceData entity)
    {
        using var conn = GetConn();
        return conn.ExecuteScalar<int>(
            "INSERT INTO SourceData (DataType, Content, SourceName, IsMarked) VALUES (@DataType, @Content, @SourceName, @IsMarked); SELECT last_insert_rowid();", entity);
    }

    public void Update(SourceData entity)
    {
        using var conn = GetConn();
        conn.Execute("UPDATE SourceData SET DataType=@DataType, Content=@Content, SourceName=@SourceName, IsMarked=@IsMarked WHERE Id=@Id", entity);
    }

    public void Delete(int id)
    {
        using var conn = GetConn();
        conn.Execute("DELETE FROM SourceData WHERE Id = @Id", new { Id = id });
    }

    public IEnumerable<SourceData> GetUnmarked()
    {
        using var conn = GetConn();
        return conn.Query<SourceData>("SELECT Id, DataType, Content, SourceName, IsMarked FROM SourceData WHERE IsMarked = 0");
    }
}
