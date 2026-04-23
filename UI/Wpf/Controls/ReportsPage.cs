using System.Collections.ObjectModel;
using System.Windows.Controls;
using POS_system_cs.Application.Models;
using POS_system_cs.Application.Services;
using POS_system_cs.UI.Wpf.Localization;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed partial class ReportsPage : WpfUserControl
{
    private readonly IReportService _service;
    private readonly ObservableCollection<SalesSummaryPoint> _daily = [];
    private readonly ObservableCollection<SalesSummaryPoint> _weekly = [];
    private readonly ObservableCollection<SalesSummaryPoint> _monthly = [];
    private readonly ObservableCollection<ProductSalesRanking> _top = [];
    private readonly ObservableCollection<ProductSalesRanking> _slow = [];

    public ReportsPage(IReportService service)
    {
        _service = service;
        InitializeComponent();
        ApplyLocalization();
        BuildGrids();
        FromDatePicker.SelectedDate = DateTime.Today.AddDays(-29);
        ToDatePicker.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await LoadAsync();
    }

    private void ApplyLocalization()
    {
        TitleText.Text = Localizer.T("Reports.Title");
        DescriptionText.Text = Localizer.T("Reports.Desc");
        FromLabel.Text = Localizer.T("Field.From");
        ToLabel.Text = Localizer.T("Field.To");
        RefreshButton.Content = Localizer.T("Action.Refresh");
        Last30Button.Content = Localizer.T("Action.Last30");
        ThisMonthButton.Content = Localizer.T("Action.ThisMonth");
        TodaySalesLabel.Text = Localizer.T("Metric.TodaySales");
        TodayOrdersLabel.Text = Localizer.T("Metric.TodayOrders");
        TodayQuantityLabel.Text = Localizer.T("Metric.TodayQuantity");
        StockQuantityLabel.Text = Localizer.T("Metric.StockQuantity");
        LowStockLabel.Text = Localizer.T("Metric.LowStock");
        DailyTab.Header = Localizer.T("Tab.Daily");
        WeeklyTab.Header = Localizer.T("Tab.Weekly");
        MonthlyTab.Header = Localizer.T("Tab.Monthly");
        TopSellingTab.Header = Localizer.T("Tab.TopSelling");
        SlowSellingTab.Header = Localizer.T("Tab.SlowSelling");
    }

    private void BuildGrids()
    {
        ConfigureSalesGrid(DailyGrid, _daily);
        ConfigureSalesGrid(WeeklyGrid, _weekly);
        ConfigureSalesGrid(MonthlyGrid, _monthly);
        ConfigureRankingGrid(TopGrid, _top);
        ConfigureRankingGrid(SlowGrid, _slow);
    }

    private async Task LoadAsync()
    {
        try
        {
            var from = DateOnly.FromDateTime((FromDatePicker.SelectedDate ?? DateTime.Today.AddDays(-29)).Date);
            var to = DateOnly.FromDateTime((ToDatePicker.SelectedDate ?? DateTime.Today).Date);
            var report = await _service.GetDashboardAsync(from, to);
            SalesText.Text = report.TodaySalesAmount.ToString("N2");
            OrdersText.Text = report.TodayOrderCount.ToString("N0");
            QuantityText.Text = report.TodayProductQuantity.ToString("N2");
            StockText.Text = report.TotalStockQuantity.ToString("N2");
            LowText.Text = report.LowStockProductCount.ToString("N0");
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
        FromDatePicker.SelectedDate = from;
        ToDatePicker.SelectedDate = to;
        await LoadAsync();
    }

    private static void ConfigureSalesGrid(WpfDataGrid grid, IEnumerable<SalesSummaryPoint> source)
    {
        grid.ItemsSource = source;
        grid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Period"), nameof(SalesSummaryPoint.Label), star: true));
        grid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Sales"), nameof(SalesSummaryPoint.SalesAmount), 130));
        grid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Orders"), nameof(SalesSummaryPoint.OrderCount), 110));
    }

    private static void ConfigureRankingGrid(WpfDataGrid grid, IEnumerable<ProductSalesRanking> source)
    {
        grid.ItemsSource = source;
        grid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Product"), nameof(ProductSalesRanking.ProductName), star: true));
        grid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Barcode"), nameof(ProductSalesRanking.Barcode), 170));
        grid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Qty"), nameof(ProductSalesRanking.Quantity), 110));
        grid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Sales"), nameof(ProductSalesRanking.SalesAmount), 130));
    }

    private async void RefreshButton_Click(object sender, System.Windows.RoutedEventArgs e) => await LoadAsync();

    private async void Last30Button_Click(object sender, System.Windows.RoutedEventArgs e) => await RangeAsync(DateTime.Today.AddDays(-29), DateTime.Today);

    private async void ThisMonthButton_Click(object sender, System.Windows.RoutedEventArgs e) => await RangeAsync(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today);
}
