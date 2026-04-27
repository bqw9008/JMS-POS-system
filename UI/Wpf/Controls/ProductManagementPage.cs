using System.Collections.ObjectModel;
using System.Windows.Controls;
using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;
using POS_system_cs.UI.Wpf.Localization;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed partial class ProductManagementPage : WpfUserControl
{
    private readonly IProductService _products;
    private readonly ICategoryService _categoriesService;
    private readonly ObservableCollection<Product> _rows = [];
    private readonly ObservableCollection<Category> _categories = [];
    private Product? _selected;

    public ProductManagementPage(IProductService products, ICategoryService categoriesService)
    {
        _products = products;
        _categoriesService = categoriesService;
        InitializeComponent();
        ApplyLocalization();
        BuildGrid();
        Clear();
        Loaded += async (_, _) => await LoadAsync();
    }

    private void ApplyLocalization()
    {
        TitleText.Text = Localizer.T("Product.Title");
        DescriptionText.Text = Localizer.T("Product.Desc");
        SearchButton.Content = Localizer.T("Action.Search");
        NewButton.Content = Localizer.T("Action.New");
        FormTitleText.Text = Localizer.T("Product.Form");
        CodeLabel.Text = Localizer.T("Field.Code");
        NameLabel.Text = Localizer.T("Field.Name");
        BarcodeLabel.Text = Localizer.T("Field.Barcode");
        CategoryLabel.Text = Localizer.T("Field.Category");
        CostLabel.Text = Localizer.T("Field.Cost");
        PriceLabel.Text = Localizer.T("Field.Price");
        LowStockLabel.Text = Localizer.T("Field.LowStock");
        ActiveCheckBox.Content = Localizer.T("Field.Active");
        EditButton.Content = Localizer.T("Action.Edit");
        DeleteButton.Content = Localizer.T("Action.DeleteDisable");
    }

    private void BuildGrid()
    {
        ProductGrid.ItemsSource = _rows;
        ProductGrid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Code"), nameof(Product.Code), 110));
        ProductGrid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Name"), nameof(Product.Name), star: true));
        ProductGrid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Barcode"), nameof(Product.Barcode), 140));
        ProductGrid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Price"), nameof(Product.SalePrice), 90));
        ProductGrid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Low"), nameof(Product.LowStockThreshold), 80));
        ProductGrid.Columns.Add(WpfUi.CheckColumn(Localizer.T("Field.Active"), nameof(Product.IsActive), 80));
    }

    private async Task LoadAsync()
    {
        await LoadCategoriesAsync();
        await LoadProductsAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        try { _categories.ReplaceWith((await _categoriesService.GetAllAsync()).Where(x => x.IsActive)); }
        catch (Exception ex) { WpfUi.Error(this, ex); }
    }

    private async Task LoadProductsAsync()
    {
        try { _rows.ReplaceWith(await _products.SearchAsync(KeywordBox.Text)); }
        catch (Exception ex) { WpfUi.Error(this, ex); }
    }

    private async Task SaveAsync(Product product)
    {
        try
        {
            await _products.SaveAsync(product);
            await LoadProductsAsync();
            Clear();
        }
        catch (Exception ex) { WpfUi.Error(this, ex); }
    }

    private async Task DeleteAsync()
    {
        if (_selected is null || !WpfUi.Confirm(this, Localizer.T("Product.DeleteConfirm"))) return;
        try
        {
            await _products.DeleteAsync(_selected.Id);
            await LoadProductsAsync();
            Clear();
        }
        catch (Exception ex) { WpfUi.Error(this, ex); }
    }

    private void Bind()
    {
        if (ProductGrid.SelectedItem is not Product row) return;
        _selected = row;
        CodeBox.Text = row.Code;
        NameBox.Text = row.Name;
        BarcodeBox.Text = row.Barcode;
        CategoryBox.Text = CategoryName(row.CategoryId);
        CostBox.Text = row.CostPrice.ToString("N2");
        PriceBox.Text = row.SalePrice.ToString("N2");
        LowStockBox.Text = row.LowStockThreshold.ToString("N2");
        ActiveCheckBox.IsChecked = row.IsActive;
        EditButton.IsEnabled = true;
        DeleteButton.IsEnabled = true;
    }

    private void Clear()
    {
        _selected = null;
        ProductGrid.SelectedItem = null;
        CodeBox.Clear();
        NameBox.Clear();
        BarcodeBox.Clear();
        CategoryBox.Clear();
        CostBox.Text = "0.00";
        PriceBox.Text = "0.00";
        LowStockBox.Text = "0.00";
        ActiveCheckBox.IsChecked = true;
        EditButton.IsEnabled = false;
        DeleteButton.IsEnabled = false;
    }

    private string CategoryName(Guid categoryId)
    {
        return _categories.FirstOrDefault(x => x.Id == categoryId)?.Name ?? string.Empty;
    }

    private async Task OpenEditorAsync(Product? product)
    {
        if (_categories.Count == 0)
        {
            await LoadCategoriesAsync();
        }

        var dialog = new ProductEditorWindow(product, _categories)
        {
            Owner = System.Windows.Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            await SaveAsync(dialog.Result);
        }
    }

    private void ProductGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) => Bind();

    private async void SearchButton_Click(object sender, System.Windows.RoutedEventArgs e) => await LoadProductsAsync();

    private async void NewButton_Click(object sender, System.Windows.RoutedEventArgs e) => await OpenEditorAsync(null);

    private async void EditButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_selected is not null)
        {
            await OpenEditorAsync(_selected);
        }
    }

    private async void DeleteButton_Click(object sender, System.Windows.RoutedEventArgs e) => await DeleteAsync();
}
