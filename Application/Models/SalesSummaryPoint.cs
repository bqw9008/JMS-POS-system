namespace POS_system_cs.Application.Models;

public sealed record SalesSummaryPoint(
    string Label,
    DateTime PeriodStart,
    decimal SalesAmount,
    int OrderCount);
