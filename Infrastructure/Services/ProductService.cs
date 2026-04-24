using Microsoft.Data.Sqlite;
using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;
using POS_system_cs.Infrastructure.Persistence;
using POS_system_cs.UI.Wpf.Localization;

namespace POS_system_cs.Infrastructure.Services;

public sealed class ProductService : IProductService
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public ProductService(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await SearchAsync(string.Empty, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var products = new List<Product>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, code, name, barcode, category_id, cost_price, sale_price,
                   low_stock_threshold, is_active, created_at, updated_at
            FROM products
            WHERE $keyword = ''
               OR code LIKE $likeKeyword
               OR name LIKE $likeKeyword
               OR barcode LIKE $likeKeyword
            ORDER BY is_active DESC, name;
            """;
        command.Parameters.AddWithValue("$keyword", keyword.Trim());
        command.Parameters.AddWithValue("$likeKeyword", $"%{keyword.Trim()}%");

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(ReadProduct(reader));
        }

        return products;
    }

    public async Task<Product?> FindByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, code, name, barcode, category_id, cost_price, sale_price,
                   low_stock_threshold, is_active, created_at, updated_at
            FROM products
            WHERE barcode = $barcode AND is_active = 1;
            """;
        command.Parameters.AddWithValue("$barcode", barcode.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadProduct(reader) : null;
    }

    public async Task<Product?> FindByCodeOrBarcodeAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, code, name, barcode, category_id, cost_price, sale_price,
                   low_stock_threshold, is_active, created_at, updated_at
            FROM products
            WHERE is_active = 1
              AND (code = $input OR barcode = $input);
            """;
        command.Parameters.AddWithValue("$input", input.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadProduct(reader) : null;
    }

    public async Task SaveAsync(Product product, CancellationToken cancellationToken = default)
    {
        Validate(product);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var exists = await ExistsAsync(connection, (SqliteTransaction)transaction, product.Id, cancellationToken);
            await using var command = connection.CreateCommand();
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText = exists
                ? """
                  UPDATE products
                  SET code = $code,
                      name = $name,
                      barcode = $barcode,
                      category_id = $categoryId,
                      cost_price = $costPrice,
                      sale_price = $salePrice,
                      low_stock_threshold = $lowStockThreshold,
                      is_active = $isActive,
                      updated_at = $updatedAt
                  WHERE id = $id;
                  """
                : """
                  INSERT INTO products (id, code, name, barcode, category_id, cost_price, sale_price,
                                        low_stock_threshold, is_active, created_at, updated_at)
                  VALUES ($id, $code, $name, $barcode, $categoryId, $costPrice, $salePrice,
                          $lowStockThreshold, $isActive, $createdAt, $updatedAt);
                  """;

            command.Parameters.AddWithValue("$id", product.Id.ToString());
            command.Parameters.AddWithValue("$code", product.Code.Trim());
            command.Parameters.AddWithValue("$name", product.Name.Trim());
            command.Parameters.AddWithValue("$barcode", product.Barcode.Trim());
            command.Parameters.AddWithValue("$categoryId", product.CategoryId.ToString());
            command.Parameters.AddWithValue("$costPrice", product.CostPrice);
            command.Parameters.AddWithValue("$salePrice", product.SalePrice);
            command.Parameters.AddWithValue("$lowStockThreshold", product.LowStockThreshold);
            command.Parameters.AddWithValue("$isActive", product.IsActive ? 1 : 0);
            command.Parameters.AddWithValue("$createdAt", product.CreatedAt.ToString("O"));
            command.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken);

            if (!exists)
            {
                await CreateInitialStockAsync(connection, (SqliteTransaction)transaction, product.Id, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var hasSales = await HasSalesAsync(connection, (SqliteTransaction)transaction, id, cancellationToken);
            if (hasSales)
            {
                await DisableProductAsync(connection, (SqliteTransaction)transaction, id, cancellationToken);
            }
            else
            {
                await DeleteProductAsync(connection, (SqliteTransaction)transaction, id, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task CreateInitialStockAsync(SqliteConnection connection, SqliteTransaction transaction, Guid productId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO stock (id, product_id, quantity, last_changed_at, created_at, updated_at)
            VALUES ($id, $productId, 0, $now, $now, NULL);
            """;
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("$productId", productId.ToString());
        command.Parameters.AddWithValue("$now", DateTime.Now.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> ExistsAsync(SqliteConnection connection, SqliteTransaction transaction, Guid id, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(1) FROM products WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private static async Task<bool> HasSalesAsync(SqliteConnection connection, SqliteTransaction transaction, Guid id, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(1) FROM order_items WHERE product_id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private static async Task DisableProductAsync(SqliteConnection connection, SqliteTransaction transaction, Guid id, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE products
            SET is_active = 0,
                updated_at = $updatedAt
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());
        command.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DeleteProductAsync(SqliteConnection connection, SqliteTransaction transaction, Guid id, CancellationToken cancellationToken)
    {
        await using (var stockCommand = connection.CreateCommand())
        {
            stockCommand.Transaction = transaction;
            stockCommand.CommandText = "DELETE FROM stock WHERE product_id = $id;";
            stockCommand.Parameters.AddWithValue("$id", id.ToString());
            await stockCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var productCommand = connection.CreateCommand();
        productCommand.Transaction = transaction;
        productCommand.CommandText = "DELETE FROM products WHERE id = $id;";
        productCommand.Parameters.AddWithValue("$id", id.ToString());
        await productCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Product ReadProduct(SqliteDataReader reader)
    {
        return new Product
        {
            Id = Guid.Parse(reader.GetString(0)),
            Code = reader.GetString(1),
            Name = reader.GetString(2),
            Barcode = reader.GetString(3),
            CategoryId = Guid.Parse(reader.GetString(4)),
            CostPrice = reader.GetDecimal(5),
            SalePrice = reader.GetDecimal(6),
            LowStockThreshold = reader.GetDecimal(7),
            IsActive = reader.GetInt32(8) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(9)),
            UpdatedAt = reader.IsDBNull(10) ? null : DateTime.Parse(reader.GetString(10))
        };
    }

    private static void Validate(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Code))
        {
            throw new InvalidOperationException(Localizer.T("Product.CodeRequired"));
        }

        if (string.IsNullOrWhiteSpace(product.Name))
        {
            throw new InvalidOperationException(Localizer.T("Product.NameRequired"));
        }

        if (string.IsNullOrWhiteSpace(product.Barcode))
        {
            throw new InvalidOperationException(Localizer.T("Product.BarcodeRequired"));
        }

        if (product.CategoryId == Guid.Empty)
        {
            throw new InvalidOperationException(Localizer.T("Product.SelectCategory"));
        }

        if (product.CostPrice < 0 || product.SalePrice < 0 || product.LowStockThreshold < 0)
        {
            throw new InvalidOperationException(Localizer.T("Product.NonNegativeValues"));
        }
    }
}
