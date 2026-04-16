using Domain.Catalog;

namespace Application.Catalog.Contracts;

public interface ICategoryRepository
{
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    Task DeleteAsync(Category category, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
