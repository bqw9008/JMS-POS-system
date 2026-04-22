using System.ComponentModel;
using POS_system_cs.Application.Models;
using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;
using POS_system_cs.Domain.Enums;

namespace POS_system_cs.UI.Controls;

public sealed class CashierControl : UserControl
{
    private readonly IProductService _productService;
    private readonly ICashierService _cashierService;
    private readonly BindingList<CashierCartItem> _cart = [];
    private readonly BindingSource _cartBindingSource = new();
    private readonly DataGridView _cartGrid = new();
    private readonly TextBox _productInputTextBox = new();
    private readonly NumericUpDown _discountBox = CreateMoneyBox();
    private readonly NumericUpDown _receivedBox = CreateMoneyBox();
    private readonly Label _totalAmountLabel = CreateAmountLabel("0.00");
    private readonly Label _payableAmountLabel = CreateAmountLabel("0.00");
    private readonly Label _changeAmountLabel = CreateAmountLabel("0.00");

    public CashierControl(IProductService productService, ICashierService cashierService)
    {
        _productService = productService;
        _cashierService = cashierService;
        Dock = DockStyle.Fill;
        BuildLayout();
        RefreshTotals();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.FromArgb(247, 249, 252)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildInputPanel(), 0, 0);
        root.Controls.Add(BuildCartGrid(), 0, 1);
        root.Controls.Add(BuildCheckoutPanel(), 0, 2);
        Controls.Add(root);
    }

    private Control BuildInputPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            Padding = new Padding(0, 0, 0, 14)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var label = new Label
        {
            Text = "商品编码 / 条码 / 名称",
            AutoSize = true,
            Margin = new Padding(0, 8, 10, 0)
        };

        _productInputTextBox.Dock = DockStyle.Fill;
        _productInputTextBox.Font = new Font("Microsoft YaHei UI", 12F);
        _productInputTextBox.PlaceholderText = "输入后回车加入购物车";
        _productInputTextBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await AddProductFromInputAsync();
            }
        };

        var addButton = CreatePrimaryButton("加入购物车", async (_, _) => await AddProductFromInputAsync());
        var clearButton = CreateSecondaryButton("清空购物车", (_, _) => ClearCart());

        panel.Controls.Add(label, 0, 0);
        panel.Controls.Add(_productInputTextBox, 1, 0);
        panel.Controls.Add(addButton, 2, 0);
        panel.Controls.Add(clearButton, 3, 0);
        return panel;
    }

    private Control BuildCartGrid()
    {
        _cartBindingSource.DataSource = _cart;

        _cartGrid.Dock = DockStyle.Fill;
        _cartGrid.AutoGenerateColumns = false;
        _cartGrid.DataSource = _cartBindingSource;
        _cartGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _cartGrid.MultiSelect = false;
        _cartGrid.AllowUserToAddRows = false;
        _cartGrid.RowHeadersVisible = false;
        _cartGrid.BackgroundColor = Color.White;
        _cartGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "编码", DataPropertyName = nameof(CashierCartItem.Code), ReadOnly = true, Width = 110 });
        _cartGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "商品", DataPropertyName = nameof(CashierCartItem.Name), ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _cartGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "条码", DataPropertyName = nameof(CashierCartItem.Barcode), ReadOnly = true, Width = 150 });
        _cartGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "单价", DataPropertyName = nameof(CashierCartItem.UnitPrice), ReadOnly = true, Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _cartGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "数量", DataPropertyName = nameof(CashierCartItem.Quantity), Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _cartGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "金额", DataPropertyName = nameof(CashierCartItem.Amount), ReadOnly = true, Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _cartGrid.CellEndEdit += (_, _) => NormalizeCartQuantities();
        _cartGrid.UserDeletingRow += (_, _) => BeginInvoke(RefreshTotals);

        var container = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(1)
        };
        container.Controls.Add(_cartGrid);
        return container;
    }

    private Control BuildCheckoutPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            ColumnCount = 8,
            Padding = new Padding(0, 14, 0, 0)
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _discountBox.ValueChanged += (_, _) => RefreshTotals();
        _receivedBox.ValueChanged += (_, _) => RefreshTotals();

        var totals = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        totals.Controls.Add(CreateSummaryItem("合计", _totalAmountLabel));
        totals.Controls.Add(CreateSummaryItem("应收", _payableAmountLabel));
        totals.Controls.Add(CreateSummaryItem("找零", _changeAmountLabel));

        var checkoutButton = CreatePrimaryButton("现金结算", async (_, _) => await CheckoutAsync());
        checkoutButton.Width = 130;
        checkoutButton.Height = 44;

        panel.Controls.Add(CreateFieldLabel("优惠"), 0, 0);
        panel.Controls.Add(_discountBox, 1, 0);
        panel.Controls.Add(CreateFieldLabel("实收"), 2, 0);
        panel.Controls.Add(_receivedBox, 3, 0);
        panel.Controls.Add(totals, 4, 0);
        panel.SetColumnSpan(totals, 3);
        panel.Controls.Add(checkoutButton, 7, 0);
        return panel;
    }

    private async Task AddProductFromInputAsync()
    {
        var input = _productInputTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        try
        {
            var product = await _productService.FindByCodeOrBarcodeAsync(input);
            if (product is null)
            {
                var matches = await _productService.SearchAsync(input);
                var activeMatches = matches.Where(p => p.IsActive).ToList();
                product = activeMatches.Count == 1 ? activeMatches[0] : null;

                if (activeMatches.Count > 1)
                {
                    MessageBox.Show("找到多个匹配商品，请输入更准确的编码或条码。", "商品不唯一", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            if (product is null)
            {
                MessageBox.Show("未找到商品。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            AddToCart(product);
            _productInputTextBox.Clear();
            _productInputTextBox.Focus();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void AddToCart(Product product)
    {
        var existing = _cart.FirstOrDefault(item => item.ProductId == product.Id);
        if (existing is not null)
        {
            existing.Quantity += 1;
        }
        else
        {
            _cart.Add(new CashierCartItem
            {
                ProductId = product.Id,
                Code = product.Code,
                Name = product.Name,
                Barcode = product.Barcode,
                UnitPrice = product.SalePrice,
                Quantity = 1
            });
        }

        _cartBindingSource.ResetBindings(false);
        RefreshTotals();
    }

    private async Task CheckoutAsync()
    {
        try
        {
            NormalizeCartQuantities();
            var order = CreateOrder();
            var savedOrder = await _cashierService.CheckoutAsync(order);

            MessageBox.Show($"结算完成。订单号：{savedOrder.OrderNo}", "结算成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearCart();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private Order CreateOrder()
    {
        var order = new Order
        {
            DiscountAmount = _discountBox.Value,
            ReceivedAmount = _receivedBox.Value,
            PaymentMethod = PaymentMethod.Cash
        };

        foreach (var item in _cart)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.Name,
                Barcode = item.Barcode,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        order.TotalAmount = order.Items.Sum(item => item.Amount);
        return order;
    }

    private void NormalizeCartQuantities()
    {
        foreach (var item in _cart)
        {
            if (item.Quantity <= 0)
            {
                item.Quantity = 1;
            }
        }

        _cartBindingSource.ResetBindings(false);
        RefreshTotals();
    }

    private void ClearCart()
    {
        _cart.Clear();
        _discountBox.Value = 0;
        _receivedBox.Value = 0;
        _cartBindingSource.ResetBindings(false);
        RefreshTotals();
        _productInputTextBox.Focus();
    }

    private void RefreshTotals()
    {
        var total = _cart.Sum(item => item.Amount);
        var discount = Math.Min(_discountBox.Value, total);
        if (_discountBox.Value != discount)
        {
            _discountBox.Value = discount;
        }

        var payable = total - discount;
        var change = Math.Max(0, _receivedBox.Value - payable);
        _totalAmountLabel.Text = total.ToString("N2");
        _payableAmountLabel.Text = payable.ToString("N2");
        _changeAmountLabel.Text = change.ToString("N2");
    }

    private static Control CreateSummaryItem(string title, Label amountLabel)
    {
        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            Margin = new Padding(18, 0, 18, 0)
        };
        panel.Controls.Add(new Label { Text = title, AutoSize = true, ForeColor = Color.FromArgb(75, 85, 99) });
        panel.Controls.Add(amountLabel);
        return panel;
    }

    private static Label CreateAmountLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39)
        };
    }

    private static Label CreateFieldLabel(string text)
    {
        return new Label { Text = text, AutoSize = true, Margin = new Padding(0, 8, 8, 0) };
    }

    private static NumericUpDown CreateMoneyBox()
    {
        return new NumericUpDown
        {
            DecimalPlaces = 2,
            Maximum = 1000000,
            Minimum = 0,
            ThousandsSeparator = true,
            Dock = DockStyle.Fill
        };
    }

    private static Button CreatePrimaryButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            BackColor = Color.FromArgb(37, 99, 235),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Margin = new Padding(8, 0, 0, 0),
            Padding = new Padding(14, 6, 14, 6),
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += onClick;
        return button;
    }

    private static Button CreateSecondaryButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            BackColor = Color.FromArgb(229, 231, 235),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(31, 41, 55),
            Margin = new Padding(8, 0, 0, 0),
            Padding = new Padding(14, 6, 14, 6),
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += onClick;
        return button;
    }

    private static void ShowError(Exception ex)
    {
        MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
