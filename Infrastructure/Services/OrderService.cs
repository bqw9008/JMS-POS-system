using Microsoft.Data.Sqlite;
using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;
using POS_system_cs.Domain.Enums;
using POS_system_cs.Infrastructure.Persistence;

namespace POS_system_cs.Infrastructure.Services;

public sealed class OrderService : IOrderService
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public OrderService(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Order>> SearchAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var orders = new List<Order>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, order_no, ordered_at, operator_id, total_amount,
                   discount_amount, received_amount, payment_method, created_at, updated_at
            FROM orders
            WHERE ($from IS NULL OR ordered_at >= $from)
              AND ($to IS NULL OR ordered_at < $to)
            ORDER BY ordered_at DESC;
            """;
        command.Parameters.AddWithValue("$from", from is null ? DBNull.Value : from.Value.ToDateTime(TimeOnly.MinValue).ToString("O"));
        command.Parameters.AddWithValue("$to", to is null ? DBNull.Value : to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue).ToString("O"));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            orders.Add(ReadOrder(reader));
        }

        return orders;
    }

    public async Task<IReadOnlyList<OrderItem>> GetItemsAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var items = new List<OrderItem>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, order_id, product_id, product_name, barcode,
                   quantity, unit_price, created_at, updated_at
            FROM order_items
            WHERE order_id = $orderId
            ORDER BY created_at, product_name;
            """;
        command.Parameters.AddWithValue("$orderId", orderId.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(ReadOrderItem(reader));
        }

        return items;
    }

    private static Order ReadOrder(SqliteDataReader reader)
    {
        return new Order
        {
            Id = Guid.Parse(reader.GetString(0)),
            OrderNo = reader.GetString(1),
            OrderedAt = DateTime.Parse(reader.GetString(2)),
            OperatorId = reader.IsDBNull(3) ? null : Guid.Parse(reader.GetString(3)),
            TotalAmount = reader.GetDecimal(4),
            DiscountAmount = reader.GetDecimal(5),
            ReceivedAmount = reader.GetDecimal(6),
            PaymentMethod = (PaymentMethod)reader.GetInt32(7),
            CreatedAt = DateTime.Parse(reader.GetString(8)),
            UpdatedAt = reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9))
        };
    }

    private static OrderItem ReadOrderItem(SqliteDataReader reader)
    {
        return new OrderItem
        {
            Id = Guid.Parse(reader.GetString(0)),
            OrderId = Guid.Parse(reader.GetString(1)),
            ProductId = Guid.Parse(reader.GetString(2)),
            ProductName = reader.GetString(3),
            Barcode = reader.GetString(4),
            Quantity = reader.GetDecimal(5),
            UnitPrice = reader.GetDecimal(6),
            CreatedAt = DateTime.Parse(reader.GetString(7)),
            UpdatedAt = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8))
        };
    }
}
