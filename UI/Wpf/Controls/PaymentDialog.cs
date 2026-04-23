using System.Globalization;
using System.Windows;
using System.Windows.Input;
using POS_system_cs.Domain.Enums;
using POS_system_cs.UI.Wpf.Localization;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed partial class PaymentDialog : Window
{
    private decimal _remainingAmount;
    private decimal _cashAmount;
    private decimal _onlineAmount;
    private decimal _receivedTotal;

    public PaymentDialog(decimal payableAmount, decimal discountAmount)
    {
        _remainingAmount = payableAmount;
        InitializeComponent();
        ApplyLocalization();
        DiscountBox.Text = discountAmount.ToString("N2");
        RefreshPaymentView();

        Loaded += (_, _) =>
        {
            ReceivedBox.Focus();
            ReceivedBox.SelectAll();
        };
    }

    public PaymentResult? Result { get; private set; }

    private void ApplyLocalization()
    {
        Title = Localizer.T("Cashier.PaymentTitle");
        PayableLabel.Text = Localizer.T("Field.Payable");
        DiscountLabel.Text = Localizer.T("Field.Discount");
        ReceivedLabel.Text = Localizer.T("Field.Received");
        ChangeLabel.Text = Localizer.T("Field.Change");
        HintText.Text = Localizer.T("Cashier.PaymentHint");
        CompleteButton.Content = Localizer.T("Cashier.Complete");
        OnlineButton.Content = Localizer.T("Cashier.Online");
        CashButton.Content = Localizer.T("Cashier.Cash");
        CancelButton.Content = Localizer.T("Cashier.Cancel");
    }

    private void RecordPayment(PaymentMethod method)
    {
        if (_remainingAmount <= 0)
        {
            RefreshPaymentView();
            return;
        }

        var inputAmount = GetDecimal(ReceivedBox.Text);
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
        ReceivedBox.Clear();
        RefreshPaymentView();
        ReceivedBox.Focus();
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
        var inputAmount = GetDecimal(ReceivedBox.Text);
        var change = Math.Max(0, inputAmount - _remainingAmount);
        PayableBox.Text = _remainingAmount.ToString("N2");
        ChangeBox.Text = change.ToString("N2");
        StatusText.Text = Localizer.Format("Cashier.PaymentStatus", _receivedTotal, _cashAmount, _onlineAmount);
    }

    private static decimal GetDecimal(string? text)
    {
        text = text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return 0;
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value)
            && !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)) return 0;
        return Math.Max(0, value);
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

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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

    private void ReceivedBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => RefreshPaymentView();

    private void CompleteButton_Click(object sender, RoutedEventArgs e) => Complete();

    private void OnlineButton_Click(object sender, RoutedEventArgs e) => RecordPayment(PaymentMethod.Online);

    private void CashButton_Click(object sender, RoutedEventArgs e) => RecordPayment(PaymentMethod.Cash);
}

public readonly record struct PaymentResult(decimal ReceivedAmount, PaymentMethod PaymentMethod);
