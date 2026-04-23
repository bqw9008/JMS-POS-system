using System.Windows;
using POS_system_cs.UI.Wpf.Localization;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed partial class QuantityDialog : Window
{
    public QuantityDialog(string productName, decimal currentQuantity)
    {
        InitializeComponent();
        Title = Localizer.T("Cashier.QuantityTitle");
        ProductText.Text = Localizer.Format("Cashier.ProductPrefix", productName);
        QuantityBox.Text = currentQuantity.ToString("N2");
        OkButton.Content = Localizer.T("Action.Apply");
        CancelButton.Content = Localizer.T("Cashier.Cancel");

        Loaded += (_, _) =>
        {
            QuantityBox.Focus();
            QuantityBox.SelectAll();
        };
    }

    public decimal Quantity { get; private set; }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var quantity = WpfUi.Number(QuantityBox.Text);
        if (quantity <= 0)
        {
            System.Windows.MessageBox.Show(this, Localizer.T("Cashier.QuantityPositive"), Localizer.T("Info"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Quantity = Math.Min(quantity, 1_000_000M);
        DialogResult = true;
    }
}
