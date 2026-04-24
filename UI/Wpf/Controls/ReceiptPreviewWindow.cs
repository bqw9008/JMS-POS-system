using System.Globalization;
using System.Text;
using System.Windows;
using POS_system_cs.Domain.Entities;
using POS_system_cs.Domain.Enums;
using POS_system_cs.UI.Wpf.Localization;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed partial class ReceiptPreviewWindow : Window
{
    public ReceiptPreviewWindow(Order order, string storeName)
    {
        InitializeComponent();
        ApplyLocalization();
        ReceiptContentBox.Text = BuildReceiptText(order, storeName);
    }

    private void ApplyLocalization()
    {
        Title = CurrentText("Receipt Preview", "小票预览");
        HeaderText.Text = CurrentText("Receipt Preview", "小票内容预览");
        DescriptionText.Text = CurrentText(
            "Checkout completed. This window shows test receipt content before real printer integration.",
            "结算已完成。当前窗口只用于测试小票内容预览，暂未接入真实打印。");
        CloseButton.Content = CurrentText("Close", "关闭");
    }

    private static string BuildReceiptText(Order order, string storeName)
    {
        var receiptStoreName = string.IsNullOrWhiteSpace(storeName)
            ? CurrentText("Store", "门店")
            : storeName.Trim();
        var builder = new StringBuilder();
        var line = new string('-', 32);
        var payableAmount = order.TotalAmount - order.DiscountAmount;

        builder.AppendLine(receiptStoreName);
        builder.AppendLine(CurrentText("Receipt Preview", "小票内容预览"));
        builder.AppendLine(line);
        builder.AppendLine($"{Localizer.T("Field.OrderNo")}: {order.OrderNo}");
        builder.AppendLine($"{Localizer.T("Field.Time")}: {order.OrderedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture)}");
        builder.AppendLine($"{Localizer.T("Field.Payment")}: {PaymentMethodText(order.PaymentMethod)}");
        builder.AppendLine(line);

        foreach (var item in order.Items)
        {
            builder.AppendLine(item.ProductName);

            if (!string.IsNullOrWhiteSpace(item.Barcode))
            {
                builder.AppendLine($"  {Localizer.T("Field.Barcode")}: {item.Barcode}");
            }

            builder.AppendLine($"  {item.Quantity:N2} x {item.UnitPrice:N2} = {item.Amount:N2}");
            builder.AppendLine();
        }

        builder.AppendLine(line);
        builder.AppendLine($"{Localizer.T("Field.Total")}: {order.TotalAmount:N2}");
        builder.AppendLine($"{Localizer.T("Field.Discount")}: {order.DiscountAmount:N2}");
        builder.AppendLine($"{Localizer.T("Field.Payable")}: {payableAmount:N2}");
        builder.AppendLine($"{Localizer.T("Field.Received")}: {order.ReceivedAmount:N2}");
        builder.AppendLine($"{Localizer.T("Field.Change")}: {order.ChangeAmount:N2}");
        builder.AppendLine(line);
        builder.AppendLine(CurrentText(
            "Thank you. This receipt is for preview testing only.",
            "感谢惠顾，本小票仅用于预览测试。"));

        return builder.ToString().TrimEnd();
    }

    private static string CurrentText(string english, string chinese)
    {
        return Localizer.Current == AppLanguage.Chinese ? chinese : english;
    }

    private static string PaymentMethodText(PaymentMethod paymentMethod)
    {
        return paymentMethod switch
        {
            PaymentMethod.Cash => Localizer.T("Payment.Cash"),
            PaymentMethod.WeChat => Localizer.T("Payment.WeChat"),
            PaymentMethod.Alipay => Localizer.T("Payment.Alipay"),
            PaymentMethod.BankCard => Localizer.T("Payment.BankCard"),
            PaymentMethod.Online => Localizer.T("Payment.Online"),
            PaymentMethod.Mixed => Localizer.T("Payment.Mixed"),
            _ => paymentMethod.ToString()
        };
    }
}
