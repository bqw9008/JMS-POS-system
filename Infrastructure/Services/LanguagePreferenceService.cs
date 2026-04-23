using System.Globalization;
using System.IO;
using System.Text.Json;
using POS_system_cs.UI.Wpf.Localization;

namespace POS_system_cs.Infrastructure.Services;

internal sealed class LanguagePreferenceService
{
    private const string SettingsFileName = "user-settings.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _settingsPath;

    public LanguagePreferenceService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _settingsPath = Path.Combine(appData, "POS-system-cs", SettingsFileName);
    }

    public AppLanguage LoadLanguageOrSystemDefault()
    {
        var savedLanguage = LoadSavedLanguage();
        return savedLanguage ?? DetectSystemLanguage();
    }

    public void SaveLanguage(AppLanguage language)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var preferences = new UserPreferences { Language = language.ToString() };
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(preferences, JsonOptions));
        }
        catch
        {
            // Language persistence should never block the POS workflow.
        }
    }

    private AppLanguage? LoadSavedLanguage()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return null;
            }

            var preferences = JsonSerializer.Deserialize<UserPreferences>(File.ReadAllText(_settingsPath));
            return Enum.TryParse<AppLanguage>(preferences?.Language, ignoreCase: true, out var language)
                ? language
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static AppLanguage DetectSystemLanguage()
    {
        var culture = CultureInfo.CurrentUICulture;
        return culture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.Chinese
            : AppLanguage.English;
    }

    private sealed class UserPreferences
    {
        public string? Language { get; set; }
    }
}
