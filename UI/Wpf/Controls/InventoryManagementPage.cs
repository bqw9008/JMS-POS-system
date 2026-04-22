using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using POS_system_cs.Application.Models;
using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;
using POS_system_cs.UI.Wpf.Localization;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed class InventoryManagementPage : WpfUserControl
{
    private readonly IInventoryService _service;
    private readonly IProductService _productsService;
    private readonly ObservableCollection<StockOverview> _rows = [];
    private readonly ObservableCollection<Product> _products = [];
    private readonly WpfDataGrid _grid = WpfUi.Grid();
    private readonly WpfComboBox _product = WpfUi.Combo();
    private readonly WpfTextBox _quantity = WpfUi.TextBox();
    private readonly WpfTextBox _reason = WpfUi.TextBox();

    public InventoryManagementPage(IInventoryService service, IProductService productsService)
    {
        _service = service;
        _productsService = productsService;
        Content = WpfUi.SplitPage(Localizer.T("Inventory.Title"), Localizer.T("Inventory.Desc"), out var list, out var form);
        BuildList(list);
        BuildForm(form);
        Loaded += async (_, _) => await LoadAsync();
    }

    private void BuildList(Border host)
    {
        _grid.ItemsSource = _rows;
        _grid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Code"), nameof(StockOverview.ProductCode), 110));
        _grid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Product"), nameof(StockOverview.ProductName), star: true));
        _grid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Barcode"), nameof(StockOverview.Barcode), 140));
        _grid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Stock"), nameof(StockOverview.Quantity), 90));
        _grid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Low"), nameof(StockOverview.LowStockThreshold), 80));
        _grid.Columns.Add(WpfUi.CheckColumn(Localizer.T("Metric.LowStock"), nameof(StockOverview.IsLowStock), 90));
        _grid.SelectionChanged += (_, _) => Bind();
        host.Child = _grid;
    }

    private void BuildForm(Border host)
    {
        _product.ItemsSource = _products;
        _product.DisplayMemberPath = nameof(Product.Name);
        _product.SelectedValuePath = nameof(Product.Id);
        _quantity.Text = "0.00";

        var form = WpfUi.Form();
        form.Children.Add(WpfUi.Title(Localizer.T("Inventory.Form")));
        form.Children.Add(WpfUi.Field(Localizer.T("Field.Product"), _product));
        form.Children.Add(WpfUi.Field(Localizer.T("Field.Quantity"), _quantity));
        form.Children.Add(WpfUi.Field(Localizer.T("Field.Reason"), _reason));
        form.Children.Add(WpfUi.Primary(Localizer.T("Action.AdjustDelta"), async (_, _) => await AdjustAsync()));
        form.Children.Add(WpfUi.Secondary(Localizer.T("Action.SetStock"), async (_, _) => await SetAsync()));
        form.Children.Add(WpfUi.Secondary(Localizer.T("Action.Refresh"), async (_, _) => await LoadAsync()));
        host.Child = new ScrollViewer { Content = form };
    }

    private async Task LoadAsync()
    {
        try
        {
            _products.ReplaceWith((await _productsService.GetAllAsync()).Where(x => x.IsActive));
            _rows.ReplaceWith(await _service.GetOverviewAsync());
        }
        catch (Exception ex) { WpfUi.Error(this, ex); }
    }

    private async Task AdjustAsync()
    {
        try
        {
            await _service.AdjustStockAsync(ProductId(), WpfUi.Number(_quantity.Text, allowNegative: true), _reason.Text.Trim());
            _rows.ReplaceWith(await _service.GetOverviewAsync());
        }
        catch (Exception ex) { WpfUi.Error(this, ex); }
    }

    private async Task SetAsync()
    {
        try
        {
            await _service.SetStockAsync(ProductId(), WpfUi.Number(_quantity.Text));
            _rows.ReplaceWith(await _service.GetOverviewAsync());
        }
        catch (Exception ex) { WpfUi.Error(this, ex); }
    }

    private void Bind()
    {
        if (_grid.SelectedItem is not StockOverview row) return;
        _product.SelectedValue = row.ProductId;
        _quantity.Text = row.Quantity.ToString("N2");
    }

    private Guid ProductId()
    {
        return _product.SelectedValue is Guid id ? id : throw new InvalidOperationException(Localizer.T("Inventory.SelectProduct"));
    }
}
