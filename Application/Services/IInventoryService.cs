using POS_system_cs.Application.Models;
using POS_system_cs.Domain.Entities;

namespace POS_system_cs.Application.Services;

public interface IInventoryService
{
    Task<IReadOnlyList<StockOverview>> GetOverviewAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockItem>> GetLowStockItemsAsync(CancellationToken cancellationToken = default);

    Task SetStockAsync(Guid productId, decimal quantity, CancellationToken cancellationToken = default);

    Task AdjustStockAsync(Guid productId, decimal quantityDelta, string reason, CancellationToken cancellationToken = default);
}
