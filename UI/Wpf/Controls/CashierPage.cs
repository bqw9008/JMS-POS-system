using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
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

public sealed partial class CashierPage : WpfUserControl
{
    private readonly IProductService _productService;
    private readonly ICashierService _cashierService;
    private readonly ObservableCollection<CashierCartItem> _cart = [];
    private bool _checkoutInProgress;
    private bool _updatingDiscount;

    public CashierPage(IProductService productService, ICashierService cashierService)
    {
        _productService = productService;
        _cashierService = cashierService;

        InitializeComponent();
        ApplyLocalization();
        ConfigureControls();
        RefreshTotals();

        Loaded += (_, _) => FocusProductInput();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void ApplyLocalization()
    {
        TitleText.Text = Localizer.T("Cashier.Title");
        DescriptionText.Text = Localizer.T("Cashier.Desc");
        ProductInputTextBox.ToolTip = Localizer.T("Cashier.ProductInput");
        AddButton.Content = Localizer.T("Cashier.Add");
        ClearButton.Content = Localizer.T("Cashier.Clear");
        DiscountLabel.Text = Localizer.T("Field.Discount");
        TotalLabel.Text = Localizer.T("Field.Total");
        PayableLabel.Text = Localizer.T("Field.Payable");
        CheckoutButton.Content = Localizer.T("Cashier.Checkout");
        ShortcutHintText.Text = Localizer.T("Cashier.Shortcuts");
    }

    private void ConfigureControls()
    {
        ProductInputTextBox.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await AddProductFromInputAsync();
            }
        };
        DiscountTextBox.TextChanged += (_, _) => RefreshTotals();
        DiscountTextBox.LostFocus += (_, _) => NormalizeDiscountText();
        ConfigureCartGrid();
    }

    private void ConfigureCartGrid()
    {
        CartGrid.ItemsSource = _cart;
        CartGrid.Columns.Add(CreateTextColumn(Localizer.T("Field.Code"), nameof(CashierCartItem.Code), 120));
        CartGrid.Columns.Add(CreateTextColumn(Localizer.T("Field.Product"), nameof(CashierCartItem.Name), 1, true));
        CartGrid.Columns.Add(CreateTextColumn(Localizer.T("Field.Barcode"), nameof(CashierCartItem.Barcode), 160));
        CartGrid.Columns.Add(CreateMoneyColumn(Localizer.T("Field.Price"), nameof(CashierCartItem.UnitPrice), 110));
        CartGrid.Columns.Add(CreateMoneyColumn(Localizer.T("Field.Quantity"), nameof(CashierCartItem.Quantity), 100));
        CartGrid.Columns.Add(CreateMoneyColumn(Localizer.T("Field.Amount"), nameof(CashierCartItem.Amount), 120));
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
                ProductInputTextBox.Clear();
                FocusProductInput();
                break;
            case Key.Delete when !ProductInputTextBox.IsKeyboardFocusWithin && !DiscountTextBox.IsKeyboardFocusWithin:
                e.Handled = true;
                RemoveSelectedCartItem();
                break;
        }
    }

    private async Task AddProductFromInputAsync()
    {
        var input = ProductInputTextBox.Text.Trim();
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
            ProductInputTextBox.Clear();
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
        if (CartGrid.SelectedItem is not CashierCartItem item)
        {
            return;
        }

        _cart.Remove(item);
        RefreshTotals();
    }

    private void EditSelectedQuantity()
    {
        if (CartGrid.SelectedItem is not CashierCartItem item)
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
        var discount = Math.Min(GetDecimal(DiscountTextBox.Text), total);
        var payable = total - discount;
        TotalAmountText.Text = total.ToString("N2");
        PayableAmountText.Text = payable.ToString("N2");
    }

    private decimal GetDiscountAmount()
    {
        var total = _cart.Sum(item => item.Amount);
        var discount = Math.Min(GetDecimal(DiscountTextBox.Text), total);
        if (GetDecimal(DiscountTextBox.Text) != discount)
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
        DiscountTextBox.Text = amount.ToString("N2");
        _updatingDiscount = false;
    }

    private void RefreshCartView()
    {
        CollectionViewSource.GetDefaultView(CartGrid.ItemsSource)?.Refresh();
    }

    private void FocusProductInput()
    {
        ProductInputTextBox.Focus();
        ProductInputTextBox.SelectAll();
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
        WpfUi.Error(this, ex);
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e) => await AddProductFromInputAsync();

    private void ClearButton_Click(object sender, RoutedEventArgs e) => ClearCart();

    private async void CheckoutButton_Click(object sender, RoutedEventArgs e) => await CheckoutAsync();

}


