using System.Collections.ObjectModel;
using System.Windows.Controls;
using POS_system_cs.Application.Models;
using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;
using POS_system_cs.UI.Wpf.Localization;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed partial class InventoryManagementPage : WpfUserControl
{
    private readonly IInventoryService _service;
    private readonly IProductService _productsService;
    private readonly ObservableCollection<StockOverview> _rows = [];
    private readonly ObservableCollection<Product> _products = [];

    public InventoryManagementPage(IInventoryService service, IProductService productsService)
    {
        _service = service;
        _productsService = productsService;
        InitializeComponent();
        ApplyLocalization();
        BuildList();
        BuildForm();
        Loaded += async (_, _) => await LoadAsync();
    }

    private void ApplyLocalization()
    {
        TitleText.Text = Localizer.T("Inventory.Title");
        DescriptionText.Text = Localizer.T("Inventory.Desc");
        FormTitleText.Text = Localizer.T("Inventory.Form");
        ProductLabel.Text = Localizer.T("Field.Product");
        QuantityLabel.Text = Localizer.T("Field.Quantity");
        ReasonLabel.Text = Localizer.T("Field.Reason");
        AdjustButton.Content = Localizer.T("Action.AdjustDelta");
        SetButton.Content = Localizer.T("Action.SetStock");
        RefreshButton.Content = Localizer.T("Action.Refresh");
    }

    private void BuildList()
    {
        InventoryGrid.ItemsSource = _rows;
        InventoryGrid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Code"), nameof(StockOverview.ProductCode), 110));
        InventoryGrid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Product"), nameof(StockOverview.ProductName), star: true));
        InventoryGrid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Barcode"), nameof(StockOverview.Barcode), 140));
        InventoryGrid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Stock"), nameof(StockOverview.Quantity), 90));
        InventoryGrid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Low"), nameof(StockOverview.LowStockThreshold), 80));
        InventoryGrid.Columns.Add(WpfUi.CheckColumn(Localizer.T("Metric.LowStock"), nameof(StockOverview.IsLowStock), 90));
    }

    private void BuildForm()
    {
        ProductComboBox.ItemsSource = _products;
        ProductComboBox.DisplayMemberPath = nameof(Product.Name);
        ProductComboBox.SelectedValuePath = nameof(Product.Id);
        QuantityBox.Text = "0.00";
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
            await _service.AdjustStockAsync(ProductId(), WpfUi.Number(QuantityBox.Text, allowNegative: true), ReasonBox.Text.Trim());
            _rows.ReplaceWith(await _service.GetOverviewAsync());
        }
        catch (Exception ex) { WpfUi.Error(this, ex); }
    }

    private async Task SetAsync()
    {
        try
        {
            await _service.SetStockAsync(ProductId(), WpfUi.Number(QuantityBox.Text));
            _rows.ReplaceWith(await _service.GetOverviewAsync());
        }
        catch (Exception ex) { WpfUi.Error(this, ex); }
    }

    private void Bind()
    {
        if (InventoryGrid.SelectedItem is not StockOverview row) return;
        ProductComboBox.SelectedValue = row.ProductId;
        QuantityBox.Text = row.Quantity.ToString("N2");
    }

    private Guid ProductId()
    {
        return ProductComboBox.SelectedValue is Guid id ? id : throw new InvalidOperationException(Localizer.T("Inventory.SelectProduct"));
    }

    private void InventoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) => Bind();

    private async void AdjustButton_Click(object sender, System.Windows.RoutedEventArgs e) => await AdjustAsync();

    private async void SetButton_Click(object sender, System.Windows.RoutedEventArgs e) => await SetAsync();

    private async void RefreshButton_Click(object sender, System.Windows.RoutedEventArgs e) => await LoadAsync();
}
