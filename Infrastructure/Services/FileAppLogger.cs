using System.IO;
using System.Text;
using POS_system_cs.Application.Services;

namespace POS_system_cs.Infrastructure.Services;

public sealed class FileAppLogger : IAppLogger
{
    private const string LogDirectoryName = "logs";
    private static readonly Lock WriteLock = new();
    private readonly string _logDirectory;

    public FileAppLogger()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _logDirectory = Path.Combine(localAppData, "POS-system-cs", LogDirectoryName);
    }

    public string LogDirectory => _logDirectory;

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Error(string message, Exception? exception = null)
    {
        Write("ERROR", message, exception);
    }

    private void Write(string level, string message, Exception? exception = null)
    {
        try
        {
            Directory.CreateDirectory(_logDirectory);
            var path = Path.Combine(_logDirectory, $"app-{DateTime.Now:yyyyMMdd}.log");
            var content = BuildEntry(level, message, exception);

            lock (WriteLock)
            {
                File.AppendAllText(path, content, Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must not interrupt POS operations.
        }
    }

    private static string BuildEntry(string level, string message, Exception? exception)
    {
        var builder = new StringBuilder();
        builder.Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
        builder.Append(" [");
        builder.Append(level);
        builder.Append("] ");
        builder.AppendLine(message);

        if (exception is not null)
        {
            builder.AppendLine(exception.ToString());
        }

        return builder.ToString();
    }
}
