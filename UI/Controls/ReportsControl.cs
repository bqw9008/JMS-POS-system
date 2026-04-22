using POS_system_cs.Application.Models;
using POS_system_cs.Application.Services;

namespace POS_system_cs.UI.Controls;

public sealed class ReportsControl : UserControl
{
    private readonly IReportService _reportService;
    private readonly DateTimePicker _fromPicker = new();
    private readonly DateTimePicker _toPicker = new();
    private readonly Label _todaySalesLabel = CreateMetricValueLabel();
    private readonly Label _todayOrdersLabel = CreateMetricValueLabel();
    private readonly Label _todayQuantityLabel = CreateMetricValueLabel();
    private readonly Label _stockQuantityLabel = CreateMetricValueLabel();
    private readonly Label _lowStockLabel = CreateMetricValueLabel();
    private readonly DataGridView _dailyGrid = new();
    private readonly DataGridView _weeklyGrid = new();
    private readonly DataGridView _monthlyGrid = new();
    private readonly DataGridView _topProductsGrid = new();
    private readonly DataGridView _slowProductsGrid = new();

    public ReportsControl(IReportService reportService)
    {
        _reportService = reportService;
        Dock = DockStyle.Fill;
        BuildLayout();
        _ = LoadReportsAsync();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            BackColor = Color.FromArgb(247, 249, 252)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));

        root.Controls.Add(BuildToolbar(), 0, 0);
        root.Controls.Add(BuildMetrics(), 0, 1);
        root.Controls.Add(BuildSalesTabs(), 0, 2);
        root.Controls.Add(BuildRankingTabs(), 0, 3);
        Controls.Add(root);
    }

    private Control BuildToolbar()
    {
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 12),
            WrapContents = false
        };

        _fromPicker.Format = DateTimePickerFormat.Short;
        _fromPicker.Value = DateTime.Today.AddDays(-29);
        _toPicker.Format = DateTimePickerFormat.Short;
        _toPicker.Value = DateTime.Today;

        toolbar.Controls.Add(CreateToolbarLabel("开始"));
        toolbar.Controls.Add(_fromPicker);
        toolbar.Controls.Add(CreateToolbarLabel("结束"));
        toolbar.Controls.Add(_toPicker);
        toolbar.Controls.Add(CreatePrimaryButton("刷新报表", async (_, _) => await LoadReportsAsync()));
        toolbar.Controls.Add(CreateSecondaryButton("近 30 天", async (_, _) => await SetRangeAsync(DateTime.Today.AddDays(-29), DateTime.Today)));
        toolbar.Controls.Add(CreateSecondaryButton("本月", async (_, _) => await SetRangeAsync(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today)));
        return toolbar;
    }

    private Control BuildMetrics()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 5,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 14)
        };

        for (var i = 0; i < 5; i++)
        {
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        }

        panel.Controls.Add(CreateMetricCard("今日销售额", _todaySalesLabel), 0, 0);
        panel.Controls.Add(CreateMetricCard("今日订单数", _todayOrdersLabel), 1, 0);
        panel.Controls.Add(CreateMetricCard("今日销售数量", _todayQuantityLabel), 2, 0);
        panel.Controls.Add(CreateMetricCard("库存总量", _stockQuantityLabel), 3, 0);
        panel.Controls.Add(CreateMetricCard("低库存商品", _lowStockLabel), 4, 0);
        return panel;
    }

    private Control BuildSalesTabs()
    {
        ConfigureSalesGrid(_dailyGrid);
        ConfigureSalesGrid(_weeklyGrid);
        ConfigureSalesGrid(_monthlyGrid);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CreateTabPage("按日销售", _dailyGrid));
        tabs.TabPages.Add(CreateTabPage("按周销售", _weeklyGrid));
        tabs.TabPages.Add(CreateTabPage("按月销售", _monthlyGrid));
        return tabs;
    }

    private Control BuildRankingTabs()
    {
        ConfigureProductRankingGrid(_topProductsGrid);
        ConfigureProductRankingGrid(_slowProductsGrid);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CreateTabPage("热销商品", _topProductsGrid));
        tabs.TabPages.Add(CreateTabPage("滞销商品", _slowProductsGrid));
        return tabs;
    }

    private async Task LoadReportsAsync()
    {
        try
        {
            var from = DateOnly.FromDateTime(_fromPicker.Value.Date);
            var to = DateOnly.FromDateTime(_toPicker.Value.Date);
            var dashboard = await _reportService.GetDashboardAsync(from, to);
            BindDashboard(dashboard);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task SetRangeAsync(DateTime from, DateTime to)
    {
        _fromPicker.Value = from;
        _toPicker.Value = to;
        await LoadReportsAsync();
    }

    private void BindDashboard(ReportDashboard dashboard)
    {
        _todaySalesLabel.Text = dashboard.TodaySalesAmount.ToString("N2");
        _todayOrdersLabel.Text = dashboard.TodayOrderCount.ToString("N0");
        _todayQuantityLabel.Text = dashboard.TodayProductQuantity.ToString("N2");
        _stockQuantityLabel.Text = dashboard.TotalStockQuantity.ToString("N2");
        _lowStockLabel.Text = dashboard.LowStockProductCount.ToString("N0");

        _dailyGrid.DataSource = dashboard.DailySales.ToList();
        _weeklyGrid.DataSource = dashboard.WeeklySales.ToList();
        _monthlyGrid.DataSource = dashboard.MonthlySales.ToList();
        _topProductsGrid.DataSource = dashboard.TopSellingProducts.ToList();
        _slowProductsGrid.DataSource = dashboard.SlowSellingProducts.ToList();
    }

    private static void ConfigureSalesGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.AutoGenerateColumns = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.RowHeadersVisible = false;
        grid.BackgroundColor = Color.White;
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "周期", DataPropertyName = nameof(SalesSummaryPoint.Label), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "销售额", DataPropertyName = nameof(SalesSummaryPoint.SalesAmount), Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "订单数", DataPropertyName = nameof(SalesSummaryPoint.OrderCount), Width = 100 });
    }

    private static void ConfigureProductRankingGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.AutoGenerateColumns = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.RowHeadersVisible = false;
        grid.BackgroundColor = Color.White;
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "商品", DataPropertyName = nameof(ProductSalesRanking.ProductName), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "条码", DataPropertyName = nameof(ProductSalesRanking.Barcode), Width = 160 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "数量", DataPropertyName = nameof(ProductSalesRanking.Quantity), Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "销售额", DataPropertyName = nameof(ProductSalesRanking.SalesAmount), Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
    }

    private static TabPage CreateTabPage(string title, Control content)
    {
        var page = new TabPage(title) { Padding = new Padding(8) };
        page.Controls.Add(content);
        return page;
    }

    private static Control CreateMetricCard(string title, Label valueLabel)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 88,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 10, 0),
            Padding = new Padding(14)
        };
        card.Controls.Add(valueLabel);
        card.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Dock = DockStyle.Top,
            ForeColor = Color.FromArgb(75, 85, 99),
            Font = new Font("Microsoft YaHei UI", 9.5F)
        });
        return card;
    }

    private static Label CreateMetricValueLabel()
    {
        return new Label
        {
            AutoSize = true,
            Dock = DockStyle.Bottom,
            Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39)
        };
    }

    private static Label CreateToolbarLabel(string text)
    {
        return new Label { Text = text, AutoSize = true, Margin = new Padding(0, 8, 8, 0) };
    }

    private static Button CreatePrimaryButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            BackColor = Color.FromArgb(37, 99, 235),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Margin = new Padding(10, 0, 0, 0),
            Padding = new Padding(14, 6, 14, 6),
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += onClick;
        return button;
    }

    private static Button CreateSecondaryButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            BackColor = Color.FromArgb(229, 231, 235),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(31, 41, 55),
            Margin = new Padding(8, 0, 0, 0),
            Padding = new Padding(14, 6, 14, 6),
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += onClick;
        return button;
    }

    private static void ShowError(Exception ex)
    {
        MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
