using POS_system_cs.Domain.Entities;

namespace POS_system_cs.Application.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> SearchAsync(string keyword, CancellationToken cancellationToken = default);

    Task<Product?> FindByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    Task SaveAsync(Product product, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
