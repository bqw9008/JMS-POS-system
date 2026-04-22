using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;
using POS_system_cs.UI.Wpf.Localization;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfDatePicker = System.Windows.Controls.DatePicker;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed class SalesRecordPage : WpfUserControl
{
    private readonly IOrderService _service;
    private readonly ObservableCollection<Order> _orders = [];
    private readonly ObservableCollection<OrderItem> _items = [];
    private readonly WpfDataGrid _orderGrid = WpfUi.Grid();
    private readonly WpfDataGrid _itemGrid = WpfUi.Grid();
    private readonly WpfDatePicker _from = new() { SelectedDate = DateTime.Today, Width = 140 };
    private readonly WpfDatePicker _to = new() { SelectedDate = DateTime.Today, Width = 140 };
    private readonly WpfCheckBox _useFrom = new() { Content = Localizer.T("Field.From"), IsChecked = true, VerticalAlignment = VerticalAlignment.Center };
    private readonly WpfCheckBox _useTo = new() { Content = Localizer.T("Field.To"), IsChecked = true, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _summary = new();

    public SalesRecordPage(IOrderService service)
    {
        _service = service;
        Content = Build();
        Loaded += async (_, _) => await LoadAsync();
    }

    private Grid Build()
    {
        var root = new Grid { Margin = new Thickness(22) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var toolbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 14), VerticalAlignment = VerticalAlignment.Center };
        toolbar.Children.Add(WpfUi.Header(Localizer.T("Orders.Title"), Localizer.T("Orders.Desc")));
        toolbar.Children.Add(_useFrom);
        toolbar.Children.Add(_from);
        toolbar.Children.Add(_useTo);
        toolbar.Children.Add(_to);
        toolbar.Children.Add(WpfUi.Primary(Localizer.T("Action.Search"), async (_, _) => await LoadAsync(), compact: true));
        toolbar.Children.Add(WpfUi.Secondary(Localizer.T("Action.Today"), async (_, _) => await TodayAsync(), compact: true));
        toolbar.Children.Add(WpfUi.Secondary(Localizer.T("Action.All"), async (_, _) => await AllAsync(), compact: true));
        root.Children.Add(toolbar);

        _orderGrid.ItemsSource = _orders;
        _orderGrid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.OrderNo"), nameof(Order.OrderNo), 180));
        _orderGrid.Columns.Add(WpfUi.DateColumn(Localizer.T("Field.Time"), nameof(Order.OrderedAt), 170));
        _orderGrid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Total"), nameof(Order.TotalAmount), 90));
        _orderGrid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Discount"), nameof(Order.DiscountAmount), 90));
        _orderGrid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Received"), nameof(Order.ReceivedAmount), 100));
        _orderGrid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Change"), nameof(Order.ChangeAmount), 90));
        _orderGrid.Columns.Add(WpfUi.PaymentColumn(Localizer.T("Field.Payment"), nameof(Order.PaymentMethod), star: true));
        _orderGrid.SelectionChanged += async (_, _) => await LoadItemsAsync();

        _itemGrid.ItemsSource = _items;
        _itemGrid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Product"), nameof(OrderItem.ProductName), star: true));
        _itemGrid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Barcode"), nameof(OrderItem.Barcode), 160));
        _itemGrid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Qty"), nameof(OrderItem.Quantity), 90));
        _itemGrid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Price"), nameof(OrderItem.UnitPrice), 90));
        _itemGrid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Amount"), nameof(OrderItem.Amount), 100));

        var grids = new Grid();
        grids.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58, GridUnitType.Star) });
        grids.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42, GridUnitType.Star) });
        grids.Children.Add(WpfUi.Section(Localizer.T("Section.Orders"), _orderGrid, new Thickness(0, 0, 0, 14)));
        var items = WpfUi.Section(Localizer.T("Section.Items"), _itemGrid, new Thickness(0));
        Grid.SetRow(items, 1);
        grids.Children.Add(items);
        Grid.SetRow(grids, 1);
        root.Children.Add(grids);

        _summary.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
        _summary.FontWeight = FontWeights.SemiBold;
        _summary.Margin = new Thickness(0, 14, 0, 0);
        Grid.SetRow(_summary, 2);
        root.Children.Add(_summary);
        return root;
    }

    private async Task LoadAsync()
    {
        try
        {
            var from = _useFrom.IsChecked == true && _from.SelectedDate is DateTime fd ? DateOnly.FromDateTime(fd.Date) : (DateOnly?)null;
            var to = _useTo.IsChecked == true && _to.SelectedDate is DateTime td ? DateOnly.FromDateTime(td.Date) : (DateOnly?)null;
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
        if (_orderGrid.SelectedItem is not Order order) return;
        try { _items.ReplaceWith(await _service.GetItemsAsync(order.Id)); }
        catch (Exception ex) { WpfUi.Error(this, ex); }
    }

    private async Task TodayAsync()
    {
        _useFrom.IsChecked = true;
        _useTo.IsChecked = true;
        _from.SelectedDate = DateTime.Today;
        _to.SelectedDate = DateTime.Today;
        await LoadAsync();
    }

    private async Task AllAsync()
    {
        _useFrom.IsChecked = false;
        _useTo.IsChecked = false;
        await LoadAsync();
    }

    private void Summary()
    {
        var total = _orders.Sum(x => x.TotalAmount);
        var discount = _orders.Sum(x => x.DiscountAmount);
        var received = _orders.Sum(x => x.ReceivedAmount);
        _summary.Text = Localizer.Format("Orders.Summary", _orders.Count, total, discount, total - discount, received);
    }
}
