using POS_system_cs.Application.Models;
using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;

namespace POS_system_cs.UI.Controls;

public sealed class InventoryManagementControl : UserControl
{
    private readonly IInventoryService _inventoryService;
    private readonly IProductService _productService;
    private readonly DataGridView _grid = new();
    private readonly ComboBox _productComboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _quantityBox = new()
    {
        DecimalPlaces = 2,
        Minimum = -1000000,
        Maximum = 1000000,
        ThousandsSeparator = true
    };
    private readonly TextBox _reasonTextBox = new();

    public InventoryManagementControl(IInventoryService inventoryService, IProductService productService)
    {
        _inventoryService = inventoryService;
        _productService = productService;
        Dock = DockStyle.Fill;
        BuildLayout();
        _ = LoadAsync();
    }

    private void BuildLayout()
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));

        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "编码", DataPropertyName = nameof(StockOverview.ProductCode), Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "商品", DataPropertyName = nameof(StockOverview.ProductName), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "条码", DataPropertyName = nameof(StockOverview.Barcode), Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "库存", DataPropertyName = nameof(StockOverview.Quantity), Width = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "预警", DataPropertyName = nameof(StockOverview.LowStockThreshold), Width = 90 });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "低库存", DataPropertyName = nameof(StockOverview.IsLowStock), Width = 80 });
        _grid.SelectionChanged += (_, _) => BindSelectedProduct();

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1
        };

        _productComboBox.Dock = DockStyle.Top;
        _quantityBox.Dock = DockStyle.Top;
        _reasonTextBox.Dock = DockStyle.Top;
        _reasonTextBox.PlaceholderText = "例如：采购入库 / 盘点修正";

        form.Controls.Add(new Label { Text = "库存调整", AutoSize = true, Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold) });
        form.Controls.Add(CreateLabel("商品"));
        form.Controls.Add(_productComboBox);
        form.Controls.Add(CreateLabel("数量"));
        form.Controls.Add(_quantityBox);
        form.Controls.Add(CreateLabel("原因"));
        form.Controls.Add(_reasonTextBox);
        form.Controls.Add(CreateButton("按增减调整", async (_, _) => await AdjustAsync()));
        form.Controls.Add(CreateButton("直接设置库存", async (_, _) => await SetStockAsync()));
        form.Controls.Add(CreateButton("刷新", async (_, _) => await LoadAsync()));

        var formHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };
        formHost.Controls.Add(form);

        content.Controls.Add(_grid, 0, 0);
        content.Controls.Add(formHost, 1, 0);
        Controls.Add(content);
    }

    private async Task LoadAsync()
    {
        await LoadProductsAsync();
        await LoadInventoryAsync();
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            var products = (await _productService.GetAllAsync()).Where(p => p.IsActive).ToList();
            _productComboBox.DataSource = products;
            _productComboBox.DisplayMember = nameof(Product.Name);
            _productComboBox.ValueMember = nameof(Product.Id);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task LoadInventoryAsync()
    {
        try
        {
            _grid.DataSource = (await _inventoryService.GetOverviewAsync()).ToList();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task AdjustAsync()
    {
        try
        {
            await _inventoryService.AdjustStockAsync(GetSelectedProductId(), _quantityBox.Value, _reasonTextBox.Text);
            await LoadInventoryAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task SetStockAsync()
    {
        try
        {
            await _inventoryService.SetStockAsync(GetSelectedProductId(), _quantityBox.Value);
            await LoadInventoryAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void BindSelectedProduct()
    {
        if (_grid.CurrentRow?.DataBoundItem is StockOverview row)
        {
            _productComboBox.SelectedValue = row.ProductId;
            _quantityBox.Value = Clamp(row.Quantity, _quantityBox.Minimum, _quantityBox.Maximum);
        }
    }

    private Guid GetSelectedProductId()
    {
        return _productComboBox.SelectedValue is Guid productId
            ? productId
            : throw new InvalidOperationException("请选择商品。");
    }

    private static decimal Clamp(decimal value, decimal min, decimal max) => Math.Min(Math.Max(value, min), max);

    private static Label CreateLabel(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(0, 10, 0, 4) };

    private static Button CreateButton(string text, EventHandler onClick)
    {
        var button = new Button { Text = text, Dock = DockStyle.Top, Height = 36, Margin = new Padding(0, 8, 0, 0) };
        button.Click += onClick;
        return button;
    }

    private static void ShowError(Exception ex)
    {
        MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
