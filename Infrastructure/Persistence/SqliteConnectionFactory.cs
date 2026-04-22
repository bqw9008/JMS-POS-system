using System.IO;
using Microsoft.Data.Sqlite;
using POS_system_cs.Configuration;

namespace POS_system_cs.Infrastructure.Persistence;

public sealed class SqliteConnectionFactory
{
    private readonly AppSettings _settings;

    public SqliteConnectionFactory(AppSettings settings)
    {
        _settings = settings;
    }

    public SqliteConnection CreateConnection()
    {
        var databasePath = Path.IsPathRooted(_settings.DatabasePath)
            ? _settings.DatabasePath
            : Path.Combine(AppContext.BaseDirectory, _settings.DatabasePath);

        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        return new SqliteConnection($"Data Source={databasePath}");
    }
}

