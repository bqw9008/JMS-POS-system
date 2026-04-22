namespace POS_system_cs.Application.Models;

public sealed record ProductSalesRanking(
    Guid ProductId,
    string ProductName,
    string Barcode,
    decimal Quantity,
    decimal SalesAmount);
