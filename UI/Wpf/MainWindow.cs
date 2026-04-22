using POS_system_cs.Application.Navigation;
using POS_system_cs.Application.Services;
using POS_system_cs.UI.Wpf.Controls;
using POS_system_cs.UI.Wpf.Localization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using MediaFontFamily = System.Windows.Media.FontFamily;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;

namespace POS_system_cs.UI.Wpf;

public sealed class MainWindow : Window
{
    private readonly IReadOnlyList<ModuleDefinition> _modules;
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;
    private readonly IInventoryService _inventoryService;
    private readonly ICashierService _cashierService;
    private readonly IOrderService _orderService;
    private readonly IReportService _reportService;
    private readonly ContentControl _contentHost = new();
    private readonly Dictionary<string, WpfButton> _navigationButtons = [];
    private readonly TextBlock _subtitleText = new();
    private ModuleDefinition? _currentModule;

    public MainWindow(
        IReadOnlyList<ModuleDefinition> modules,
        ICategoryService categoryService,
        IProductService productService,
        IInventoryService inventoryService,
        ICashierService cashierService,
        IOrderService orderService,
        IReportService reportService)
    {
        _modules = modules;
        _categoryService = categoryService;
        _productService = productService;
        _inventoryService = inventoryService;
        _cashierService = cashierService;
        _orderService = orderService;
        _reportService = reportService;

        Title = Localizer.T("App.Title");
        Width = 1320;
        Height = 840;
        MinWidth = 1180;
        MinHeight = 760;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(MediaColor.FromRgb(242, 245, 249));
        FontFamily = new MediaFontFamily("Microsoft YaHei UI");

        Content = BuildLayout();
        ShowModule(_modules[0]);
    }

    private Grid BuildLayout()
    {
        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(236) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var navigation = BuildNavigation();
        Grid.SetColumn(navigation, 0);
        root.Children.Add(navigation);

        var contentShell = new Border
        {
            Margin = new Thickness(18),
            Padding = new Thickness(0),
            Background = MediaBrushes.White,
            CornerRadius = new CornerRadius(18),
            BorderBrush = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240)),
            BorderThickness = new Thickness(1),
            Child = _contentHost
        };
        Grid.SetColumn(contentShell, 1);
        root.Children.Add(contentShell);

        return root;
    }

    private Border BuildNavigation()
    {
        var panel = new StackPanel { Margin = new Thickness(16, 20, 16, 20) };

        panel.Children.Add(new TextBlock
        {
            Text = "POS System",
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = MediaBrushes.White,
            Margin = new Thickness(6, 0, 6, 4)
        });

        _subtitleText.Text = Localizer.T("App.Subtitle");
        _subtitleText.FontSize = 12;
        _subtitleText.Foreground = new SolidColorBrush(MediaColor.FromRgb(203, 213, 225));
        _subtitleText.Margin = new Thickness(6, 0, 6, 24);
        panel.Children.Add(_subtitleText);

        panel.Children.Add(BuildLanguageSelector());

        foreach (var module in _modules)
        {
            var button = CreateNavigationButton(module);
            _navigationButtons[module.Key] = button;
            panel.Children.Add(button);
        }

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = panel
        };

        return new Border
        {
            Background = new LinearGradientBrush(
                MediaColor.FromRgb(15, 23, 42),
                MediaColor.FromRgb(30, 41, 59),
                90),
            Child = scrollViewer
        };
    }


    private WpfComboBox BuildLanguageSelector()
    {
        var selector = new WpfComboBox
        {
            Margin = new Thickness(0, 0, 0, 18),
            Height = 34,
            SelectedValuePath = "Tag",
            DisplayMemberPath = "Content"
        };
        selector.Items.Add(new WpfComboBoxItem { Content = Localizer.T("Chinese"), Tag = AppLanguage.Chinese });
        selector.Items.Add(new WpfComboBoxItem { Content = Localizer.T("English"), Tag = AppLanguage.English });
        selector.SelectedValue = Localizer.Current;
        selector.SelectionChanged += (_, _) =>
        {
            if (selector.SelectedValue is not AppLanguage language || language == Localizer.Current)
            {
                return;
            }

            Localizer.SetLanguage(language);
            Title = Localizer.T("App.Title");
            _subtitleText.Text = Localizer.T("App.Subtitle");
            foreach (var module in _modules)
            {
                if (_navigationButtons.TryGetValue(module.Key, out var button))
                {
                    button.Content = Localizer.ModuleTitle(module.Key);
                }
            }

            if (_currentModule is not null)
            {
                ShowModule(_currentModule);
            }
        };
        return selector;
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
            Foreground = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240)),
            Background = new SolidColorBrush(MediaColor.FromArgb(40, 255, 255, 255)),
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        button.Click += (_, _) => ShowModule(module);
        return button;
    }

    private void ShowModule(ModuleDefinition module)
    {
        _currentModule = module;
        foreach (var button in _navigationButtons.Values)
        {
            button.Background = new SolidColorBrush(MediaColor.FromArgb(40, 255, 255, 255));
            button.Foreground = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240));
        }

        if (_navigationButtons.TryGetValue(module.Key, out var activeButton))
        {
            activeButton.Background = new SolidColorBrush(MediaColor.FromRgb(37, 99, 235));
            activeButton.Foreground = MediaBrushes.White;
        }

        _contentHost.Content = CreateModuleContent(module);
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




