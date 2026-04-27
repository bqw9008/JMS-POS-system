using System.Collections.ObjectModel;
using System.Windows;
using POS_system_cs.Domain.Entities;
using POS_system_cs.UI.Wpf.Localization;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed partial class ProductEditorWindow : Window
{
    private readonly Product _product;
    private readonly ObservableCollection<Category> _categories;

    public ProductEditorWindow(Product? product, IEnumerable<Category> categories)
    {
        _product = product is null ? new Product() : Clone(product);
        _categories = new ObservableCollection<Category>(categories.Where(x => x.IsActive || x.Id == _product.CategoryId));

        InitializeComponent();
        Title = product is null ? Localizer.T("Action.New") : Localizer.T("Action.Edit");
        ApplyLocalization();
        BuildCategoryList();
        Bind();

        Loaded += (_, _) =>
        {
            CodeBox.Focus();
            CodeBox.SelectAll();
        };
    }

    public Product Result { get; private set; } = new();

    private void ApplyLocalization()
    {
        CodeLabel.Text = Localizer.T("Field.Code");
        NameLabel.Text = Localizer.T("Field.Name");
        BarcodeLabel.Text = Localizer.T("Field.Barcode");
        CategoryLabel.Text = Localizer.T("Field.Category");
        CostLabel.Text = Localizer.T("Field.Cost");
        PriceLabel.Text = Localizer.T("Field.Price");
        LowStockLabel.Text = Localizer.T("Field.LowStock");
        ActiveCheckBox.Content = Localizer.T("Field.Active");
        SaveButton.Content = Localizer.T("Action.Save");
        CancelButton.Content = Localizer.T("Action.Cancel");
    }

    private void BuildCategoryList()
    {
        CategoryComboBox.ItemsSource = _categories;
        CategoryComboBox.DisplayMemberPath = nameof(Category.Name);
        CategoryComboBox.SelectedValuePath = nameof(Category.Id);
    }

    private void Bind()
    {
        CodeBox.Text = _product.Code;
        NameBox.Text = _product.Name;
        BarcodeBox.Text = _product.Barcode;
        CategoryComboBox.SelectedValue = _product.CategoryId;
        if (CategoryComboBox.SelectedIndex < 0 && _categories.Count > 0)
        {
            CategoryComboBox.SelectedIndex = 0;
        }

        CostBox.Text = _product.CostPrice.ToString("N2");
        PriceBox.Text = _product.SalePrice.ToString("N2");
        LowStockBox.Text = _product.LowStockThreshold.ToString("N2");
        ActiveCheckBox.IsChecked = _product.IsActive;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _product.Code = CodeBox.Text.Trim();
        _product.Name = NameBox.Text.Trim();
        _product.Barcode = BarcodeBox.Text.Trim();
        _product.CategoryId = CategoryComboBox.SelectedValue is Guid id ? id : Guid.Empty;
        _product.CostPrice = WpfUi.Number(CostBox.Text);
        _product.SalePrice = WpfUi.Number(PriceBox.Text);
        _product.LowStockThreshold = WpfUi.Number(LowStockBox.Text);
        _product.IsActive = ActiveCheckBox.IsChecked == true;

        Result = _product;
        DialogResult = true;
    }

    private static Product Clone(Product product)
    {
        return new Product
        {
            Id = product.Id,
            Code = product.Code,
            Name = product.Name,
            Barcode = product.Barcode,
            CategoryId = product.CategoryId,
            CostPrice = product.CostPrice,
            SalePrice = product.SalePrice,
            LowStockThreshold = product.LowStockThreshold,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}
