using POS_system_cs.Domain.Entities;

namespace POS_system_cs.Application.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(Category category, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
