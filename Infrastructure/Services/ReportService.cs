using Microsoft.Data.Sqlite;
using POS_system_cs.Application.Models;
using POS_system_cs.Application.Services;
using POS_system_cs.Infrastructure.Persistence;
using POS_system_cs.UI.Wpf.Localization;

namespace POS_system_cs.Infrastructure.Services;

public sealed class ReportService : IReportService
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public ReportService(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ReportDashboard> GetDashboardAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            throw new InvalidOperationException(Localizer.T("Orders.FromDateError"));
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var todaySalesAmount = await GetSalesAmountAsync(connection, today, today, cancellationToken);
        var todayOrderCount = await GetOrderCountAsync(connection, today, today, cancellationToken);
        var todayProductQuantity = await GetProductQuantityAsync(connection, today, today, cancellationToken);
        var totalStockQuantity = await GetTotalStockQuantityAsync(connection, cancellationToken);
        var lowStockProductCount = await GetLowStockProductCountAsync(connection, cancellationToken);
        var dailySales = await GetSalesSummaryAsync(connection, from, to, SalesSummaryKind.Day, cancellationToken);
        var weeklySales = await GetSalesSummaryAsync(connection, from, to, SalesSummaryKind.Week, cancellationToken);
        var monthlySales = await GetSalesSummaryAsync(connection, from, to, SalesSummaryKind.Month, cancellationToken);
        var topSellingProducts = await GetProductRankingAsync(connection, from, to, true, cancellationToken);
        var slowSellingProducts = await GetProductRankingAsync(connection, from, to, false, cancellationToken);

        return new ReportDashboard(
            todaySalesAmount,
            todayOrderCount,
            todayProductQuantity,
            totalStockQuantity,
            lowStockProductCount,
            dailySales,
            weeklySales,
            monthlySales,
            topSellingProducts,
            slowSellingProducts);
    }

    private static async Task<decimal> GetSalesAmountAsync(SqliteConnection connection, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COALESCE(SUM(total_amount - discount_amount), 0)
            FROM orders
            WHERE ordered_at >= $from AND ordered_at < $to;
            """;
        AddDateRange(command, from, to);
        return Convert.ToDecimal(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<int> GetOrderCountAsync(SqliteConnection connection, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(1)
            FROM orders
            WHERE ordered_at >= $from AND ordered_at < $to;
            """;
        AddDateRange(command, from, to);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<decimal> GetProductQuantityAsync(SqliteConnection connection, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COALESCE(SUM(oi.quantity), 0)
            FROM order_items oi
            INNER JOIN orders o ON o.id = oi.order_id
            WHERE o.ordered_at >= $from AND o.ordered_at < $to;
            """;
        AddDateRange(command, from, to);
        return Convert.ToDecimal(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<decimal> GetTotalStockQuantityAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COALESCE(SUM(s.quantity), 0)
            FROM stock s
            INNER JOIN products p ON p.id = s.product_id
            WHERE p.is_active = 1;
            """;
        return Convert.ToDecimal(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<int> GetLowStockProductCountAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(1)
            FROM stock s
            INNER JOIN products p ON p.id = s.product_id
            WHERE p.is_active = 1 AND s.quantity <= p.low_stock_threshold;
            """;
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<IReadOnlyList<SalesSummaryPoint>> GetSalesSummaryAsync(
        SqliteConnection connection,
        DateOnly from,
        DateOnly to,
        SalesSummaryKind kind,
        CancellationToken cancellationToken)
    {
        var points = new List<SalesSummaryPoint>();
        await using var command = connection.CreateCommand();
        command.CommandText = kind switch
        {
            SalesSummaryKind.Day => """
                SELECT strftime('%Y-%m-%d', ordered_at) AS label,
                       MIN(ordered_at) AS period_start,
                       COALESCE(SUM(total_amount - discount_amount), 0) AS sales_amount,
                       COUNT(1) AS order_count
                FROM orders
                WHERE ordered_at >= $from AND ordered_at < $to
                GROUP BY label
                ORDER BY label;
                """,
            SalesSummaryKind.Week => """
                SELECT strftime('%Y-W%W', ordered_at) AS label,
                       MIN(ordered_at) AS period_start,
                       COALESCE(SUM(total_amount - discount_amount), 0) AS sales_amount,
                       COUNT(1) AS order_count
                FROM orders
                WHERE ordered_at >= $from AND ordered_at < $to
                GROUP BY label
                ORDER BY label;
                """,
            _ => """
                SELECT strftime('%Y-%m', ordered_at) AS label,
                       MIN(ordered_at) AS period_start,
                       COALESCE(SUM(total_amount - discount_amount), 0) AS sales_amount,
                       COUNT(1) AS order_count
                FROM orders
                WHERE ordered_at >= $from AND ordered_at < $to
                GROUP BY label
                ORDER BY label;
                """
        };
        AddDateRange(command, from, to);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            points.Add(new SalesSummaryPoint(
                reader.GetString(0),
                DateTime.Parse(reader.GetString(1)),
                reader.GetDecimal(2),
                reader.GetInt32(3)));
        }

        return points;
    }

    private static async Task<IReadOnlyList<ProductSalesRanking>> GetProductRankingAsync(
        SqliteConnection connection,
        DateOnly from,
        DateOnly to,
        bool descending,
        CancellationToken cancellationToken)
    {
        var products = new List<ProductSalesRanking>();
        await using var command = connection.CreateCommand();
        command.CommandText = $$"""
            SELECT oi.product_id,
                   oi.product_name,
                   oi.barcode,
                   COALESCE(SUM(oi.quantity), 0) AS quantity,
                   COALESCE(SUM(oi.quantity * oi.unit_price), 0) AS sales_amount
            FROM order_items oi
            INNER JOIN orders o ON o.id = oi.order_id
            WHERE o.ordered_at >= $from AND o.ordered_at < $to
            GROUP BY oi.product_id, oi.product_name, oi.barcode
            ORDER BY quantity {{(descending ? "DESC" : "ASC")}}, sales_amount {{(descending ? "DESC" : "ASC")}}
            LIMIT 10;
            """;
        AddDateRange(command, from, to);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(new ProductSalesRanking(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDecimal(3),
                reader.GetDecimal(4)));
        }

        return products;
    }

    private static void AddDateRange(SqliteCommand command, DateOnly from, DateOnly to)
    {
        command.Parameters.AddWithValue("$from", from.ToDateTime(TimeOnly.MinValue).ToString("O"));
        command.Parameters.AddWithValue("$to", to.AddDays(1).ToDateTime(TimeOnly.MinValue).ToString("O"));
    }

    private enum SalesSummaryKind
    {
        Day,
        Week,
        Month
    }
}
