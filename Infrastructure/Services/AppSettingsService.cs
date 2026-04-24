using System.IO;
using System.Text.Json;
using POS_system_cs.Configuration;

namespace POS_system_cs.Infrastructure.Services;

internal sealed class AppSettingsService
{
    private const string SettingsFileName = "app-settings.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _settingsPath;

    public AppSettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _settingsPath = Path.Combine(appData, "POS-system-cs", SettingsFileName);
    }

    public string SettingsPath => _settingsPath;

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettings();
            }

            var persisted = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath));
            return Sanitize(persisted);
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(Sanitize(settings), JsonOptions));
    }

    private static AppSettings Sanitize(AppSettings? settings)
    {
        var sanitized = settings?.Clone() ?? new AppSettings();
        sanitized.StoreName = string.IsNullOrWhiteSpace(sanitized.StoreName) ? "小型商超" : sanitized.StoreName.Trim();
        sanitized.DatabasePath = string.IsNullOrWhiteSpace(sanitized.DatabasePath) ? "pos.db" : sanitized.DatabasePath.Trim();
        sanitized.ReceiptPrinterName = sanitized.ReceiptPrinterName?.Trim() ?? string.Empty;
        return sanitized;
    }
}
