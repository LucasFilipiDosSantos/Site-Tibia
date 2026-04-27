using Domain.Catalog;

namespace Application.Catalog.Contracts;

public interface IProductReviewRepository
{
    Task<IReadOnlyList<ProductReview>> ListByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ProductReview?> GetByUserAndProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task AddAsync(ProductReview review, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
