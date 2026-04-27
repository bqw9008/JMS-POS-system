using System.IO;
using POS_system_cs.Configuration;
using POS_system_cs.UI.Wpf.Localization;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed partial class SettingsPage : WpfUserControl
{
    private readonly AppSettings _settings;
    private readonly Func<AppSettings, Task> _saveSettingsAsync;
    private readonly string _settingsPath;
    private readonly string _languageSettingsPath;
    private readonly string _logDirectory;
    private bool _saveInProgress;

    public SettingsPage(
        AppSettings settings,
        Func<AppSettings, Task> saveSettingsAsync,
        string settingsPath,
        string languageSettingsPath,
        string logDirectory)
    {
        _settings = settings;
        _saveSettingsAsync = saveSettingsAsync;
        _settingsPath = settingsPath;
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
        EditNoticeText.Text = Localizer.T("Settings.EditNotice");
        StoreNameLabel.Text = Localizer.T("Settings.StoreName");
        DatabasePathLabel.Text = Localizer.T("Settings.DatabasePath");
        ReceiptPrinterLabel.Text = Localizer.T("Settings.ReceiptPrinter");
        CurrentLanguageLabel.Text = Localizer.T("Settings.CurrentLanguage");
        SettingsFilePathLabel.Text = Localizer.T("Settings.SettingsFile");
        LanguageSettingsPathLabel.Text = Localizer.T("Settings.LanguageSettingsPath");
        LogDirectoryLabel.Text = Localizer.T("Settings.LogDirectory");
        RuntimeDirectoryLabel.Text = Localizer.T("Settings.RuntimeDirectory");
        ResetButton.Content = Localizer.T("Action.Refresh");
        SaveButton.Content = Localizer.T("Action.Save");
    }

    private void LoadSettings()
    {
        StoreNameBox.Text = _settings.StoreName;
        DatabasePathBox.Text = _settings.DatabasePath;
        ReceiptPrinterBox.Text = _settings.ReceiptPrinterName;
        CurrentLanguageBox.Text = Localizer.LanguageName(Localizer.Current);
        SettingsFilePathBox.Text = _settingsPath;
        LanguageSettingsPathBox.Text = _languageSettingsPath;
        LogDirectoryBox.Text = _logDirectory;
        RuntimeDirectoryBox.Text = AppContext.BaseDirectory;
    }

    private async void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_saveInProgress)
        {
            return;
        }

        var updatedSettings = BuildSettingsFromForm();
        if (updatedSettings is null)
        {
            return;
        }

        try
        {
            SetBusyState(true);
            await _saveSettingsAsync(updatedSettings);
            LoadSettings();
            WpfUi.Info(this, Localizer.T("Settings.Saved"));
        }
        catch (Exception ex)
        {
            WpfUi.Error(this, ex);
            LoadSettings();
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private void ResetButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_saveInProgress)
        {
            return;
        }

        LoadSettings();
    }

    private AppSettings? BuildSettingsFromForm()
    {
        var storeName = StoreNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(storeName))
        {
            WpfUi.Info(this, Localizer.T("Settings.StoreNameRequired"));
            StoreNameBox.Focus();
            StoreNameBox.SelectAll();
            return null;
        }

        var databasePath = DatabasePathBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            WpfUi.Info(this, Localizer.T("Settings.DatabasePathRequired"));
            DatabasePathBox.Focus();
            DatabasePathBox.SelectAll();
            return null;
        }

        if (!TryValidateDatabasePath(databasePath, out var validationMessage))
        {
            WpfUi.Info(this, validationMessage);
            DatabasePathBox.Focus();
            DatabasePathBox.SelectAll();
            return null;
        }

        return new AppSettings
        {
            StoreName = storeName,
            DatabasePath = databasePath,
            ReceiptPrinterName = ReceiptPrinterBox.Text.Trim()
        };
    }

    private void SetBusyState(bool isBusy)
    {
        _saveInProgress = isBusy;
        SaveButton.IsEnabled = !isBusy;
        ResetButton.IsEnabled = !isBusy;
    }

    private static bool TryValidateDatabasePath(string databasePath, out string validationMessage)
    {
        try
        {
            var resolvedPath = Path.IsPathRooted(databasePath)
                ? databasePath
                : Path.Combine(AppContext.BaseDirectory, databasePath);
            var fullPath = Path.GetFullPath(resolvedPath);

            if (string.IsNullOrWhiteSpace(Path.GetFileName(fullPath)))
            {
                validationMessage = Localizer.T("Settings.DatabasePathFileRequired");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Path.GetDirectoryName(fullPath)))
            {
                validationMessage = Localizer.T("Settings.DatabasePathInvalid");
                return false;
            }

            validationMessage = string.Empty;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            validationMessage = Localizer.T("Settings.DatabasePathInvalid");
            return false;
        }
    }
}
