namespace POS_system_cs.Configuration;

public sealed class AppSettings
{
    public string StoreName { get; set; } = "小型商超";

    public string DatabasePath { get; set; } = "pos.db";

    public string ReceiptPrinterName { get; set; } = string.Empty;

    public AppSettings Clone()
    {
        return new AppSettings
        {
            StoreName = StoreName,
            DatabasePath = DatabasePath,
            ReceiptPrinterName = ReceiptPrinterName
        };
    }

    public void ApplyFrom(AppSettings other)
    {
        StoreName = other.StoreName;
        DatabasePath = other.DatabasePath;
        ReceiptPrinterName = other.ReceiptPrinterName;
    }
}
