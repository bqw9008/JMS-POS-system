using System.Collections.ObjectModel;
using System.Windows.Controls;
using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;
using POS_system_cs.UI.Wpf.Localization;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed partial class SalesRecordPage : WpfUserControl
{
    private readonly IOrderService _service;
    private readonly ObservableCollection<Order> _orders = [];
    private readonly ObservableCollection<OrderItem> _items = [];

    public SalesRecordPage(IOrderService service)
    {
        _service = service;
        InitializeComponent();
        ApplyLocalization();
        BuildGrids();
        FromDatePicker.SelectedDate = DateTime.Today;
        ToDatePicker.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await LoadAsync();
    }

    private void ApplyLocalization()
    {
        TitleText.Text = Localizer.T("Orders.Title");
        DescriptionText.Text = Localizer.T("Orders.Desc");
        UseFromCheckBox.Content = Localizer.T("Field.From");
        UseToCheckBox.Content = Localizer.T("Field.To");
        SearchButton.Content = Localizer.T("Action.Search");
        TodayButton.Content = Localizer.T("Action.Today");
        AllButton.Content = Localizer.T("Action.All");
        OrdersSectionTitle.Text = Localizer.T("Section.Orders");
        ItemsSectionTitle.Text = Localizer.T("Section.Items");
    }

    private void BuildGrids()
    {
        OrderGrid.ItemsSource = _orders;
        OrderGrid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.OrderNo"), nameof(Order.OrderNo), 180));
        OrderGrid.Columns.Add(WpfUi.DateColumn(Localizer.T("Field.Time"), nameof(Order.OrderedAt), 170));
        OrderGrid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Total"), nameof(Order.TotalAmount), 90));
        OrderGrid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Discount"), nameof(Order.DiscountAmount), 90));
        OrderGrid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Received"), nameof(Order.ReceivedAmount), 100));
        OrderGrid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Change"), nameof(Order.ChangeAmount), 90));
        OrderGrid.Columns.Add(WpfUi.PaymentColumn(Localizer.T("Field.Payment"), nameof(Order.PaymentMethod), star: true));

        ItemGrid.ItemsSource = _items;
        ItemGrid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Product"), nameof(OrderItem.ProductName), star: true));
        ItemGrid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Barcode"), nameof(OrderItem.Barcode), 160));
        ItemGrid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Qty"), nameof(OrderItem.Quantity), 90));
        ItemGrid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Price"), nameof(OrderItem.UnitPrice), 90));
        ItemGrid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Amount"), nameof(OrderItem.Amount), 100));
    }

    private async Task LoadAsync()
    {
        try
        {
            var from = UseFromCheckBox.IsChecked == true && FromDatePicker.SelectedDate is DateTime fd ? DateOnly.FromDateTime(fd.Date) : (DateOnly?)null;
            var to = UseToCheckBox.IsChecked == true && ToDatePicker.SelectedDate is DateTime td ? DateOnly.FromDateTime(td.Date) : (DateOnly?)null;
            if (from is not null && to is not null && from > to)
            {
                WpfUi.Info(this, Localizer.T("Orders.FromDateError"));
                return;
            }

            _orders.ReplaceWith(await _service.SearchAsync(from, to));
            _items.Clear();
            Summary();
        }
        catch (Exception ex) { WpfUi.Error(this, ex); }
    }

    private async Task LoadItemsAsync()
    {
        if (OrderGrid.SelectedItem is not Order order) return;
        try { _items.ReplaceWith(await _service.GetItemsAsync(order.Id)); }
        catch (Exception ex) { WpfUi.Error(this, ex); }
    }

    private async Task TodayAsync()
    {
        UseFromCheckBox.IsChecked = true;
        UseToCheckBox.IsChecked = true;
        FromDatePicker.SelectedDate = DateTime.Today;
        ToDatePicker.SelectedDate = DateTime.Today;
        await LoadAsync();
    }

    private async Task AllAsync()
    {
        UseFromCheckBox.IsChecked = false;
        UseToCheckBox.IsChecked = false;
        await LoadAsync();
    }

    private void Summary()
    {
        var total = _orders.Sum(x => x.TotalAmount);
        var discount = _orders.Sum(x => x.DiscountAmount);
        var received = _orders.Sum(x => x.ReceivedAmount);
        SummaryText.Text = Localizer.Format("Orders.Summary", _orders.Count, total, discount, total - discount, received);
    }

    private async void OrderGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) => await LoadItemsAsync();

    private async void SearchButton_Click(object sender, System.Windows.RoutedEventArgs e) => await LoadAsync();

    private async void TodayButton_Click(object sender, System.Windows.RoutedEventArgs e) => await TodayAsync();

    private async void AllButton_Click(object sender, System.Windows.RoutedEventArgs e) => await AllAsync();
}
