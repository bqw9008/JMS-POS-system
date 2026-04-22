using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;

namespace POS_system_cs.UI.Controls;

public sealed class ProductManagementControl : UserControl
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly DataGridView _grid = new();
    private readonly TextBox _keywordTextBox = new();
    private readonly TextBox _codeTextBox = new();
    private readonly TextBox _nameTextBox = new();
    private readonly TextBox _barcodeTextBox = new();
    private readonly ComboBox _categoryComboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _costPriceBox = CreateMoneyBox();
    private readonly NumericUpDown _salePriceBox = CreateMoneyBox();
    private readonly NumericUpDown _lowStockBox = CreateMoneyBox();
    private readonly CheckBox _isActiveCheckBox = new() { Text = "启用", Checked = true, AutoSize = true };
    private Product? _selectedProduct;

    public ProductManagementControl(IProductService productService, ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
        Dock = DockStyle.Fill;
        BuildLayout();
        _ = LoadAsync();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 8)
        };
        _keywordTextBox.Width = 260;
        toolbar.Controls.Add(new Label { Text = "关键字", AutoSize = true, Margin = new Padding(0, 8, 8, 0) });
        toolbar.Controls.Add(_keywordTextBox);
        toolbar.Controls.Add(CreateButton("搜索", async (_, _) => await LoadProductsAsync()));
        toolbar.Controls.Add(CreateButton("新增", (_, _) => ClearForm()));

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel2,
            SplitterDistance = 760
        };

        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "编码", DataPropertyName = nameof(Product.Code), Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "名称", DataPropertyName = nameof(Product.Name), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "条码", DataPropertyName = nameof(Product.Barcode), Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "售价", DataPropertyName = nameof(Product.SalePrice), Width = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "预警", DataPropertyName = nameof(Product.LowStockThreshold), Width = 80 });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "启用", DataPropertyName = nameof(Product.IsActive), Width = 70 });
        _grid.SelectionChanged += (_, _) => BindSelectedProduct();

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1
        };
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _codeTextBox.Dock = DockStyle.Top;
        _nameTextBox.Dock = DockStyle.Top;
        _barcodeTextBox.Dock = DockStyle.Top;
        _categoryComboBox.Dock = DockStyle.Top;
        _costPriceBox.Dock = DockStyle.Top;
        _salePriceBox.Dock = DockStyle.Top;
        _lowStockBox.Dock = DockStyle.Top;

        form.Controls.Add(new Label { Text = "商品信息", AutoSize = true, Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold) });
        form.Controls.Add(CreateLabel("商品编码"));
        form.Controls.Add(_codeTextBox);
        form.Controls.Add(CreateLabel("商品名称"));
        form.Controls.Add(_nameTextBox);
        form.Controls.Add(CreateLabel("条码"));
        form.Controls.Add(_barcodeTextBox);
        form.Controls.Add(CreateLabel("分类"));
        form.Controls.Add(_categoryComboBox);
        form.Controls.Add(CreateLabel("进价"));
        form.Controls.Add(_costPriceBox);
        form.Controls.Add(CreateLabel("售价"));
        form.Controls.Add(_salePriceBox);
        form.Controls.Add(CreateLabel("库存预警值"));
        form.Controls.Add(_lowStockBox);
        form.Controls.Add(_isActiveCheckBox);
        form.Controls.Add(CreateButton("保存", async (_, _) => await SaveAsync()));
        form.Controls.Add(CreateButton("停用", async (_, _) => await DisableAsync()));

        split.Panel1.Controls.Add(_grid);
        split.Panel2.Controls.Add(form);
        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(split, 0, 1);
        Controls.Add(root);
    }

    private async Task LoadAsync()
    {
        await LoadCategoriesAsync();
        await LoadProductsAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = (await _categoryService.GetAllAsync()).Where(c => c.IsActive).ToList();
            _categoryComboBox.DataSource = categories;
            _categoryComboBox.DisplayMember = nameof(Category.Name);
            _categoryComboBox.ValueMember = nameof(Category.Id);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            _grid.DataSource = (await _productService.SearchAsync(_keywordTextBox.Text)).ToList();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            var product = _selectedProduct ?? new Product();
            product.Code = _codeTextBox.Text;
            product.Name = _nameTextBox.Text;
            product.Barcode = _barcodeTextBox.Text;
            product.CategoryId = _categoryComboBox.SelectedValue is Guid categoryId ? categoryId : Guid.Empty;
            product.CostPrice = _costPriceBox.Value;
            product.SalePrice = _salePriceBox.Value;
            product.LowStockThreshold = _lowStockBox.Value;
            product.IsActive = _isActiveCheckBox.Checked;

            await _productService.SaveAsync(product);
            await LoadProductsAsync();
            ClearForm();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task DisableAsync()
    {
        if (_selectedProduct is null)
        {
            return;
        }

        if (MessageBox.Show("确认停用当前商品？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            await _productService.DeleteAsync(_selectedProduct.Id);
            await LoadProductsAsync();
            ClearForm();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void BindSelectedProduct()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Product product)
        {
            return;
        }

        _selectedProduct = product;
        _codeTextBox.Text = product.Code;
        _nameTextBox.Text = product.Name;
        _barcodeTextBox.Text = product.Barcode;
        _categoryComboBox.SelectedValue = product.CategoryId;
        _costPriceBox.Value = Clamp(product.CostPrice, _costPriceBox.Minimum, _costPriceBox.Maximum);
        _salePriceBox.Value = Clamp(product.SalePrice, _salePriceBox.Minimum, _salePriceBox.Maximum);
        _lowStockBox.Value = Clamp(product.LowStockThreshold, _lowStockBox.Minimum, _lowStockBox.Maximum);
        _isActiveCheckBox.Checked = product.IsActive;
    }

    private void ClearForm()
    {
        _selectedProduct = null;
        _grid.ClearSelection();
        _codeTextBox.Clear();
        _nameTextBox.Clear();
        _barcodeTextBox.Clear();
        _costPriceBox.Value = 0;
        _salePriceBox.Value = 0;
        _lowStockBox.Value = 0;
        _isActiveCheckBox.Checked = true;
        if (_categoryComboBox.Items.Count > 0)
        {
            _categoryComboBox.SelectedIndex = 0;
        }
        _codeTextBox.Focus();
    }

    private static NumericUpDown CreateMoneyBox()
    {
        return new NumericUpDown
        {
            DecimalPlaces = 2,
            Maximum = 1000000,
            Minimum = 0,
            ThousandsSeparator = true
        };
    }

    private static decimal Clamp(decimal value, decimal min, decimal max) => Math.Min(Math.Max(value, min), max);

    private static Label CreateLabel(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(0, 10, 0, 4) };

    private static Button CreateButton(string text, EventHandler onClick)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 32, Margin = new Padding(0, 0, 8, 0) };
        button.Click += onClick;
        return button;
    }

    private static void ShowError(Exception ex)
    {
        MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
