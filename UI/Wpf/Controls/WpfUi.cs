using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Enums;
using POS_system_cs.UI.Wpf.Localization;
using MediaColor = System.Windows.Media.Color;

namespace POS_system_cs.UI.Wpf.Controls;

internal static class WpfUi
{
    private static IAppLogger? Logger { get; set; }

    public static readonly MediaColor AppBackground = MediaColor.FromRgb(237, 241, 235);
    public static readonly MediaColor ShellBackground = MediaColor.FromRgb(250, 248, 241);
    public static readonly MediaColor CardBackground = MediaColor.FromRgb(252, 250, 244);
    public static readonly MediaColor SubtleBackground = MediaColor.FromRgb(244, 246, 239);
    public static readonly MediaColor BorderColor = MediaColor.FromRgb(218, 224, 215);
    public static readonly MediaColor TextColor = MediaColor.FromRgb(31, 42, 46);
    public static readonly MediaColor MutedTextColor = MediaColor.FromRgb(91, 105, 105);
    public static readonly MediaColor PrimaryColor = MediaColor.FromRgb(82, 121, 111);
    public static readonly MediaColor SecondaryButtonColor = MediaColor.FromRgb(236, 241, 232);
    public static readonly MediaColor DangerBackground = MediaColor.FromRgb(252, 239, 235);
    public static readonly MediaColor DangerText = MediaColor.FromRgb(157, 62, 48);
    public static readonly MediaColor NavStart = MediaColor.FromRgb(224, 234, 224);
    public static readonly MediaColor NavEnd = MediaColor.FromRgb(205, 221, 213);
    public static readonly MediaColor NavButtonBackground = MediaColor.FromRgb(239, 244, 236);

    public static SolidColorBrush Brush(MediaColor color) => new(color);

    public static void ConfigureLogger(IAppLogger logger)
    {
        Logger = logger;
    }

    public static DataGridTextColumn TextColumn(string header, string path, double width = 120, bool star = false) => new()
    {
        Header = header,
        Binding = new System.Windows.Data.Binding(path),
        Width = star ? new DataGridLength(1, DataGridLengthUnitType.Star) : new DataGridLength(width)
    };

    public static DataGridTextColumn MoneyColumn(string header, string path, double width) => new()
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

    public static DataGridTextColumn DateColumn(string header, string path, double width) => new()
    {
        Header = header,
        Binding = new System.Windows.Data.Binding(path) { StringFormat = "yyyy-MM-dd HH:mm:ss" },
        Width = new DataGridLength(width)
    };

    public static DataGridTextColumn PaymentColumn(string header, string path, double width = 120, bool star = false) => new()
    {
        Header = header,
        Binding = new System.Windows.Data.Binding(path) { Converter = new PaymentMethodTextConverter() },
        Width = star ? new DataGridLength(1, DataGridLengthUnitType.Star) : new DataGridLength(width)
    };

    public static DataGridCheckBoxColumn CheckColumn(string header, string path, double width) => new()
    {
        Header = header,
        Binding = new System.Windows.Data.Binding(path),
        Width = new DataGridLength(width)
    };

    public static decimal Number(string? text, bool allowNegative = false)
    {
        text = text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return 0;
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value)
            && !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)) return 0;
        return allowNegative ? value : Math.Max(0, value);
    }

    public static bool Confirm(DependencyObject owner, string message) =>
        System.Windows.MessageBox.Show(Window.GetWindow(owner), message, Localizer.T("Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public static void Info(DependencyObject owner, string message) =>
        System.Windows.MessageBox.Show(Window.GetWindow(owner), message, Localizer.T("Info"), MessageBoxButton.OK, MessageBoxImage.Information);

    public static void Error(DependencyObject owner, Exception ex)
    {
        Logger?.Error("UI operation failed.", ex);
        System.Windows.MessageBox.Show(Window.GetWindow(owner), ex.Message, Localizer.T("OperationFailed"), MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

internal sealed class PaymentMethodTextConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is PaymentMethod method
            ? method switch
            {
                PaymentMethod.Online => Localizer.T("Payment.Online"),
                PaymentMethod.Mixed => Localizer.T("Payment.Mixed"),
                PaymentMethod.WeChat => Localizer.T("Payment.WeChat"),
                PaymentMethod.Alipay => Localizer.T("Payment.Alipay"),
                PaymentMethod.BankCard => Localizer.T("Payment.BankCard"),
                _ => Localizer.T("Payment.Cash")
            }
            : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

internal static class ObservableCollectionExtensions
{
    public static void ReplaceWith<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
