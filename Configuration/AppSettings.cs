namespace POS_system_cs.Configuration;

public sealed class AppSettings
{
    public string StoreName { get; init; } = "小型商超";

    public string DatabasePath { get; init; } = "pos.db";

    public string ReceiptPrinterName { get; init; } = string.Empty;
}
