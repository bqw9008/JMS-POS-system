using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;
using POS_system_cs.Domain.Enums;

namespace POS_system_cs.UI.Controls;

public sealed class SalesRecordControl : UserControl
{
    private readonly IOrderService _orderService;
    private readonly DataGridView _orderGrid = new();
    private readonly DataGridView _itemGrid = new();
    private readonly DateTimePicker _fromPicker = new();
    private readonly DateTimePicker _toPicker = new();
    private readonly CheckBox _useFromCheckBox = new() { Text = "开始日期", Checked = true, AutoSize = true };
    private readonly CheckBox _useToCheckBox = new() { Text = "结束日期", Checked = true, AutoSize = true };
    private readonly Label _summaryLabel = new();
    private IReadOnlyList<Order> _orders = [];

    public SalesRecordControl(IOrderService orderService)
    {
        _orderService = orderService;
        Dock = DockStyle.Fill;
        BuildLayout();
        _ = LoadOrdersAsync();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = Color.FromArgb(247, 249, 252)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildToolbar(), 0, 0);
        root.Controls.Add(BuildContent(), 0, 1);
        root.Controls.Add(BuildSummary(), 0, 2);
        Controls.Add(root);
    }

    private Control BuildToolbar()
    {
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 12),
            WrapContents = true
        };

        _fromPicker.Format = DateTimePickerFormat.Short;
        _fromPicker.Value = DateTime.Today;
        _toPicker.Format = DateTimePickerFormat.Short;
        _toPicker.Value = DateTime.Today;

        _useFromCheckBox.CheckedChanged += (_, _) => _fromPicker.Enabled = _useFromCheckBox.Checked;
        _useToCheckBox.CheckedChanged += (_, _) => _toPicker.Enabled = _useToCheckBox.Checked;

        toolbar.Controls.Add(_useFromCheckBox);
        toolbar.Controls.Add(_fromPicker);
        toolbar.Controls.Add(_useToCheckBox);
        toolbar.Controls.Add(_toPicker);
        toolbar.Controls.Add(CreatePrimaryButton("查询", async (_, _) => await LoadOrdersAsync()));
        toolbar.Controls.Add(CreateSecondaryButton("今天", async (_, _) => await SetTodayAsync()));
        toolbar.Controls.Add(CreateSecondaryButton("全部", async (_, _) => await SetAllAsync()));
        return toolbar;
    }

    private Control BuildContent()
    {
        ConfigureOrderGrid();
        ConfigureItemGrid();

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        content.Controls.Add(WrapWithTitle("销售单", _orderGrid), 0, 0);
        content.Controls.Add(WrapWithTitle("订单明细", _itemGrid), 0, 1);
        return content;
    }

    private Control BuildSummary()
    {
        _summaryLabel.Dock = DockStyle.Fill;
        _summaryLabel.AutoSize = false;
        _summaryLabel.MinimumSize = new Size(0, 42);
        _summaryLabel.TextAlign = ContentAlignment.MiddleRight;
        _summaryLabel.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
        _summaryLabel.ForeColor = Color.FromArgb(31, 41, 55);
        return _summaryLabel;
    }

    private void ConfigureOrderGrid()
    {
        _orderGrid.Dock = DockStyle.Fill;
        _orderGrid.AutoGenerateColumns = false;
        _orderGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _orderGrid.MultiSelect = false;
        _orderGrid.ReadOnly = true;
        _orderGrid.AllowUserToAddRows = false;
        _orderGrid.RowHeadersVisible = false;
        _orderGrid.BackgroundColor = Color.White;
        _orderGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "订单号", DataPropertyName = nameof(Order.OrderNo), Width = 180 });
        _orderGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "时间", DataPropertyName = nameof(Order.OrderedAt), Width = 160, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" } });
        _orderGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "合计", DataPropertyName = nameof(Order.TotalAmount), Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _orderGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "优惠", DataPropertyName = nameof(Order.DiscountAmount), Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _orderGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "实收", DataPropertyName = nameof(Order.ReceivedAmount), Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _orderGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "找零", DataPropertyName = nameof(Order.ChangeAmount), Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _orderGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "支付方式", DataPropertyName = nameof(Order.PaymentMethod), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _orderGrid.SelectionChanged += async (_, _) => await LoadSelectedOrderItemsAsync();
    }

    private void ConfigureItemGrid()
    {
        _itemGrid.Dock = DockStyle.Fill;
        _itemGrid.AutoGenerateColumns = false;
        _itemGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _itemGrid.MultiSelect = false;
        _itemGrid.ReadOnly = true;
        _itemGrid.AllowUserToAddRows = false;
        _itemGrid.RowHeadersVisible = false;
        _itemGrid.BackgroundColor = Color.White;
        _itemGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "商品", DataPropertyName = nameof(OrderItem.ProductName), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _itemGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "条码", DataPropertyName = nameof(OrderItem.Barcode), Width = 160 });
        _itemGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "数量", DataPropertyName = nameof(OrderItem.Quantity), Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _itemGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "单价", DataPropertyName = nameof(OrderItem.UnitPrice), Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _itemGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "金额", DataPropertyName = nameof(OrderItem.Amount), Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
    }

    private async Task LoadOrdersAsync()
    {
        try
        {
            var from = _useFromCheckBox.Checked ? DateOnly.FromDateTime(_fromPicker.Value.Date) : (DateOnly?)null;
            var to = _useToCheckBox.Checked ? DateOnly.FromDateTime(_toPicker.Value.Date) : (DateOnly?)null;

            if (from is not null && to is not null && from > to)
            {
                MessageBox.Show("开始日期不能晚于结束日期。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _orders = await _orderService.SearchAsync(from, to);
            _orderGrid.DataSource = _orders.ToList();
            UpdateSummary();

            if (_orders.Count == 0)
            {
                _itemGrid.DataSource = null;
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task LoadSelectedOrderItemsAsync()
    {
        if (_orderGrid.CurrentRow?.DataBoundItem is not Order order)
        {
            return;
        }

        try
        {
            _itemGrid.DataSource = (await _orderService.GetItemsAsync(order.Id)).ToList();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task SetTodayAsync()
    {
        _useFromCheckBox.Checked = true;
        _useToCheckBox.Checked = true;
        _fromPicker.Value = DateTime.Today;
        _toPicker.Value = DateTime.Today;
        await LoadOrdersAsync();
    }

    private async Task SetAllAsync()
    {
        _useFromCheckBox.Checked = false;
        _useToCheckBox.Checked = false;
        await LoadOrdersAsync();
    }

    private void UpdateSummary()
    {
        var orderCount = _orders.Count;
        var totalAmount = _orders.Sum(order => order.TotalAmount);
        var discountAmount = _orders.Sum(order => order.DiscountAmount);
        var receivedAmount = _orders.Sum(order => order.ReceivedAmount);
        var payableAmount = totalAmount - discountAmount;
        _summaryLabel.Text = $"订单数：{orderCount}    商品合计：{totalAmount:N2}    优惠：{discountAmount:N2}    应收：{payableAmount:N2}    实收：{receivedAmount:N2}";
    }

    private static Control WrapWithTitle(string title, Control content)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Color.White,
            Padding = new Padding(1)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 41, 55),
            Padding = new Padding(8, 8, 0, 8)
        }, 0, 0);
        panel.Controls.Add(content, 0, 1);
        return panel;
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
            Margin = new Padding(10, 0, 0, 0),
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
