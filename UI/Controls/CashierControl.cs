using System.ComponentModel;
using System.Globalization;
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
    private readonly Label _totalAmountLabel = CreateAmountLabel("0.00");
    private readonly Label _payableAmountLabel = CreateAmountLabel("0.00");

    public CashierControl(IProductService productService, ICashierService cashierService)
    {
        _productService = productService;
        _cashierService = cashierService;
        Dock = DockStyle.Fill;
        BuildLayout();
        RefreshTotals();
        Load += (_, _) => BeginInvoke(() => _productInputTextBox.Focus());
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.F2:
                _productInputTextBox.Focus();
                _productInputTextBox.SelectAll();
                return true;
            case Keys.F4:
                ClearCart();
                return true;
            case Keys.F6:
                EditSelectedQuantity();
                return true;
            case Keys.F9:
                _ = CheckoutAsync();
                return true;
            case Keys.Escape:
                _productInputTextBox.Clear();
                _productInputTextBox.Focus();
                return true;
            case Keys.Delete when !_cartGrid.IsCurrentCellInEditMode:
                RemoveSelectedCartItem();
                return true;
            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.FromArgb(247, 249, 252)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildInputPanel(), 0, 0);
        root.Controls.Add(BuildCartGrid(), 0, 1);
        root.Controls.Add(BuildCheckoutPanel(), 0, 2);
        root.Controls.Add(BuildShortcutHint(), 0, 3);
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
            Text = "商品编码 / 条码 / 名称  (F2)",
            AutoSize = true,
            Margin = new Padding(0, 8, 10, 0)
        };

        _productInputTextBox.Dock = DockStyle.Fill;
        _productInputTextBox.Font = new Font("Microsoft YaHei UI", 12F);
        _productInputTextBox.PlaceholderText = "输入后按 Enter 加入购物车";
        _productInputTextBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await AddProductFromInputAsync();
            }
        };

        var addButton = CreatePrimaryButton("加入购物车 Enter", async (_, _) => await AddProductFromInputAsync());
        var clearButton = CreateSecondaryButton("清空购物车 F4", (_, _) => ClearCart());

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
            AutoScroll = true,
            BackColor = Color.White,
            Padding = new Padding(1)
        };
        container.Controls.Add(_cartGrid);
        return container;
    }

    private Control BuildCheckoutPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 14, 0, 0),
            WrapContents = true
        };

        _discountBox.ValueChanged += (_, _) => RefreshTotals();
        _discountBox.Width = 120;

        var totals = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        totals.Controls.Add(CreateSummaryItem("合计", _totalAmountLabel));
        totals.Controls.Add(CreateSummaryItem("应收", _payableAmountLabel));

        var checkoutButton = CreatePrimaryButton("收款 F9", async (_, _) => await CheckoutAsync());
        checkoutButton.Width = 130;
        checkoutButton.Height = 44;

        panel.Controls.Add(CreateFieldLabel("优惠"));
        panel.Controls.Add(_discountBox);
        panel.Controls.Add(totals);
        panel.Controls.Add(checkoutButton);
        return panel;
    }

    private static Control BuildShortcutHint()
    {
        return new Label
        {
            AutoSize = true,
            Dock = DockStyle.Bottom,
            ForeColor = Color.FromArgb(107, 114, 128),
            Padding = new Padding(0, 10, 0, 0),
            Text = "快捷键：F2 聚焦输入 / Enter 加入商品 / F6 修改数量 / Delete 移除选中商品 / F4 清空购物车 / F9 收款 / Esc 清空输入"
        };
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

    private void RemoveSelectedCartItem()
    {
        if (_cartGrid.CurrentRow?.DataBoundItem is not CashierCartItem item)
        {
            return;
        }

        _cart.Remove(item);
        _cartBindingSource.ResetBindings(false);
        RefreshTotals();
    }

    private void EditSelectedQuantity()
    {
        if (_cartGrid.CurrentRow?.DataBoundItem is not CashierCartItem item)
        {
            MessageBox.Show("请先选择购物车中的商品。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new Form
        {
            Text = "修改数量",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(300, 132),
            Font = Font
        };

        var label = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 36,
            Padding = new Padding(14, 10, 14, 0),
            Text = $"商品：{item.Name}"
        };

        var quantityBox = new NumericUpDown
        {
            DecimalPlaces = 2,
            Minimum = 0.01M,
            Maximum = 1000000,
            Value = Math.Clamp(item.Quantity, 0.01M, 1000000),
            Width = 260,
            Location = new Point(14, 46),
            ThousandsSeparator = true
        };

        var okButton = new Button
        {
            Text = "应用 Enter",
            DialogResult = DialogResult.OK,
            Width = 110,
            Height = 32,
            Location = new Point(68, 88)
        };

        var cancelButton = new Button
        {
            Text = "取消 Esc",
            DialogResult = DialogResult.Cancel,
            Width = 90,
            Height = 32,
            Location = new Point(190, 88)
        };

        dialog.Controls.Add(label);
        dialog.Controls.Add(quantityBox);
        dialog.Controls.Add(okButton);
        dialog.Controls.Add(cancelButton);
        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;
        dialog.Shown += (_, _) =>
        {
            quantityBox.Focus();
            quantityBox.Select(0, quantityBox.Text.Length);
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        item.Quantity = quantityBox.Value;
        _cartBindingSource.ResetBindings(false);
        RefreshTotals();
    }

    private async Task CheckoutAsync()
    {
        try
        {
            NormalizeCartQuantities();
            var total = _cart.Sum(item => item.Amount);
            var discount = Math.Min(_discountBox.Value, total);
            var payable = total - discount;
            var payment = ShowPaymentDialog(payable);
            if (payment is null)
            {
                return;
            }

            var order = CreateOrder(payment.Value.ReceivedAmount, payment.Value.PaymentMethod);
            var savedOrder = await _cashierService.CheckoutAsync(order);

            MessageBox.Show($"结算完成。订单号：{savedOrder.OrderNo}", "结算成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearCart();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private Order CreateOrder(decimal receivedAmount, PaymentMethod paymentMethod)
    {
        var order = new Order
        {
            DiscountAmount = _discountBox.Value,
            ReceivedAmount = receivedAmount,
            PaymentMethod = paymentMethod
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
        _totalAmountLabel.Text = total.ToString("N2");
        _payableAmountLabel.Text = payable.ToString("N2");
    }

    private PaymentResult? ShowPaymentDialog(decimal payableAmount)
    {
        using var dialog = new Form
        {
            Text = "收款",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            KeyPreview = true,
            ClientSize = new Size(430, 300),
            Font = Font
        };

        var remainingAmount = payableAmount;
        var cashAmount = 0M;
        var onlineAmount = 0M;
        var receivedTotal = 0M;

        var payableTextBox = CreateReadonlyPaymentTextBox(remainingAmount);
        var discountTextBox = CreateReadonlyPaymentTextBox(_discountBox.Value);
        var receivedBox = CreatePaymentAmountBox();
        var changeTextBox = CreateReadonlyPaymentTextBox(0);
        var methodLabel = new Label
        {
            AutoSize = false,
            Height = 24,
            Text = "已收：0.00",
            ForeColor = Color.FromArgb(31, 41, 55)
        };

        void RefreshPaymentView()
        {
            var inputAmount = GetPaymentBoxAmount(receivedBox);
            var change = Math.Max(0, inputAmount - remainingAmount);
            payableTextBox.Text = remainingAmount.ToString("N2");
            changeTextBox.Text = change.ToString("N2");
            methodLabel.Text = $"已收：{receivedTotal:N2}    现金：{cashAmount:N2}    线上：{onlineAmount:N2}";
        }

        void RecordPayment(PaymentMethod method)
        {
            if (remainingAmount <= 0)
            {
                RefreshPaymentView();
                return;
            }

            var inputAmount = GetPaymentBoxAmount(receivedBox);
            if (inputAmount <= 0)
            {
                inputAmount = remainingAmount;
            }

            var appliedAmount = Math.Min(inputAmount, remainingAmount);
            var overpaidAmount = Math.Max(0, inputAmount - remainingAmount);
            if (method == PaymentMethod.Online)
            {
                onlineAmount += appliedAmount;
            }
            else
            {
                cashAmount += appliedAmount + overpaidAmount;
            }

            receivedTotal = cashAmount + onlineAmount;
            remainingAmount = Math.Max(0, remainingAmount - appliedAmount);
            SetPaymentBoxAmount(receivedBox, 0);
            RefreshPaymentView();
            receivedBox.Focus();
            receivedBox.Select(0, receivedBox.Text.Length);
        }

        receivedBox.ValueChanged += (_, _) => RefreshPaymentView();
        receivedBox.TextChanged += (_, _) => RefreshPaymentView();

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(16)
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        form.Controls.Add(CreatePaymentLabel("应收"), 0, 0);
        form.Controls.Add(payableTextBox, 1, 0);
        form.Controls.Add(CreatePaymentLabel("优惠"), 0, 1);
        form.Controls.Add(discountTextBox, 1, 1);
        form.Controls.Add(CreatePaymentLabel("实收"), 0, 2);
        form.Controls.Add(receivedBox, 1, 2);
        form.Controls.Add(CreatePaymentLabel("找零"), 0, 3);
        form.Controls.Add(changeTextBox, 1, 3);
        form.Controls.Add(methodLabel, 1, 4);
        form.Controls.Add(new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(107, 114, 128),
            Text = "快捷键：Enter 完成 / F7 现金 / F8 线上 / Esc 取消。未输入金额时默认收完剩余应收。"
        }, 1, 5);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill
        };
        var okButton = new Button { Text = "完成 Enter", DialogResult = DialogResult.OK, Width = 100, Height = 32 };
        var onlineButton = new Button { Text = "线上 F8", Width = 90, Height = 32 };
        var cashButton = new Button { Text = "现金 F7", Width = 90, Height = 32 };
        var cancelButton = new Button { Text = "取消 Esc", DialogResult = DialogResult.Cancel, Width = 90, Height = 32 };
        onlineButton.Click += (_, _) => RecordPayment(PaymentMethod.Online);
        cashButton.Click += (_, _) => RecordPayment(PaymentMethod.Cash);
        buttons.Controls.Add(okButton);
        buttons.Controls.Add(onlineButton);
        buttons.Controls.Add(cashButton);
        buttons.Controls.Add(cancelButton);
        form.Controls.Add(buttons, 1, 6);

        dialog.Controls.Add(form);
        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;
        dialog.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.F7)
            {
                e.SuppressKeyPress = true;
                RecordPayment(PaymentMethod.Cash);
            }
            else if (e.KeyCode == Keys.F8)
            {
                e.SuppressKeyPress = true;
                RecordPayment(PaymentMethod.Online);
            }
        };
        dialog.Shown += (_, _) =>
        {
            receivedBox.Focus();
            receivedBox.Select(0, receivedBox.Text.Length);
        };

        RefreshPaymentView();

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return null;
        }

        if (remainingAmount > 0)
        {
            MessageBox.Show("还有未收金额，请继续收款。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        return new PaymentResult(receivedTotal, GetPaymentMethod(cashAmount, onlineAmount));
    }
    private static decimal GetPaymentBoxAmount(NumericUpDown box)
    {
        var text = box.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value)
            && !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
        {
            return box.Value;
        }

        return Math.Clamp(value, box.Minimum, box.Maximum);
    }

    private static void SetPaymentBoxAmount(NumericUpDown box, decimal amount)
    {
        box.Value = Math.Clamp(amount, box.Minimum, box.Maximum);
        box.Text = box.Value.ToString("N2");
    }
    private static PaymentMethod GetPaymentMethod(decimal cashAmount, decimal onlineAmount)
    {
        return (cashAmount > 0, onlineAmount > 0) switch
        {
            (true, true) => PaymentMethod.Mixed,
            (false, true) => PaymentMethod.Online,
            _ => PaymentMethod.Cash
        };
    }

    private static string GetPaymentMethodText(PaymentMethod paymentMethod)
    {
        return paymentMethod switch
        {
            PaymentMethod.Online => "线上支付",
            PaymentMethod.Mixed => "混合支付",
            _ => "现金"
        };
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

    private static Label CreatePaymentLabel(string text)
    {
        return new Label { Text = text, AutoSize = true, Margin = new Padding(0, 7, 8, 8) };
    }

    private static TextBox CreateReadonlyPaymentTextBox(decimal value)
    {
        return new TextBox
        {
            Text = value.ToString("N2"),
            ReadOnly = true,
            Dock = DockStyle.Fill,
            TextAlign = HorizontalAlignment.Right
        };
    }

    private static NumericUpDown CreatePaymentAmountBox()
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

    private readonly record struct PaymentResult(decimal ReceivedAmount, PaymentMethod PaymentMethod);
}



