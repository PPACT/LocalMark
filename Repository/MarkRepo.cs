using Microsoft.Data.Sqlite;
using LocalMark.Model;

namespace LocalMark.Repository;

/// <summary>
/// 标注结果仓储
/// </summary>
public class MarkRepo : IBaseRepo<MarkResult>
{
    public IEnumerable<MarkResult> GetAll()
    {
        var list = new List<MarkResult>();
        using var conn = new SqliteConnection(DbInitializer.GetConnectionString());
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, SourceId, LabelName, BoxPosition FROM MarkResult";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new MarkResult
            {
                Id = reader.GetInt32(0),
                SourceId = reader.GetInt32(1),
                LabelName = reader.GetString(2),
                BoxPosition = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }
        return list;
    }

    public MarkResult? GetById(int id)
    {
        using var conn = new SqliteConnection(DbInitializer.GetConnectionString());
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, SourceId, LabelName, BoxPosition FROM MarkResult WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", id);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new MarkResult
            {
                Id = reader.GetInt32(0),
                SourceId = reader.GetInt32(1),
                LabelName = reader.GetString(2),
                BoxPosition = reader.IsDBNull(3) ? null : reader.GetString(3)
            };
        }
        return null;
    }

    public int Insert(MarkResult entity)
    {
        using var conn = new SqliteConnection(DbInitializer.GetConnectionString());
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO MarkResult (SourceId, LabelName, BoxPosition)
                            VALUES (@SourceId, @LabelName, @BoxPosition); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@SourceId", entity.SourceId);
        cmd.Parameters.AddWithValue("@LabelName", entity.LabelName);
        cmd.Parameters.AddWithValue("@BoxPosition", (object?)entity.BoxPosition ?? DBNull.Value);
        return Convert.ToInt32((long)cmd.ExecuteScalar()!);
    }

    public void Update(MarkResult entity)
    {
        using var conn = new SqliteConnection(DbInitializer.GetConnectionString());
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE MarkResult SET SourceId = @SourceId, LabelName = @LabelName,
                            BoxPosition = @BoxPosition WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", entity.Id);
        cmd.Parameters.AddWithValue("@SourceId", entity.SourceId);
        cmd.Parameters.AddWithValue("@LabelName", entity.LabelName);
        cmd.Parameters.AddWithValue("@BoxPosition", (object?)entity.BoxPosition ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var conn = new SqliteConnection(DbInitializer.GetConnectionString());
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM MarkResult WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    public MarkResult? GetBySourceId(int sourceId)
    {
        using var conn = new SqliteConnection(DbInitializer.GetConnectionString());
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, SourceId, LabelName, BoxPosition FROM MarkResult WHERE SourceId = @SourceId";
        cmd.Parameters.AddWithValue("@SourceId", sourceId);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new MarkResult
            {
                Id = reader.GetInt32(0),
                SourceId = reader.GetInt32(1),
                LabelName = reader.GetString(2),
                BoxPosition = reader.IsDBNull(3) ? null : reader.GetString(3)
            };
        }
        return null;
    }
}
