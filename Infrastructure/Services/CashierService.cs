using Microsoft.Data.Sqlite;
using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;
using POS_system_cs.Infrastructure.Persistence;
using POS_system_cs.UI.Wpf.Localization;

namespace POS_system_cs.Infrastructure.Services;

public sealed class CashierService : ICashierService
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly IAppLogger _logger;

    public CashierService(SqliteConnectionFactory connectionFactory, IAppLogger logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Order> CheckoutAsync(Order order, CancellationToken cancellationToken = default)
    {
        Validate(order);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await EnsureEnoughStockAsync(connection, (SqliteTransaction)transaction, order, cancellationToken);

            order.Id = order.Id == Guid.Empty ? Guid.NewGuid() : order.Id;
            order.OrderNo = string.IsNullOrWhiteSpace(order.OrderNo) ? CreateOrderNo() : order.OrderNo;
            order.OrderedAt = DateTime.Now;
            order.CreatedAt = DateTime.Now;

            await InsertOrderAsync(connection, (SqliteTransaction)transaction, order, cancellationToken);

            foreach (var item in order.Items)
            {
                item.Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id;
                item.OrderId = order.Id;
                item.CreatedAt = DateTime.Now;
                await InsertOrderItemAsync(connection, (SqliteTransaction)transaction, item, cancellationToken);
                await DeductStockAsync(connection, (SqliteTransaction)transaction, item.ProductId, item.Quantity, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            _logger.Info($"Checkout completed. OrderNo={order.OrderNo}; OrderId={order.Id}; Items={order.Items.Count}; Total={order.TotalAmount:N2}; Discount={order.DiscountAmount:N2}; Received={order.ReceivedAmount:N2}; Payment={order.PaymentMethod}.");
            return order;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.Error($"Checkout failed. OrderNo={order.OrderNo}; Items={order.Items.Count}; Total={order.TotalAmount:N2}; Discount={order.DiscountAmount:N2}; Received={order.ReceivedAmount:N2}; Payment={order.PaymentMethod}.", ex);
            throw;
        }
    }

    private static async Task EnsureEnoughStockAsync(SqliteConnection connection, SqliteTransaction transaction, Order order, CancellationToken cancellationToken)
    {
        foreach (var item in order.Items)
        {
            var stock = await GetStockAsync(connection, transaction, item.ProductId, cancellationToken);
            if (stock < item.Quantity)
            {
                throw new InvalidOperationException(Localizer.Format("Cashier.StockInsufficient", item.ProductName, stock, item.Quantity));
            }
        }
    }

    private static async Task<decimal> GetStockAsync(SqliteConnection connection, SqliteTransaction transaction, Guid productId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COALESCE(quantity, 0) FROM stock WHERE product_id = $productId;";
        command.Parameters.AddWithValue("$productId", productId.ToString());
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null || value == DBNull.Value ? 0 : Convert.ToDecimal(value);
    }

    private static async Task InsertOrderAsync(SqliteConnection connection, SqliteTransaction transaction, Order order, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO orders (id, order_no, ordered_at, operator_id, total_amount,
                                discount_amount, received_amount, payment_method, created_at, updated_at)
            VALUES ($id, $orderNo, $orderedAt, $operatorId, $totalAmount,
                    $discountAmount, $receivedAmount, $paymentMethod, $createdAt, NULL);
            """;
        command.Parameters.AddWithValue("$id", order.Id.ToString());
        command.Parameters.AddWithValue("$orderNo", order.OrderNo);
        command.Parameters.AddWithValue("$orderedAt", order.OrderedAt.ToString("O"));
        command.Parameters.AddWithValue("$operatorId", order.OperatorId is null ? DBNull.Value : order.OperatorId.Value.ToString());
        command.Parameters.AddWithValue("$totalAmount", order.TotalAmount);
        command.Parameters.AddWithValue("$discountAmount", order.DiscountAmount);
        command.Parameters.AddWithValue("$receivedAmount", order.ReceivedAmount);
        command.Parameters.AddWithValue("$paymentMethod", (int)order.PaymentMethod);
        command.Parameters.AddWithValue("$createdAt", order.CreatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertOrderItemAsync(SqliteConnection connection, SqliteTransaction transaction, OrderItem item, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO order_items (id, order_id, product_id, product_name, barcode,
                                     quantity, unit_price, created_at, updated_at)
            VALUES ($id, $orderId, $productId, $productName, $barcode,
                    $quantity, $unitPrice, $createdAt, NULL);
            """;
        command.Parameters.AddWithValue("$id", item.Id.ToString());
        command.Parameters.AddWithValue("$orderId", item.OrderId.ToString());
        command.Parameters.AddWithValue("$productId", item.ProductId.ToString());
        command.Parameters.AddWithValue("$productName", item.ProductName);
        command.Parameters.AddWithValue("$barcode", item.Barcode);
        command.Parameters.AddWithValue("$quantity", item.Quantity);
        command.Parameters.AddWithValue("$unitPrice", item.UnitPrice);
        command.Parameters.AddWithValue("$createdAt", item.CreatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DeductStockAsync(SqliteConnection connection, SqliteTransaction transaction, Guid productId, decimal quantity, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE stock
            SET quantity = quantity - $quantity,
                last_changed_at = $now,
                updated_at = $now
            WHERE product_id = $productId;
            """;
        command.Parameters.AddWithValue("$productId", productId.ToString());
        command.Parameters.AddWithValue("$quantity", quantity);
        command.Parameters.AddWithValue("$now", DateTime.Now.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void Validate(Order order)
    {
        if (order.Items.Count == 0)
        {
            throw new InvalidOperationException(Localizer.T("Cashier.EmptyCart"));
        }

        if (order.Items.Any(item => item.Quantity <= 0))
        {
            throw new InvalidOperationException(Localizer.T("Cashier.QuantityPositive"));
        }

        order.TotalAmount = order.Items.Sum(item => item.Amount);
        if (order.DiscountAmount < 0)
        {
            throw new InvalidOperationException(Localizer.T("Cashier.DiscountNonNegative"));
        }

        if (order.DiscountAmount > order.TotalAmount)
        {
            throw new InvalidOperationException(Localizer.T("Cashier.DiscountTooLarge"));
        }

        var payable = order.TotalAmount - order.DiscountAmount;
        if (order.ReceivedAmount < payable)
        {
            throw new InvalidOperationException(Localizer.T("Cashier.ReceivedTooSmall"));
        }
    }

    private static string CreateOrderNo()
    {
        return $"SO{DateTime.Now:yyyyMMddHHmmssfff}";
    }
}
