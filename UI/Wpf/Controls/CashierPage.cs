using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using MediaFontFamily = System.Windows.Media.FontFamily;
using WpfButton = System.Windows.Controls.Button;
using WpfControl = System.Windows.Controls.Control;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfUserControl = System.Windows.Controls.UserControl;
using POS_system_cs.Application.Models;
using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;
using POS_system_cs.Domain.Enums;
using POS_system_cs.UI.Wpf.Localization;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed class CashierPage : WpfUserControl
{
    private readonly IProductService _productService;
    private readonly ICashierService _cashierService;
    private readonly ObservableCollection<CashierCartItem> _cart = [];
    private readonly DataGrid _cartGrid = new();
    private readonly WpfTextBox _productInputTextBox = new();
    private readonly WpfTextBox _discountTextBox = new();
    private readonly TextBlock _totalAmountText = CreateAmountText("0.00");
    private readonly TextBlock _payableAmountText = CreateAmountText("0.00");
    private bool _checkoutInProgress;
    private bool _updatingDiscount;

    public CashierPage(IProductService productService, ICashierService cashierService)
    {
        _productService = productService;
        _cashierService = cashierService;

        Focusable = true;
        Background = new SolidColorBrush(MediaColor.FromRgb(248, 250, 252));
        Content = BuildLayout();
        RefreshTotals();

        Loaded += (_, _) => FocusProductInput();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private Grid BuildLayout()
    {
        var root = new Grid { Margin = new Thickness(22) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var inputPanel = BuildInputPanel();
        Grid.SetRow(inputPanel, 0);
        root.Children.Add(inputPanel);

        var cartPanel = BuildCartPanel();
        Grid.SetRow(cartPanel, 1);
        root.Children.Add(cartPanel);

        var checkoutPanel = BuildCheckoutPanel();
        Grid.SetRow(checkoutPanel, 2);
        root.Children.Add(checkoutPanel);

        var shortcutHint = new TextBlock
        {
            Text = Localizer.T("Cashier.Shortcuts"),
            Foreground = new SolidColorBrush(MediaColor.FromRgb(100, 116, 139)),
            FontSize = 12,
            Margin = new Thickness(2, 14, 2, 0),
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetRow(shortcutHint, 3);
        root.Children.Add(shortcutHint);

        return root;
    }

    private Border BuildInputPanel()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleBlock = new StackPanel { Margin = new Thickness(0, 0, 18, 0) };
        titleBlock.Children.Add(new TextBlock
        {
            Text = Localizer.T("Cashier.Title"),
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(MediaColor.FromRgb(15, 23, 42))
        });
        titleBlock.Children.Add(new TextBlock
        {
            Text = Localizer.T("Cashier.Desc"),
            Foreground = new SolidColorBrush(MediaColor.FromRgb(100, 116, 139)),
            Margin = new Thickness(0, 4, 0, 0)
        });
        Grid.SetColumn(titleBlock, 0);
        grid.Children.Add(titleBlock);

        _productInputTextBox.MinWidth = 300;
        _productInputTextBox.Height = 46;
        _productInputTextBox.VerticalContentAlignment = VerticalAlignment.Center;
        _productInputTextBox.FontSize = 16;
        _productInputTextBox.Padding = new Thickness(14, 0, 14, 0);
        _productInputTextBox.ToolTip = Localizer.T("Cashier.ProductInput");
        _productInputTextBox.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await AddProductFromInputAsync();
            }
        };
        Grid.SetColumn(_productInputTextBox, 1);
        grid.Children.Add(_productInputTextBox);

        var addButton = CreatePrimaryButton(Localizer.T("Cashier.Add"));
        addButton.Click += async (_, _) => await AddProductFromInputAsync();
        Grid.SetColumn(addButton, 2);
        grid.Children.Add(addButton);

        var clearButton = CreateSecondaryButton(Localizer.T("Cashier.Clear"));
        clearButton.Click += (_, _) => ClearCart();
        Grid.SetColumn(clearButton, 3);
        grid.Children.Add(clearButton);

        return CreateCard(grid, new Thickness(0, 0, 0, 16));
    }

    private Border BuildCartPanel()
    {
        _cartGrid.AutoGenerateColumns = false;
        _cartGrid.ItemsSource = _cart;
        _cartGrid.SelectionMode = DataGridSelectionMode.Single;
        _cartGrid.SelectionUnit = DataGridSelectionUnit.FullRow;
        _cartGrid.CanUserAddRows = false;
        _cartGrid.CanUserDeleteRows = false;
        _cartGrid.IsReadOnly = true;
        _cartGrid.RowHeaderWidth = 0;
        _cartGrid.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;
        _cartGrid.HeadersVisibility = DataGridHeadersVisibility.Column;
        _cartGrid.Background = MediaBrushes.White;
        _cartGrid.BorderThickness = new Thickness(0);
        _cartGrid.FontSize = 14;
        _cartGrid.RowHeight = 42;
        _cartGrid.ColumnHeaderHeight = 40;
        _cartGrid.Columns.Add(CreateTextColumn(Localizer.T("Field.Code"), nameof(CashierCartItem.Code), 120));
        _cartGrid.Columns.Add(CreateTextColumn(Localizer.T("Field.Product"), nameof(CashierCartItem.Name), 1, true));
        _cartGrid.Columns.Add(CreateTextColumn(Localizer.T("Field.Barcode"), nameof(CashierCartItem.Barcode), 160));
        _cartGrid.Columns.Add(CreateMoneyColumn(Localizer.T("Field.Price"), nameof(CashierCartItem.UnitPrice), 110));
        _cartGrid.Columns.Add(CreateMoneyColumn(Localizer.T("Field.Quantity"), nameof(CashierCartItem.Quantity), 100));
        _cartGrid.Columns.Add(CreateMoneyColumn(Localizer.T("Field.Amount"), nameof(CashierCartItem.Amount), 120));

        var panel = new DockPanel();
        panel.Children.Add(_cartGrid);
        return CreateCard(panel, new Thickness(0, 0, 0, 16));
    }

    private Border BuildCheckoutPanel()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var discountLabel = new TextBlock
        {
            Text = Localizer.T("Field.Discount"),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(MediaColor.FromRgb(71, 85, 105)),
            Margin = new Thickness(0, 0, 10, 0)
        };
        Grid.SetColumn(discountLabel, 0);
        grid.Children.Add(discountLabel);

        _discountTextBox.Height = 40;
        _discountTextBox.VerticalContentAlignment = VerticalAlignment.Center;
        _discountTextBox.TextAlignment = TextAlignment.Right;
        _discountTextBox.Text = "0.00";
        _discountTextBox.Padding = new Thickness(10, 0, 10, 0);
        _discountTextBox.TextChanged += (_, _) => RefreshTotals();
        _discountTextBox.LostFocus += (_, _) => NormalizeDiscountText();
        Grid.SetColumn(_discountTextBox, 1);
        grid.Children.Add(_discountTextBox);

        var totals = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20, 0, 20, 0)
        };
        totals.Children.Add(CreateSummaryBlock(Localizer.T("Field.Total"), _totalAmountText));
        totals.Children.Add(CreateSummaryBlock(Localizer.T("Field.Payable"), _payableAmountText));
        Grid.SetColumn(totals, 2);
        grid.Children.Add(totals);

        var checkoutButton = CreatePrimaryButton(Localizer.T("Cashier.Checkout"));
        checkoutButton.MinWidth = 132;
        checkoutButton.Height = 46;
        checkoutButton.Click += async (_, _) => await CheckoutAsync();
        Grid.SetColumn(checkoutButton, 3);
        grid.Children.Add(checkoutButton);

        return CreateCard(grid, new Thickness(0));
    }

    private async void OnPreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F2:
                e.Handled = true;
                FocusProductInput();
                break;
            case Key.F4:
                e.Handled = true;
                ClearCart();
                break;
            case Key.F6:
                e.Handled = true;
                EditSelectedQuantity();
                break;
            case Key.F9:
                e.Handled = true;
                await CheckoutAsync();
                break;
            case Key.Escape:
                e.Handled = true;
                _productInputTextBox.Clear();
                FocusProductInput();
                break;
            case Key.Delete when !_productInputTextBox.IsKeyboardFocusWithin && !_discountTextBox.IsKeyboardFocusWithin:
                e.Handled = true;
                RemoveSelectedCartItem();
                break;
        }
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
                var activeMatches = matches.Where(item => item.IsActive).ToList();
                product = activeMatches.Count == 1 ? activeMatches[0] : null;

                if (activeMatches.Count > 1)
                {
                    ShowInfo(Localizer.T("Cashier.MultipleProducts"), Localizer.T("Cashier.ProductNotUnique"));
                    return;
                }
            }

            if (product is null)
            {
                ShowInfo(Localizer.T("Cashier.ProductNotFound"), Localizer.T("Info"));
                return;
            }

            AddToCart(product);
            _productInputTextBox.Clear();
            FocusProductInput();
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

        RefreshCartView();
        RefreshTotals();
    }

    private void RemoveSelectedCartItem()
    {
        if (_cartGrid.SelectedItem is not CashierCartItem item)
        {
            return;
        }

        _cart.Remove(item);
        RefreshTotals();
    }

    private void EditSelectedQuantity()
    {
        if (_cartGrid.SelectedItem is not CashierCartItem item)
        {
            ShowInfo(Localizer.T("Cashier.SelectCartItem"), Localizer.T("Info"));
            return;
        }

        var dialog = new QuantityDialog(item.Name, item.Quantity)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        item.Quantity = dialog.Quantity;
        RefreshCartView();
        RefreshTotals();
    }

    private async Task CheckoutAsync()
    {
        if (_checkoutInProgress)
        {
            return;
        }

        try
        {
            _checkoutInProgress = true;
            NormalizeCartQuantities();

            var total = _cart.Sum(item => item.Amount);
            if (total <= 0 || _cart.Count == 0)
            {
                ShowInfo(Localizer.T("Cashier.EmptyCart"), Localizer.T("Info"));
                return;
            }

            var discount = GetDiscountAmount();
            var payable = total - discount;
            var dialog = new PaymentDialog(payable, discount)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true || dialog.Result is null)
            {
                return;
            }

            var order = CreateOrder(dialog.Result.Value.ReceivedAmount, dialog.Result.Value.PaymentMethod, discount);
            var savedOrder = await _cashierService.CheckoutAsync(order);

            System.Windows.MessageBox.Show(Window.GetWindow(this), Localizer.Format("Cashier.Success", savedOrder.OrderNo), Localizer.T("Cashier.SuccessTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            ClearCart();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            _checkoutInProgress = false;
        }
    }

    private Order CreateOrder(decimal receivedAmount, PaymentMethod paymentMethod, decimal discount)
    {
        var order = new Order
        {
            DiscountAmount = discount,
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

        RefreshCartView();
        RefreshTotals();
    }

    private void ClearCart()
    {
        _cart.Clear();
        SetDiscountText(0);
        RefreshTotals();
        FocusProductInput();
    }

    private void RefreshTotals()
    {
        if (_updatingDiscount)
        {
            return;
        }

        var total = _cart.Sum(item => item.Amount);
        var discount = Math.Min(GetDecimal(_discountTextBox.Text), total);
        var payable = total - discount;
        _totalAmountText.Text = total.ToString("N2");
        _payableAmountText.Text = payable.ToString("N2");
    }

    private decimal GetDiscountAmount()
    {
        var total = _cart.Sum(item => item.Amount);
        var discount = Math.Min(GetDecimal(_discountTextBox.Text), total);
        if (GetDecimal(_discountTextBox.Text) != discount)
        {
            SetDiscountText(discount);
        }

        return discount;
    }

    private void NormalizeDiscountText()
    {
        SetDiscountText(GetDiscountAmount());
    }

    private void SetDiscountText(decimal amount)
    {
        _updatingDiscount = true;
        _discountTextBox.Text = amount.ToString("N2");
        _updatingDiscount = false;
    }

    private void RefreshCartView()
    {
        CollectionViewSource.GetDefaultView(_cartGrid.ItemsSource)?.Refresh();
    }

    private void FocusProductInput()
    {
        _productInputTextBox.Focus();
        _productInputTextBox.SelectAll();
    }

    private static DataGridTextColumn CreateTextColumn(string header, string path, double width, bool star = false)
    {
        return new DataGridTextColumn
        {
            Header = header,
            Binding = new System.Windows.Data.Binding(path),
            Width = star ? new DataGridLength(width, DataGridLengthUnitType.Star) : new DataGridLength(width)
        };
    }

    private static DataGridTextColumn CreateMoneyColumn(string header, string path, double width)
    {
        return new DataGridTextColumn
        {
            Header = header,
            Binding = new System.Windows.Data.Binding(path) { StringFormat = "N2" },
            Width = new DataGridLength(width),
            ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right),
                    new Setter(TextBlock.PaddingProperty, new Thickness(0, 0, 12, 0))
                }
            }
        };
    }

    private static Border CreateCard(UIElement child, Thickness margin)
    {
        return new Border
        {
            Margin = margin,
            Padding = new Thickness(18),
            Background = MediaBrushes.White,
            BorderBrush = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Child = child
        };
    }

    private static WpfButton CreatePrimaryButton(string text)
    {
        return new WpfButton
        {
            Content = text,
            Height = 42,
            MinWidth = 104,
            Margin = new Thickness(10, 0, 0, 0),
            Padding = new Thickness(18, 0, 18, 0),
            BorderThickness = new Thickness(0),
            Background = new SolidColorBrush(MediaColor.FromRgb(37, 99, 235)),
            Foreground = MediaBrushes.White,
            FontWeight = FontWeights.SemiBold,
            Cursor = System.Windows.Input.Cursors.Hand
        };
    }

    private static WpfButton CreateSecondaryButton(string text)
    {
        return new WpfButton
        {
            Content = text,
            Height = 42,
            MinWidth = 104,
            Margin = new Thickness(10, 0, 0, 0),
            Padding = new Thickness(18, 0, 18, 0),
            BorderBrush = new SolidColorBrush(MediaColor.FromRgb(203, 213, 225)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(MediaColor.FromRgb(241, 245, 249)),
            Foreground = new SolidColorBrush(MediaColor.FromRgb(30, 41, 59)),
            Cursor = System.Windows.Input.Cursors.Hand
        };
    }

    private static StackPanel CreateSummaryBlock(string title, TextBlock amountText)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(20, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = new SolidColorBrush(MediaColor.FromRgb(100, 116, 139)),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        });
        panel.Children.Add(amountText);
        return panel;
    }

    private static TextBlock CreateAmountText(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(MediaColor.FromRgb(15, 23, 42)),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };
    }

    private static decimal GetDecimal(string? text)
    {
        text = text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value)
            && !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
        {
            return 0;
        }

        return Math.Max(0, value);
    }

    private void ShowInfo(string message, string title)
    {
        System.Windows.MessageBox.Show(Window.GetWindow(this), message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShowError(Exception ex)
    {
        System.Windows.MessageBox.Show(Window.GetWindow(this), ex.Message, Localizer.T("OperationFailed"), MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private sealed class QuantityDialog : Window
    {
        private readonly WpfTextBox _quantityBox = new();

        public QuantityDialog(string productName, decimal currentQuantity)
        {
            Title = Localizer.T("Cashier.QuantityTitle");
            Width = 380;
            Height = 210;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = MediaBrushes.White;
            FontFamily = new MediaFontFamily("Microsoft YaHei UI");

            var root = new Grid { Margin = new Thickness(18) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            root.Children.Add(new TextBlock
            {
                Text = Localizer.Format("Cashier.ProductPrefix", productName),
                FontSize = 15,
                Foreground = new SolidColorBrush(MediaColor.FromRgb(15, 23, 42)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            _quantityBox.Text = currentQuantity.ToString("N2");
            _quantityBox.Height = 40;
            _quantityBox.FontSize = 16;
            _quantityBox.TextAlignment = TextAlignment.Right;
            _quantityBox.VerticalContentAlignment = VerticalAlignment.Center;
            _quantityBox.Margin = new Thickness(0, 16, 0, 16);
            Grid.SetRow(_quantityBox, 1);
            root.Children.Add(_quantityBox);

            var buttons = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };
            var okButton = CreateDialogPrimaryButton(Localizer.T("Action.Apply"));
            okButton.IsDefault = true;
            okButton.Click += (_, _) => Apply();
            var cancelButton = CreateDialogSecondaryButton(Localizer.T("Cashier.Cancel"));
            cancelButton.IsCancel = true;
            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);
            Grid.SetRow(buttons, 2);
            root.Children.Add(buttons);

            Content = root;
            Loaded += (_, _) =>
            {
                _quantityBox.Focus();
                _quantityBox.SelectAll();
            };
        }

        public decimal Quantity { get; private set; }

        private void Apply()
        {
            var quantity = GetDecimal(_quantityBox.Text);
            if (quantity <= 0)
            {
                System.Windows.MessageBox.Show(this, Localizer.T("Cashier.QuantityPositive"), Localizer.T("Info"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Quantity = Math.Min(quantity, 1_000_000M);
            DialogResult = true;
        }
    }

    private sealed class PaymentDialog : Window
    {
        private readonly WpfTextBox _payableBox = CreateReadonlyTextBox();
        private readonly WpfTextBox _discountBox = CreateReadonlyTextBox();
        private readonly WpfTextBox _receivedBox = new();
        private readonly WpfTextBox _changeBox = CreateReadonlyTextBox();
        private readonly TextBlock _statusText = new();
        private decimal _remainingAmount;
        private decimal _cashAmount;
        private decimal _onlineAmount;
        private decimal _receivedTotal;

        public PaymentDialog(decimal payableAmount, decimal discountAmount)
        {
            _remainingAmount = payableAmount;

            Title = Localizer.T("Cashier.PaymentTitle");
            Width = 480;
            Height = 340;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = MediaBrushes.White;
            FontFamily = new MediaFontFamily("Microsoft YaHei UI");
            KeyDown += OnDialogKeyDown;

            _discountBox.Text = discountAmount.ToString("N2");
            _receivedBox.Height = 38;
            _receivedBox.FontSize = 15;
            _receivedBox.TextAlignment = TextAlignment.Right;
            _receivedBox.VerticalContentAlignment = VerticalAlignment.Center;
            _receivedBox.TextChanged += (_, _) => RefreshPaymentView();

            Content = BuildLayout();
            RefreshPaymentView();

            Loaded += (_, _) =>
            {
                _receivedBox.Focus();
                _receivedBox.SelectAll();
            };
        }

        public PaymentResult? Result { get; private set; }

        private Grid BuildLayout()
        {
            var root = new Grid { Margin = new Thickness(20) };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(82) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (var i = 0; i < 7; i++)
            {
                root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            AddPaymentRow(root, 0, Localizer.T("Field.Payable"), _payableBox);
            AddPaymentRow(root, 1, Localizer.T("Field.Discount"), _discountBox);
            AddPaymentRow(root, 2, Localizer.T("Field.Received"), _receivedBox);
            AddPaymentRow(root, 3, Localizer.T("Field.Change"), _changeBox);

            _statusText.Foreground = new SolidColorBrush(MediaColor.FromRgb(30, 41, 59));
            _statusText.Margin = new Thickness(0, 8, 0, 0);
            Grid.SetColumn(_statusText, 1);
            Grid.SetRow(_statusText, 4);
            root.Children.Add(_statusText);

            var hint = new TextBlock
            {
                Text = Localizer.T("Cashier.PaymentHint"),
                Foreground = new SolidColorBrush(MediaColor.FromRgb(100, 116, 139)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 14)
            };
            Grid.SetColumn(hint, 1);
            Grid.SetRow(hint, 5);
            root.Children.Add(hint);

            var buttons = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };
            var completeButton = CreateDialogPrimaryButton(Localizer.T("Cashier.Complete"));
            completeButton.IsDefault = true;
            completeButton.Click += (_, _) => Complete();
            var onlineButton = CreateDialogSecondaryButton(Localizer.T("Cashier.Online"));
            onlineButton.Click += (_, _) => RecordPayment(PaymentMethod.Online);
            var cashButton = CreateDialogSecondaryButton(Localizer.T("Cashier.Cash"));
            cashButton.Click += (_, _) => RecordPayment(PaymentMethod.Cash);
            var cancelButton = CreateDialogSecondaryButton(Localizer.T("Cashier.Cancel"));
            cancelButton.IsCancel = true;
            buttons.Children.Add(completeButton);
            buttons.Children.Add(onlineButton);
            buttons.Children.Add(cashButton);
            buttons.Children.Add(cancelButton);
            Grid.SetColumn(buttons, 1);
            Grid.SetRow(buttons, 6);
            root.Children.Add(buttons);

            return root;
        }

        private void OnDialogKeyDown(object sender, WpfKeyEventArgs e)
        {
            if (e.Key == Key.F7)
            {
                e.Handled = true;
                RecordPayment(PaymentMethod.Cash);
            }
            else if (e.Key == Key.F8)
            {
                e.Handled = true;
                RecordPayment(PaymentMethod.Online);
            }
        }

        private void RecordPayment(PaymentMethod method)
        {
            if (_remainingAmount <= 0)
            {
                RefreshPaymentView();
                return;
            }

            var inputAmount = GetDecimal(_receivedBox.Text);
            if (inputAmount <= 0)
            {
                inputAmount = _remainingAmount;
            }

            var appliedAmount = Math.Min(inputAmount, _remainingAmount);
            var overpaidAmount = Math.Max(0, inputAmount - _remainingAmount);
            if (method == PaymentMethod.Online)
            {
                _onlineAmount += appliedAmount;
            }
            else
            {
                _cashAmount += appliedAmount + overpaidAmount;
            }

            _receivedTotal = _cashAmount + _onlineAmount;
            _remainingAmount = Math.Max(0, _remainingAmount - appliedAmount);
            _receivedBox.Clear();
            RefreshPaymentView();
            _receivedBox.Focus();
        }

        private void Complete()
        {
            if (_remainingAmount > 0)
            {
                System.Windows.MessageBox.Show(this, Localizer.T("Cashier.RemainingDue"), Localizer.T("Info"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Result = new PaymentResult(_receivedTotal, GetPaymentMethod(_cashAmount, _onlineAmount));
            DialogResult = true;
        }

        private void RefreshPaymentView()
        {
            var inputAmount = GetDecimal(_receivedBox.Text);
            var change = Math.Max(0, inputAmount - _remainingAmount);
            _payableBox.Text = _remainingAmount.ToString("N2");
            _changeBox.Text = change.ToString("N2");
            _statusText.Text = Localizer.Format("Cashier.PaymentStatus", _receivedTotal, _cashAmount, _onlineAmount);
        }

        private static void AddPaymentRow(Grid root, int row, string label, WpfControl editor)
        {
            var labelBlock = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(MediaColor.FromRgb(71, 85, 105)),
                Margin = new Thickness(0, 0, 12, 10)
            };
            Grid.SetColumn(labelBlock, 0);
            Grid.SetRow(labelBlock, row);
            root.Children.Add(labelBlock);

            editor.Margin = new Thickness(0, 0, 0, 10);
            Grid.SetColumn(editor, 1);
            Grid.SetRow(editor, row);
            root.Children.Add(editor);
        }

        private static WpfTextBox CreateReadonlyTextBox()
        {
            return new WpfTextBox
            {
                Height = 38,
                FontSize = 15,
                TextAlignment = TextAlignment.Right,
                VerticalContentAlignment = VerticalAlignment.Center,
                IsReadOnly = true,
                Background = new SolidColorBrush(MediaColor.FromRgb(248, 250, 252)),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(203, 213, 225))
            };
        }
    }

    private static WpfButton CreateDialogPrimaryButton(string text)
    {
        return new WpfButton
        {
            Content = text,
            MinWidth = 90,
            Height = 34,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(12, 0, 12, 0),
            BorderThickness = new Thickness(0),
            Background = new SolidColorBrush(MediaColor.FromRgb(37, 99, 235)),
            Foreground = MediaBrushes.White,
            Cursor = System.Windows.Input.Cursors.Hand
        };
    }

    private static WpfButton CreateDialogSecondaryButton(string text)
    {
        return new WpfButton
        {
            Content = text,
            MinWidth = 82,
            Height = 34,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(12, 0, 12, 0),
            BorderBrush = new SolidColorBrush(MediaColor.FromRgb(203, 213, 225)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(MediaColor.FromRgb(241, 245, 249)),
            Foreground = new SolidColorBrush(MediaColor.FromRgb(30, 41, 59)),
            Cursor = System.Windows.Input.Cursors.Hand
        };
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

    private readonly record struct PaymentResult(decimal ReceivedAmount, PaymentMethod PaymentMethod);
}




