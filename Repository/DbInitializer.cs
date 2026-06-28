using System.IO;
using Microsoft.Data.Sqlite;

namespace LocalMark.Repository;

/// <summary>
/// 数据库初始化：启动时判断 .db 是否存在，不存在则建库建表
/// </summary>
public static class DbInitializer
{
    public static string DbPath =>
        Environment.GetEnvironmentVariable("LOCALMARK_TEST_DB") ?? "localmark.db";

    public static string GetConnectionString() => $"Data Source={DbPath}";

    public static void Initialize()
    {
        // 判断数据库文件是否存在
        bool dbExists = File.Exists(DbPath);

        if (!dbExists)
        {
            CreateTables();
        }

        Migrate();
    }

    private static void Migrate()
    {
        using var conn = new SqliteConnection(GetConnectionString());
        conn.Open();
        // 为旧数据库补充 SourceName 列
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "ALTER TABLE SourceData ADD COLUMN SourceName NVARCHAR DEFAULT ''";
            cmd.ExecuteNonQuery();
        }
        catch { }

        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "ALTER TABLE MarkResult ADD COLUMN MarkedAt DATETIME";
            cmd.ExecuteNonQuery();
        }
        catch { }
    }

    private static void CreateTables()
    {
        using var connection = new SqliteConnection(GetConnectionString());
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS SourceData (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DataType INTEGER NOT NULL,
                Content NVARCHAR NOT NULL,
                SourceName NVARCHAR DEFAULT '',
                IsMarked INTEGER DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS MarkResult (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SourceId INTEGER NOT NULL,
                LabelName NVARCHAR NOT NULL,
                BoxPosition NVARCHAR,
                MarkedAt DATETIME,
                FOREIGN KEY (SourceId) REFERENCES SourceData(Id)
            );
        ";
        cmd.ExecuteNonQuery();
    }
}
