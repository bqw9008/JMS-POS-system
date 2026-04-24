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
        EditNoticeText.Text = CurrentText(
            "Settings on this page can be edited and saved immediately. If the database path changes, the target database will be initialized automatically.",
            "此页面的设置可以直接编辑并保存。若数据库路径发生变化，目标数据库会自动初始化。");
        StoreNameLabel.Text = Localizer.T("Settings.StoreName");
        DatabasePathLabel.Text = Localizer.T("Settings.DatabasePath");
        ReceiptPrinterLabel.Text = Localizer.T("Settings.ReceiptPrinter");
        CurrentLanguageLabel.Text = Localizer.T("Settings.CurrentLanguage");
        SettingsFilePathLabel.Text = CurrentText("Settings file", "设置文件");
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
            WpfUi.Info(this, CurrentText("Settings saved.", "设置已保存。"));
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
            WpfUi.Info(this, CurrentText("Store name is required.", "店铺名称不能为空。"));
            StoreNameBox.Focus();
            StoreNameBox.SelectAll();
            return null;
        }

        var databasePath = DatabasePathBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            WpfUi.Info(this, CurrentText("Database path is required.", "数据库路径不能为空。"));
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

    private static string CurrentText(string english, string chinese)
    {
        return Localizer.Current == AppLanguage.Chinese ? chinese : english;
    }
}
