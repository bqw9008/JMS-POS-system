namespace POS_system_cs.Application.Models;

public sealed record StockOverview(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string Barcode,
    decimal Quantity,
    decimal LowStockThreshold,
    bool IsLowStock,
    DateTime LastChangedAt);
