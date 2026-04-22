namespace POS_system_cs.Application.Models;

public sealed record ReportDashboard(
    decimal TodaySalesAmount,
    int TodayOrderCount,
    decimal TodayProductQuantity,
    decimal TotalStockQuantity,
    int LowStockProductCount,
    IReadOnlyList<SalesSummaryPoint> DailySales,
    IReadOnlyList<SalesSummaryPoint> WeeklySales,
    IReadOnlyList<SalesSummaryPoint> MonthlySales,
    IReadOnlyList<ProductSalesRanking> TopSellingProducts,
    IReadOnlyList<ProductSalesRanking> SlowSellingProducts);
