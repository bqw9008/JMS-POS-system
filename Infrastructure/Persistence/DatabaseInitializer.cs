using System.IO;
using Microsoft.Data.Sqlite;
using POS_system_cs.UI.Wpf.Localization;

namespace POS_system_cs.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public DatabaseInitializer(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await ExecuteAsync(connection, "PRAGMA foreign_keys = ON;", cancellationToken);
        await ExecuteAsync(connection, LoadSchema(), cancellationToken);
        await SeedDefaultsAsync(connection, cancellationToken);

        if (IsTestDataSeedingEnabled())
        {
            await SeedTestDataAsync(connection, cancellationToken);
        }
    }

    private static async Task SeedDefaultsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var now = DateTime.Now.ToString("O");
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO categories (id, name, description, is_active, created_at)
            SELECT $id, $name, $description, 1, $createdAt
            WHERE NOT EXISTS (SELECT 1 FROM categories);
            """;
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("$name", "默认分类");
        command.Parameters.AddWithValue("$description", "系统初始化分类");
        command.Parameters.AddWithValue("$createdAt", now);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SeedTestDataAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var now = DateTime.Now.ToString("O");
        var categories = new[]
        {
            new { Id = "11111111-1111-1111-1111-111111111111", Name = "饮料", Description = "测试分类：饮料" },
            new { Id = "22222222-2222-2222-2222-222222222222", Name = "食品", Description = "测试分类：食品" },
            new { Id = "33333333-3333-3333-3333-333333333333", Name = "日用品", Description = "测试分类：日用品" }
        };

        foreach (var category in categories)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO categories (id, name, description, is_active, created_at)
                SELECT $id, $name, $description, 1, $createdAt
                WHERE NOT EXISTS (SELECT 1 FROM categories WHERE id = $id OR name = $name);
                """;
            command.Parameters.AddWithValue("$id", category.Id);
            command.Parameters.AddWithValue("$name", category.Name);
            command.Parameters.AddWithValue("$description", category.Description);
            command.Parameters.AddWithValue("$createdAt", now);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        var categoryIds = new Dictionary<string, string>();
        foreach (var category in categories)
        {
            categoryIds[category.Name] = await GetCategoryIdAsync(connection, category.Name, cancellationToken);
        }

        var products = new[]
        {
            new { Id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1", Code = "P1001", Name = "可乐 500ml", Barcode = "6900000000011", CategoryName = "饮料", CostPrice = 2.00M, SalePrice = 3.50M, LowStock = 10M, Stock = 80M },
            new { Id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2", Code = "P1002", Name = "矿泉水 550ml", Barcode = "6900000000028", CategoryName = "饮料", CostPrice = 0.80M, SalePrice = 1.50M, LowStock = 20M, Stock = 120M },
            new { Id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3", Code = "P2001", Name = "方便面", Barcode = "6900000000035", CategoryName = "食品", CostPrice = 2.50M, SalePrice = 4.00M, LowStock = 15M, Stock = 60M },
            new { Id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4", Code = "P2002", Name = "火腿肠", Barcode = "6900000000042", CategoryName = "食品", CostPrice = 1.20M, SalePrice = 2.00M, LowStock = 10M, Stock = 50M },
            new { Id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5", Code = "P3001", Name = "抽纸", Barcode = "6900000000059", CategoryName = "日用品", CostPrice = 3.80M, SalePrice = 6.50M, LowStock = 8M, Stock = 25M }
        };

        foreach (var product in products)
        {
            var categoryId = categoryIds[product.CategoryName];
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = """
                    INSERT INTO products (id, code, name, barcode, category_id, cost_price, sale_price,
                                          low_stock_threshold, is_active, created_at, updated_at)
                    SELECT $id, $code, $name, $barcode, $categoryId, $costPrice, $salePrice,
                           $lowStock, 1, $createdAt, NULL
                    WHERE NOT EXISTS (SELECT 1 FROM products WHERE code = $code OR barcode = $barcode);
                    """;
                command.Parameters.AddWithValue("$id", product.Id);
                command.Parameters.AddWithValue("$code", product.Code);
                command.Parameters.AddWithValue("$name", product.Name);
                command.Parameters.AddWithValue("$barcode", product.Barcode);
                command.Parameters.AddWithValue("$categoryId", categoryId);
                command.Parameters.AddWithValue("$costPrice", product.CostPrice);
                command.Parameters.AddWithValue("$salePrice", product.SalePrice);
                command.Parameters.AddWithValue("$lowStock", product.LowStock);
                command.Parameters.AddWithValue("$createdAt", now);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = """
                    INSERT INTO stock (id, product_id, quantity, last_changed_at, created_at, updated_at)
                    SELECT $id, $productId, $quantity, $now, $now, NULL
                    WHERE NOT EXISTS (SELECT 1 FROM stock WHERE product_id = $productId);
                    """;
                command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("$productId", product.Id);
                command.Parameters.AddWithValue("$quantity", product.Stock);
                command.Parameters.AddWithValue("$now", now);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }

    private static bool IsTestDataSeedingEnabled()
    {
        var value = Environment.GetEnvironmentVariable("POS_SEED_TEST_DATA");
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> GetCategoryIdAsync(SqliteConnection connection, string name, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM categories WHERE name = $name LIMIT 1;";
        command.Parameters.AddWithValue("$name", name);
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken))
            ?? throw new InvalidOperationException(Localizer.Format("Database.TestCategoryNotFound", name));
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string LoadSchema()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Data", "schema.sql"),
            Path.Combine(Environment.CurrentDirectory, "Data", "schema.sql")
        };

        var schemaPath = candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException(Localizer.T("Database.SchemaNotFound"), candidates[0]);

        return File.ReadAllText(schemaPath);
    }
}
