using POS_system_cs.Application.Navigation;
using POS_system_cs.Application.Services;
using POS_system_cs.UI.Wpf.Controls;
using POS_system_cs.UI.Wpf.Localization;
using System.Windows;
using System.Windows.Controls;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;

namespace POS_system_cs.UI.Wpf;

public sealed partial class MainWindow : Window
{
    private readonly IReadOnlyList<ModuleDefinition> _modules;
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;
    private readonly IInventoryService _inventoryService;
    private readonly ICashierService _cashierService;
    private readonly IOrderService _orderService;
    private readonly IReportService _reportService;
    private readonly Action<AppLanguage> _saveLanguage;
    private readonly Dictionary<string, WpfButton> _navigationButtons = [];
    private ModuleDefinition? _currentModule;

    public MainWindow(
        IReadOnlyList<ModuleDefinition> modules,
        ICategoryService categoryService,
        IProductService productService,
        IInventoryService inventoryService,
        ICashierService cashierService,
        IOrderService orderService,
        IReportService reportService,
        Action<AppLanguage> saveLanguage)
    {
        _modules = modules;
        _categoryService = categoryService;
        _productService = productService;
        _inventoryService = inventoryService;
        _cashierService = cashierService;
        _orderService = orderService;
        _reportService = reportService;
        _saveLanguage = saveLanguage;

        InitializeComponent();
        ApplyLocalization();
        BuildLanguageSelector();
        BuildNavigationButtons();
        ShowModule(_modules[0]);
    }

    private void ApplyLocalization()
    {
        Title = Localizer.T("App.Title");
        SubtitleText.Text = Localizer.T("App.Subtitle");
    }

    private void BuildLanguageSelector()
    {
        LanguageSelector.Items.Clear();
        LanguageSelector.Items.Add(new WpfComboBoxItem { Content = Localizer.T("Chinese"), Tag = AppLanguage.Chinese });
        LanguageSelector.Items.Add(new WpfComboBoxItem { Content = Localizer.T("English"), Tag = AppLanguage.English });
        LanguageSelector.SelectedValue = Localizer.Current;
    }

    private void BuildNavigationButtons()
    {
        NavigationItemsPanel.Children.Clear();
        _navigationButtons.Clear();

        foreach (var module in _modules)
        {
            var button = CreateNavigationButton(module);
            _navigationButtons[module.Key] = button;
            NavigationItemsPanel.Children.Add(button);
        }
    }

    private WpfButton CreateNavigationButton(ModuleDefinition module)
    {
        var button = new WpfButton
        {
            Content = Localizer.ModuleTitle(module.Key),
            Tag = module.Key,
            Height = 44,
            HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(16, 0, 12, 0),
            FontSize = 14,
            Foreground = WpfUi.Brush(WpfUi.TextColor),
            Background = WpfUi.Brush(WpfUi.NavButtonBackground),
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        button.Click += (_, _) => ShowModule(module);
        return button;
    }

    private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageSelector.SelectedValue is not AppLanguage language || language == Localizer.Current)
        {
            return;
        }

        Localizer.SetLanguage(language);
        _saveLanguage(language);
        ApplyLocalization();
        BuildLanguageSelector();
        BuildNavigationButtons();

        if (_currentModule is not null)
        {
            ShowModule(_currentModule);
        }
    }

    private void ShowModule(ModuleDefinition module)
    {
        _currentModule = module;
        foreach (var button in _navigationButtons.Values)
        {
            button.Background = WpfUi.Brush(WpfUi.NavButtonBackground);
            button.Foreground = WpfUi.Brush(WpfUi.TextColor);
        }

        if (_navigationButtons.TryGetValue(module.Key, out var activeButton))
        {
            activeButton.Background = WpfUi.Brush(WpfUi.PrimaryColor);
            activeButton.Foreground = WpfUi.Brush(WpfUi.CardBackground);
        }

        ContentHost.Content = CreateModuleContent(module);
    }

    private object CreateModuleContent(ModuleDefinition module)
    {
        return module.Key switch
        {
            "cashier" => new CashierPage(_productService, _cashierService),
            "categories" => new CategoryManagementPage(_categoryService),
            "products" => new ProductManagementPage(_productService, _categoryService),
            "inventory" => new InventoryManagementPage(_inventoryService, _productService),
            "orders" => new SalesRecordPage(_orderService),
            "reports" => new ReportsPage(_reportService),
            _ => new ModulePlaceholderPage(module)
        };
    }
}
