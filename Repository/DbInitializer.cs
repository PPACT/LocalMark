using System.IO;
using Microsoft.Data.Sqlite;

namespace LocalMark.Repository;

public static class DbInitializer
{
    public const string DbPath = "localmark.db";

    private static string ConnectionString => $"Data Source={DbPath}";

    public static void Initialize()
    {
        bool dbExists = File.Exists(DbPath);

        if (!dbExists)
        {
            CreateTables();
        }

        Migrate();
    }

    public static string GetConnectionString() => ConnectionString;

    private static void CreateTables()
    {
        using var connection = new SqliteConnection(ConnectionString);
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
                FOREIGN KEY (SourceId) REFERENCES SourceData(Id)
            );
        ";
        cmd.ExecuteNonQuery();
    }

    private static void Migrate()
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "ALTER TABLE SourceData ADD COLUMN SourceName NVARCHAR DEFAULT ''";
            cmd.ExecuteNonQuery();
        }
        catch { }
    }
}
