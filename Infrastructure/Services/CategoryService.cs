using Microsoft.Data.Sqlite;
using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;
using POS_system_cs.Infrastructure.Persistence;

namespace POS_system_cs.Infrastructure.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public CategoryService(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = new List<Category>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, name, description, is_active, created_at, updated_at
            FROM categories
            ORDER BY is_active DESC, name;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            categories.Add(ReadCategory(reader));
        }

        return categories;
    }

    public async Task SaveAsync(Category category, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category.Name);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var exists = await ExistsAsync(connection, category.Id, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = exists
            ? """
              UPDATE categories
              SET name = $name,
                  description = $description,
                  is_active = $isActive,
                  updated_at = $updatedAt
              WHERE id = $id;
              """
            : """
              INSERT INTO categories (id, name, description, is_active, created_at, updated_at)
              VALUES ($id, $name, $description, $isActive, $createdAt, $updatedAt);
              """;

        command.Parameters.AddWithValue("$id", category.Id.ToString());
        command.Parameters.AddWithValue("$name", category.Name.Trim());
        command.Parameters.AddWithValue("$description", DbValue(category.Description));
        command.Parameters.AddWithValue("$isActive", category.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$createdAt", category.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using (var checkCommand = connection.CreateCommand())
        {
            checkCommand.CommandText = "SELECT COUNT(1) FROM products WHERE category_id = $id;";
            checkCommand.Parameters.AddWithValue("$id", id.ToString());
            var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync(cancellationToken));
            if (count > 0)
            {
                throw new InvalidOperationException("该分类已关联商品，不能删除。");
            }
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM categories WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> ExistsAsync(SqliteConnection connection, Guid id, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM categories WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private static Category ReadCategory(SqliteDataReader reader)
    {
        return new Category
        {
            Id = Guid.Parse(reader.GetString(0)),
            Name = reader.GetString(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            IsActive = reader.GetInt32(3) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(4)),
            UpdatedAt = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5))
        };
    }

    private static object DbValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
    }
}
