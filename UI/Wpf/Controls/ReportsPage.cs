using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using POS_system_cs.Application.Models;
using POS_system_cs.Application.Services;
using POS_system_cs.UI.Wpf.Localization;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfDatePicker = System.Windows.Controls.DatePicker;
using WpfTabControl = System.Windows.Controls.TabControl;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed class ReportsPage : WpfUserControl
{
    private readonly IReportService _service;
    private readonly WpfDatePicker _from = new() { SelectedDate = DateTime.Today.AddDays(-29), Width = 140 };
    private readonly WpfDatePicker _to = new() { SelectedDate = DateTime.Today, Width = 140 };
    private readonly TextBlock _sales = WpfUi.Metric();
    private readonly TextBlock _orders = WpfUi.Metric();
    private readonly TextBlock _quantity = WpfUi.Metric();
    private readonly TextBlock _stock = WpfUi.Metric();
    private readonly TextBlock _low = WpfUi.Metric();
    private readonly ObservableCollection<SalesSummaryPoint> _daily = [];
    private readonly ObservableCollection<SalesSummaryPoint> _weekly = [];
    private readonly ObservableCollection<SalesSummaryPoint> _monthly = [];
    private readonly ObservableCollection<ProductSalesRanking> _top = [];
    private readonly ObservableCollection<ProductSalesRanking> _slow = [];

    public ReportsPage(IReportService service)
    {
        _service = service;
        Content = Build();
        Loaded += async (_, _) => await LoadAsync();
    }

    private Grid Build()
    {
        var root = new Grid { Margin = new Thickness(22) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var toolbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 14), VerticalAlignment = VerticalAlignment.Center };
        toolbar.Children.Add(WpfUi.Header(Localizer.T("Reports.Title"), Localizer.T("Reports.Desc")));
        toolbar.Children.Add(WpfUi.SmallLabel(Localizer.T("Field.From")));
        toolbar.Children.Add(_from);
        toolbar.Children.Add(WpfUi.SmallLabel(Localizer.T("Field.To")));
        toolbar.Children.Add(_to);
        toolbar.Children.Add(WpfUi.Primary(Localizer.T("Action.Refresh"), async (_, _) => await LoadAsync(), compact: true));
        toolbar.Children.Add(WpfUi.Secondary(Localizer.T("Action.Last30"), async (_, _) => await RangeAsync(DateTime.Today.AddDays(-29), DateTime.Today), compact: true));
        toolbar.Children.Add(WpfUi.Secondary(Localizer.T("Action.ThisMonth"), async (_, _) => await RangeAsync(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today), compact: true));
        root.Children.Add(toolbar);

        var metrics = new System.Windows.Controls.Primitives.UniformGrid { Columns = 5, Margin = new Thickness(0, 0, 0, 14) };
        metrics.Children.Add(WpfUi.MetricCard(Localizer.T("Metric.TodaySales"), _sales));
        metrics.Children.Add(WpfUi.MetricCard(Localizer.T("Metric.TodayOrders"), _orders));
        metrics.Children.Add(WpfUi.MetricCard(Localizer.T("Metric.TodayQuantity"), _quantity));
        metrics.Children.Add(WpfUi.MetricCard(Localizer.T("Metric.StockQuantity"), _stock));
        metrics.Children.Add(WpfUi.MetricCard(Localizer.T("Metric.LowStock"), _low));
        Grid.SetRow(metrics, 1);
        root.Children.Add(metrics);

        var salesTabs = new WpfTabControl();
        salesTabs.Items.Add(WpfUi.Tab(Localizer.T("Tab.Daily"), SalesGrid(_daily)));
        salesTabs.Items.Add(WpfUi.Tab(Localizer.T("Tab.Weekly"), SalesGrid(_weekly)));
        salesTabs.Items.Add(WpfUi.Tab(Localizer.T("Tab.Monthly"), SalesGrid(_monthly)));
        var salesCard = WpfUi.Card(salesTabs, new Thickness(0, 0, 0, 14));
        Grid.SetRow(salesCard, 2);
        root.Children.Add(salesCard);

        var productTabs = new WpfTabControl();
        productTabs.Items.Add(WpfUi.Tab(Localizer.T("Tab.TopSelling"), RankingGrid(_top)));
        productTabs.Items.Add(WpfUi.Tab(Localizer.T("Tab.SlowSelling"), RankingGrid(_slow)));
        var productCard = WpfUi.Card(productTabs, new Thickness(0));
        Grid.SetRow(productCard, 3);
        root.Children.Add(productCard);
        return root;
    }

    private async Task LoadAsync()
    {
        try
        {
            var from = DateOnly.FromDateTime((_from.SelectedDate ?? DateTime.Today.AddDays(-29)).Date);
            var to = DateOnly.FromDateTime((_to.SelectedDate ?? DateTime.Today).Date);
            var report = await _service.GetDashboardAsync(from, to);
            _sales.Text = report.TodaySalesAmount.ToString("N2");
            _orders.Text = report.TodayOrderCount.ToString("N0");
            _quantity.Text = report.TodayProductQuantity.ToString("N2");
            _stock.Text = report.TotalStockQuantity.ToString("N2");
            _low.Text = report.LowStockProductCount.ToString("N0");
            _daily.ReplaceWith(report.DailySales);
            _weekly.ReplaceWith(report.WeeklySales);
            _monthly.ReplaceWith(report.MonthlySales);
            _top.ReplaceWith(report.TopSellingProducts);
            _slow.ReplaceWith(report.SlowSellingProducts);
        }
        catch (Exception ex) { WpfUi.Error(this, ex); }
    }

    private async Task RangeAsync(DateTime from, DateTime to)
    {
        _from.SelectedDate = from;
        _to.SelectedDate = to;
        await LoadAsync();
    }

    private static WpfDataGrid SalesGrid(IEnumerable<SalesSummaryPoint> source)
    {
        var grid = WpfUi.Grid();
        grid.ItemsSource = source;
        grid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Period"), nameof(SalesSummaryPoint.Label), star: true));
        grid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Sales"), nameof(SalesSummaryPoint.SalesAmount), 130));
        grid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Orders"), nameof(SalesSummaryPoint.OrderCount), 110));
        return grid;
    }

    private static WpfDataGrid RankingGrid(IEnumerable<ProductSalesRanking> source)
    {
        var grid = WpfUi.Grid();
        grid.ItemsSource = source;
        grid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Product"), nameof(ProductSalesRanking.ProductName), star: true));
        grid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Barcode"), nameof(ProductSalesRanking.Barcode), 170));
        grid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Qty"), nameof(ProductSalesRanking.Quantity), 110));
        grid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Sales"), nameof(ProductSalesRanking.SalesAmount), 130));
        return grid;
    }
}
