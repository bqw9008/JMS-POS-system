using Microsoft.Data.Sqlite;
using POS_system_cs.Application.Models;
using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;
using POS_system_cs.Infrastructure.Persistence;
using POS_system_cs.UI.Wpf.Localization;

namespace POS_system_cs.Infrastructure.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly IAppLogger _logger;

    public InventoryService(SqliteConnectionFactory connectionFactory, IAppLogger logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<StockOverview>> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var rows = new List<StockOverview>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.id,
                   p.code,
                   p.name,
                   p.barcode,
                   COALESCE(s.quantity, 0) AS quantity,
                   p.low_stock_threshold,
                   COALESCE(s.last_changed_at, p.created_at) AS last_changed_at
            FROM products p
            LEFT JOIN stock s ON s.product_id = p.id
            WHERE p.is_active = 1
            ORDER BY p.name;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var quantity = reader.GetDecimal(4);
            var threshold = reader.GetDecimal(5);
            rows.Add(new StockOverview(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                quantity,
                threshold,
                quantity <= threshold,
                DateTime.Parse(reader.GetString(6))));
        }

        return rows;
    }

    public async Task<IReadOnlyList<StockItem>> GetLowStockItemsAsync(CancellationToken cancellationToken = default)
    {
        var stockItems = new List<StockItem>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT s.id, s.product_id, s.quantity, s.last_changed_at, s.created_at, s.updated_at
            FROM stock s
            INNER JOIN products p ON p.id = s.product_id
            WHERE p.is_active = 1 AND s.quantity <= p.low_stock_threshold
            ORDER BY s.quantity ASC;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            stockItems.Add(ReadStockItem(reader));
        }

        return stockItems;
    }

    public async Task SetStockAsync(Guid productId, decimal quantity, CancellationToken cancellationToken = default)
    {
        if (quantity < 0)
        {
            throw new InvalidOperationException(Localizer.T("Inventory.StockNonNegative"));
        }

        try
        {
            var change = await UpsertStockAsync(productId, quantity, false, cancellationToken);
            _logger.Info($"Stock set. ProductId={productId}; Previous={change.PreviousQuantity:N2}; New={change.NewQuantity:N2}.");
        }
        catch (Exception ex)
        {
            _logger.Error($"Stock set failed. ProductId={productId}; Requested={quantity:N2}.", ex);
            throw;
        }
    }

    public async Task AdjustStockAsync(Guid productId, decimal quantityDelta, string reason, CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
        {
            throw new InvalidOperationException(Localizer.T("Inventory.SelectProduct"));
        }

        if (quantityDelta == 0)
        {
            throw new InvalidOperationException(Localizer.T("Inventory.AdjustNonZero"));
        }

        try
        {
            var change = await UpsertStockAsync(productId, quantityDelta, true, cancellationToken);
            _logger.Info($"Stock adjusted. ProductId={productId}; Previous={change.PreviousQuantity:N2}; Delta={quantityDelta:N2}; New={change.NewQuantity:N2}; Reason={NormalizeReason(reason)}.");
        }
        catch (Exception ex)
        {
            _logger.Error($"Stock adjustment failed. ProductId={productId}; Delta={quantityDelta:N2}; Reason={NormalizeReason(reason)}.", ex);
            throw;
        }
    }

    private async Task<(decimal PreviousQuantity, decimal NewQuantity)> UpsertStockAsync(Guid productId, decimal quantity, bool isDelta, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var currentQuantity = await GetCurrentQuantityAsync(connection, (SqliteTransaction)transaction, productId, cancellationToken);
            var newQuantity = isDelta ? currentQuantity + quantity : quantity;
            if (newQuantity < 0)
            {
                throw new InvalidOperationException(Localizer.T("Inventory.ResultNonNegative"));
            }

            await using var command = connection.CreateCommand();
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText = """
                INSERT INTO stock (id, product_id, quantity, last_changed_at, created_at, updated_at)
                VALUES ($id, $productId, $quantity, $now, $now, NULL)
                ON CONFLICT(product_id) DO UPDATE SET
                    quantity = excluded.quantity,
                    last_changed_at = excluded.last_changed_at,
                    updated_at = excluded.last_changed_at;
                """;
            command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("$productId", productId.ToString());
            command.Parameters.AddWithValue("$quantity", newQuantity);
            command.Parameters.AddWithValue("$now", DateTime.Now.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return (currentQuantity, newQuantity);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<decimal> GetCurrentQuantityAsync(SqliteConnection connection, SqliteTransaction transaction, Guid productId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COALESCE(quantity, 0) FROM stock WHERE product_id = $productId;";
        command.Parameters.AddWithValue("$productId", productId.ToString());
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null || value == DBNull.Value ? 0 : Convert.ToDecimal(value);
    }

    private static StockItem ReadStockItem(SqliteDataReader reader)
    {
        return new StockItem
        {
            Id = Guid.Parse(reader.GetString(0)),
            ProductId = Guid.Parse(reader.GetString(1)),
            Quantity = reader.GetDecimal(2),
            LastChangedAt = DateTime.Parse(reader.GetString(3)),
            CreatedAt = DateTime.Parse(reader.GetString(4)),
            UpdatedAt = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5))
        };
    }

    private static string NormalizeReason(string? reason)
    {
        return string.IsNullOrWhiteSpace(reason) ? "<empty>" : reason.Trim();
    }
}
