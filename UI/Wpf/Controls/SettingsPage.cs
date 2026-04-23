using System.IO;
using POS_system_cs.Configuration;
using POS_system_cs.UI.Wpf.Localization;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed partial class SettingsPage : WpfUserControl
{
    private readonly AppSettings _settings;
    private readonly string _languageSettingsPath;
    private readonly string _logDirectory;

    public SettingsPage(AppSettings settings, string languageSettingsPath, string logDirectory)
    {
        _settings = settings;
        _languageSettingsPath = languageSettingsPath;
        _logDirectory = logDirectory;

        InitializeComponent();
        ApplyLocalization();
        LoadSettings();
    }

    private void ApplyLocalization()
    {
        TitleText.Text = Localizer.T("Settings.Title");
        DescriptionText.Text = Localizer.T("Settings.Desc");
        ReadOnlyNoticeText.Text = Localizer.T("Settings.ReadOnlyNotice");
        StoreNameLabel.Text = Localizer.T("Settings.StoreName");
        DatabasePathLabel.Text = Localizer.T("Settings.DatabasePath");
        ReceiptPrinterLabel.Text = Localizer.T("Settings.ReceiptPrinter");
        CurrentLanguageLabel.Text = Localizer.T("Settings.CurrentLanguage");
        LanguageSettingsPathLabel.Text = Localizer.T("Settings.LanguageSettingsPath");
        LogDirectoryLabel.Text = Localizer.T("Settings.LogDirectory");
        RuntimeDirectoryLabel.Text = Localizer.T("Settings.RuntimeDirectory");
    }

    private void LoadSettings()
    {
        StoreNameBox.Text = _settings.StoreName;
        DatabasePathBox.Text = ResolveDatabasePath(_settings.DatabasePath);
        ReceiptPrinterBox.Text = string.IsNullOrWhiteSpace(_settings.ReceiptPrinterName) ? "-" : _settings.ReceiptPrinterName;
        CurrentLanguageBox.Text = Localizer.Current.ToString();
        LanguageSettingsPathBox.Text = _languageSettingsPath;
        LogDirectoryBox.Text = _logDirectory;
        RuntimeDirectoryBox.Text = AppContext.BaseDirectory;
    }

    private static string ResolveDatabasePath(string databasePath)
    {
        return Path.IsPathRooted(databasePath)
            ? databasePath
            : Path.Combine(AppContext.BaseDirectory, databasePath);
    }
}
