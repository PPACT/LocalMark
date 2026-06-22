using Microsoft.Data.Sqlite;
using LocalMark.Model;

namespace LocalMark.Repository;

/// <summary>
/// 素材数据仓储
/// </summary>
public class SourceRepo : IBaseRepo<SourceData>
{
    public IEnumerable<SourceData> GetAll()
    {
        var list = new List<SourceData>();
        using var conn = new SqliteConnection(DbInitializer.GetConnectionString());
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, DataType, Content, SourceName, IsMarked FROM SourceData";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new SourceData
            {
                Id = reader.GetInt32(0),
                DataType = reader.GetInt32(1),
                Content = reader.GetString(2),
                SourceName = reader.GetString(3),
                IsMarked = reader.GetInt32(4) == 1
            });
        }
        return list;
    }

    public SourceData? GetById(int id)
    {
        using var conn = new SqliteConnection(DbInitializer.GetConnectionString());
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, DataType, Content, SourceName, IsMarked FROM SourceData WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", id);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new SourceData
            {
                Id = reader.GetInt32(0),
                DataType = reader.GetInt32(1),
                Content = reader.GetString(2),
                SourceName = reader.GetString(3),
                IsMarked = reader.GetInt32(4) == 1
            };
        }
        return null;
    }

    public int Insert(SourceData entity)
    {
        using var conn = new SqliteConnection(DbInitializer.GetConnectionString());
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO SourceData (DataType, Content, SourceName, IsMarked) VALUES (@DataType, @Content, @SourceName, @IsMarked); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@DataType", entity.DataType);
        cmd.Parameters.AddWithValue("@Content", entity.Content);
        cmd.Parameters.AddWithValue("@SourceName", entity.SourceName ?? "");
        cmd.Parameters.AddWithValue("@IsMarked", entity.IsMarked ? 1 : 0);
        return Convert.ToInt32((long)cmd.ExecuteScalar()!);
    }

    public void Update(SourceData entity)
    {
        using var conn = new SqliteConnection(DbInitializer.GetConnectionString());
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE SourceData SET DataType = @DataType, Content = @Content, SourceName = @SourceName, IsMarked = @IsMarked WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", entity.Id);
        cmd.Parameters.AddWithValue("@DataType", entity.DataType);
        cmd.Parameters.AddWithValue("@Content", entity.Content);
        cmd.Parameters.AddWithValue("@SourceName", entity.SourceName ?? "");
        cmd.Parameters.AddWithValue("@IsMarked", entity.IsMarked ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var conn = new SqliteConnection(DbInitializer.GetConnectionString());
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM SourceData WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    public IEnumerable<SourceData> GetUnmarked()
    {
        var list = new List<SourceData>();
        using var conn = new SqliteConnection(DbInitializer.GetConnectionString());
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, DataType, Content, SourceName, IsMarked FROM SourceData WHERE IsMarked = 0";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new SourceData
            {
                Id = reader.GetInt32(0),
                DataType = reader.GetInt32(1),
                Content = reader.GetString(2),
                SourceName = reader.GetString(3),
                IsMarked = reader.GetInt32(4) == 1
            });
        }
        return list;
    }
}
