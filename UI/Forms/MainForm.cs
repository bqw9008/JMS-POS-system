using POS_system_cs.Application.Navigation;
using POS_system_cs.Application.Services;
using POS_system_cs.UI.Controls;

namespace POS_system_cs.UI.Forms;

public sealed class MainForm : Form
{
    private readonly IReadOnlyList<ModuleDefinition> _modules;
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;
    private readonly IInventoryService _inventoryService;
    private readonly ICashierService _cashierService;
    private readonly IOrderService _orderService;
    private readonly IReportService _reportService;
    private readonly Panel _contentPanel = new();

    public MainForm(
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

        Text = "小型商超 POS 系统";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1180, 760);
        Size = new Size(1280, 800);
        Font = new Font("Microsoft YaHei UI", 9F);

        BuildLayout();
        ShowModule(_modules[0]);
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var navigation = BuildNavigation();

        _contentPanel.Dock = DockStyle.Fill;
        _contentPanel.BackColor = Color.FromArgb(247, 249, 252);
        _contentPanel.Padding = new Padding(18);

        root.Controls.Add(navigation, 0, 0);
        root.Controls.Add(_contentPanel, 1, 0);

        Controls.Add(root);
    }

    private Control BuildNavigation()
    {
        var navigation = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(17, 24, 39),
            Padding = new Padding(14, 18, 14, 18)
        };

        var title = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 72,
            Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold),
            ForeColor = Color.White,
            Text = "POS 系统"
        };

        var menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoScroll = true,
            WrapContents = false
        };

        foreach (var module in _modules)
        {
            menu.Controls.Add(CreateNavigationButton(module));
        }

        navigation.Controls.Add(menu);
        navigation.Controls.Add(title);
        return navigation;
    }

    private Button CreateNavigationButton(ModuleDefinition module)
    {
        var button = new Button
        {
            Width = 190,
            Height = 42,
            BackColor = Color.FromArgb(31, 41, 55),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei UI", 10F),
            ForeColor = Color.FromArgb(229, 231, 235),
            Margin = new Padding(0, 0, 0, 8),
            Text = module.Title,
            TextAlign = ContentAlignment.MiddleLeft,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => ShowModule(module);
        return button;
    }

    private void ShowModule(ModuleDefinition module)
    {
        _contentPanel.Controls.Clear();
        _contentPanel.Controls.Add(CreateModuleControl(module));
    }

    private Control CreateModuleControl(ModuleDefinition module)
    {
        return module.Key switch
        {
            "cashier" => new CashierControl(_productService, _cashierService),
            "categories" => new CategoryManagementControl(_categoryService),
            "products" => new ProductManagementControl(_productService, _categoryService),
            "inventory" => new InventoryManagementControl(_inventoryService, _productService),
            "orders" => new SalesRecordControl(_orderService),
            "reports" => new ReportsControl(_reportService),
            _ => new ModulePlaceholderControl(module)
        };
    }
}
