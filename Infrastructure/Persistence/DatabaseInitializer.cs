using Microsoft.Data.Sqlite;

namespace POS_system_cs.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public DatabaseInitializer(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await ExecuteAsync(connection, "PRAGMA foreign_keys = ON;", cancellationToken);
        await ExecuteAsync(connection, LoadSchema(), cancellationToken);
        await SeedDefaultsAsync(connection, cancellationToken);
    }

    private static async Task SeedDefaultsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var now = DateTime.Now.ToString("O");
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO categories (id, name, description, is_active, created_at)
            SELECT $id, $name, $description, 1, $createdAt
            WHERE NOT EXISTS (SELECT 1 FROM categories);
            """;
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("$name", "默认分类");
        command.Parameters.AddWithValue("$description", "系统初始化分类");
        command.Parameters.AddWithValue("$createdAt", now);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string LoadSchema()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Data", "schema.sql"),
            Path.Combine(Environment.CurrentDirectory, "Data", "schema.sql")
        };

        var schemaPath = candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException("未找到数据库建表脚本 Data/schema.sql。", candidates[0]);

        return File.ReadAllText(schemaPath);
    }
}
