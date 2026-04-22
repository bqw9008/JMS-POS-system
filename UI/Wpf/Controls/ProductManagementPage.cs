using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;
using POS_system_cs.UI.Wpf.Localization;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed class ProductManagementPage : WpfUserControl
{
    private readonly IProductService _products;
    private readonly ICategoryService _categoriesService;
    private readonly ObservableCollection<Product> _rows = [];
    private readonly ObservableCollection<Category> _categories = [];
    private readonly WpfDataGrid _grid = WpfUi.Grid();
    private readonly WpfTextBox _keyword = WpfUi.TextBox();
    private readonly WpfTextBox _code = WpfUi.TextBox();
    private readonly WpfTextBox _name = WpfUi.TextBox();
    private readonly WpfTextBox _barcode = WpfUi.TextBox();
    private readonly WpfComboBox _category = WpfUi.Combo();
    private readonly WpfTextBox _cost = WpfUi.TextBox();
    private readonly WpfTextBox _price = WpfUi.TextBox();
    private readonly WpfTextBox _lowStock = WpfUi.TextBox();
    private readonly WpfCheckBox _active = new() { Content = Localizer.T("Field.Active"), IsChecked = true };
    private Product? _selected;

    public ProductManagementPage(IProductService products, ICategoryService categoriesService)
    {
        _products = products;
        _categoriesService = categoriesService;
        Content = Build();
        Loaded += async (_, _) => await LoadAsync();
    }

    private Grid Build()
    {
        var root = new Grid { Margin = new Thickness(22) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var toolbar = new DockPanel { Margin = new Thickness(0, 0, 0, 14) };
        toolbar.Children.Add(WpfUi.Header(Localizer.T("Product.Title"), Localizer.T("Product.Desc")));
        var actions = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        _keyword.Width = 260;
        actions.Children.Add(_keyword);
        actions.Children.Add(WpfUi.Primary(Localizer.T("Action.Search"), async (_, _) => await LoadProductsAsync(), compact: true));
        actions.Children.Add(WpfUi.Secondary(Localizer.T("Action.New"), (_, _) => Clear(), compact: true));
        toolbar.Children.Add(actions);
        root.Children.Add(toolbar);

        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(330) });
        Grid.SetRow(content, 1);
        root.Children.Add(content);

        _grid.ItemsSource = _rows;
        _grid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Code"), nameof(Product.Code), 110));
        _grid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Name"), nameof(Product.Name), star: true));
        _grid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Barcode"), nameof(Product.Barcode), 140));
        _grid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Price"), nameof(Product.SalePrice), 90));
        _grid.Columns.Add(WpfUi.MoneyColumn(Localizer.T("Field.Low"), nameof(Product.LowStockThreshold), 80));
        _grid.Columns.Add(WpfUi.CheckColumn(Localizer.T("Field.Active"), nameof(Product.IsActive), 80));
        _grid.SelectionChanged += (_, _) => Bind();
        content.Children.Add(WpfUi.Card(_grid, new Thickness(0, 0, 16, 0)));

        _category.ItemsSource = _categories;
        _category.DisplayMemberPath = nameof(Category.Name);
        _category.SelectedValuePath = nameof(Category.Id);

        var form = WpfUi.Form();
        form.Children.Add(WpfUi.Title(Localizer.T("Product.Form")));
        form.Children.Add(WpfUi.Field(Localizer.T("Field.Code"), _code));
        form.Children.Add(WpfUi.Field(Localizer.T("Field.Name"), _name));
        form.Children.Add(WpfUi.Field(Localizer.T("Field.Barcode"), _barcode));
        form.Children.Add(WpfUi.Field(Localizer.T("Field.Category"), _category));
        form.Children.Add(WpfUi.Field(Localizer.T("Field.Cost"), _cost));
        form.Children.Add(WpfUi.Field(Localizer.T("Field.Price"), _price));
        form.Children.Add(WpfUi.Field(Localizer.T("Field.LowStock"), _lowStock));
        form.Children.Add(_active);
        form.Children.Add(WpfUi.Primary(Localizer.T("Action.Save"), async (_, _) => await SaveAsync()));
        form.Children.Add(WpfUi.Danger(Localizer.T("Action.DeleteDisable"), async (_, _) => await DeleteAsync()));
        var formCard = WpfUi.Card(new ScrollViewer { Content = form }, new Thickness(0));
        Grid.SetColumn(formCard, 1);
        content.Children.Add(formCard);
        return root;
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
        try { _rows.ReplaceWith(await _products.SearchAsync(_keyword.Text)); }
        catch (Exception ex) { WpfUi.Error(this, ex); }
    }

    private async Task SaveAsync()
    {
        try
        {
            var row = _selected ?? new Product();
            row.Code = _code.Text.Trim();
            row.Name = _name.Text.Trim();
            row.Barcode = _barcode.Text.Trim();
            row.CategoryId = _category.SelectedValue is Guid id ? id : Guid.Empty;
            row.CostPrice = WpfUi.Number(_cost.Text);
            row.SalePrice = WpfUi.Number(_price.Text);
            row.LowStockThreshold = WpfUi.Number(_lowStock.Text);
            row.IsActive = _active.IsChecked == true;
            await _products.SaveAsync(row);
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
        if (_grid.SelectedItem is not Product row) return;
        _selected = row;
        _code.Text = row.Code;
        _name.Text = row.Name;
        _barcode.Text = row.Barcode;
        _category.SelectedValue = row.CategoryId;
        _cost.Text = row.CostPrice.ToString("N2");
        _price.Text = row.SalePrice.ToString("N2");
        _lowStock.Text = row.LowStockThreshold.ToString("N2");
        _active.IsChecked = row.IsActive;
    }

    private void Clear()
    {
        _selected = null;
        _grid.SelectedItem = null;
        _code.Clear();
        _name.Clear();
        _barcode.Clear();
        _cost.Text = "0.00";
        _price.Text = "0.00";
        _lowStock.Text = "0.00";
        _active.IsChecked = true;
        _category.SelectedIndex = _categories.Count > 0 ? 0 : -1;
    }
}
