using POS_system_cs.Domain.Entities;

namespace POS_system_cs.Application.Services;

public interface ICashierService
{
    Task<Order> CheckoutAsync(Order order, CancellationToken cancellationToken = default);
}
