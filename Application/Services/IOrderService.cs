using POS_system_cs.Domain.Entities;

namespace POS_system_cs.Application.Services;

public interface IOrderService
{
    Task<IReadOnlyList<Order>> SearchAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);
}
