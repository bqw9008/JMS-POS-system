using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using POS_system_cs.Domain.Enums;
using POS_system_cs.UI.Wpf.Localization;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfControl = System.Windows.Controls.Control;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfMediaBrush = System.Windows.Media.Brush;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace POS_system_cs.UI.Wpf.Controls;

internal static class WpfUi
{
    public static Grid SplitPage(string title, string description, out Border list, out Border form, double rightWidth = 320)
    {
        var root = new Grid { Margin = new Thickness(22) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = Header(title, description);
        header.Margin = new Thickness(0, 0, 0, 14);
        root.Children.Add(header);

        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(rightWidth) });
        list = Card(null, new Thickness(0, 0, 16, 0));
        form = Card(null, new Thickness(0));
        System.Windows.Controls.Grid.SetColumn(form, 1);
        content.Children.Add(list);
        content.Children.Add(form);
        System.Windows.Controls.Grid.SetRow(content, 1);
        root.Children.Add(content);
        return root;
    }

    public static StackPanel Header(string title, string description)
    {
        var panel = new StackPanel { MinWidth = 260, Margin = new Thickness(0, 0, 18, 0) };
        panel.Children.Add(new TextBlock { Text = title, FontSize = 24, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(MediaColor.FromRgb(15, 23, 42)) });
        panel.Children.Add(new TextBlock { Text = description, Foreground = new SolidColorBrush(MediaColor.FromRgb(100, 116, 139)), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) });
        return panel;
    }

    public static StackPanel Form() => new();

    public static TextBlock Title(string text) => new()
    {
        Text = text,
        FontSize = 18,
        FontWeight = FontWeights.Bold,
        Foreground = new SolidColorBrush(MediaColor.FromRgb(15, 23, 42)),
        Margin = new Thickness(0, 0, 0, 12)
    };

    public static StackPanel Field(string label, WpfControl editor)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        panel.Children.Add(new TextBlock { Text = label, Foreground = new SolidColorBrush(MediaColor.FromRgb(71, 85, 105)), Margin = new Thickness(0, 0, 0, 5) });
        panel.Children.Add(editor);
        return panel;
    }

    public static WpfTextBox TextBox(bool multiline = false) => new()
    {
        Height = multiline ? 88 : 38,
        Padding = new Thickness(10, 0, 10, 0),
        VerticalContentAlignment = multiline ? VerticalAlignment.Top : VerticalAlignment.Center,
        TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
        AcceptsReturn = multiline
    };

    public static WpfComboBox Combo() => new() { Height = 38, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };

    public static WpfDataGrid Grid() => new()
    {
        AutoGenerateColumns = false,
        CanUserAddRows = false,
        CanUserDeleteRows = false,
        IsReadOnly = true,
        SelectionMode = DataGridSelectionMode.Single,
        SelectionUnit = DataGridSelectionUnit.FullRow,
        RowHeaderWidth = 0,
        RowHeight = 40,
        ColumnHeaderHeight = 40,
        GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
        Background = MediaBrushes.White,
        BorderThickness = new Thickness(0),
        FontSize = 13.5
    };

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

    public static Border Card(UIElement? child, Thickness margin) => new()
    {
        Margin = margin,
        Padding = new Thickness(16),
        Background = MediaBrushes.White,
        BorderBrush = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(18),
        Child = child
    };

    public static Border Section(string title, UIElement child, Thickness margin)
    {
        var panel = new Grid();
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        panel.Children.Add(new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
        System.Windows.Controls.Grid.SetRow(child, 1);
        panel.Children.Add(child);
        return Card(panel, margin);
    }

    public static WpfButton Primary(string text, RoutedEventHandler click, bool compact = false) => Button(text, click, compact, MediaColor.FromRgb(37, 99, 235), MediaBrushes.White);

    public static WpfButton Secondary(string text, RoutedEventHandler click, bool compact = false) => Button(text, click, compact, MediaColor.FromRgb(241, 245, 249), new SolidColorBrush(MediaColor.FromRgb(30, 41, 59)));

    public static WpfButton Danger(string text, RoutedEventHandler click) => Button(text, click, compact: false, MediaColor.FromRgb(254, 242, 242), new SolidColorBrush(MediaColor.FromRgb(185, 28, 28)));

    private static WpfButton Button(string text, RoutedEventHandler click, bool compact, MediaColor color, WpfMediaBrush foreground)
    {
        var button = new WpfButton
        {
            Content = text,
            Height = compact ? 34 : 40,
            MinWidth = compact ? 76 : 110,
            Margin = compact ? new Thickness(8, 0, 0, 0) : new Thickness(0, 8, 0, 0),
            Padding = new Thickness(14, 0, 14, 0),
            Background = new SolidColorBrush(color),
            Foreground = foreground,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        button.Click += click;
        return button;
    }

    public static TextBlock SmallLabel(string text) => new() { Text = text, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 8, 0) };

    public static TextBlock Metric() => new() { FontSize = 24, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 0) };

    public static Border MetricCard(string title, TextBlock value)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = title, Foreground = new SolidColorBrush(MediaColor.FromRgb(100, 116, 139)) });
        panel.Children.Add(value);
        return Card(panel, new Thickness(0, 0, 10, 0));
    }

    public static TabItem Tab(string header, UIElement content) => new() { Header = header, Content = content, Padding = new Thickness(12, 6, 12, 6) };

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

    public static void Error(DependencyObject owner, Exception ex) =>
        System.Windows.MessageBox.Show(Window.GetWindow(owner), ex.Message, Localizer.T("OperationFailed"), MessageBoxButton.OK, MessageBoxImage.Error);
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
