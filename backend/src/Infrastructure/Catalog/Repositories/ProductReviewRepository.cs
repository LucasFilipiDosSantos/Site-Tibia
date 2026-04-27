using Application.Catalog.Contracts;
using Domain.Catalog;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Catalog.Repositories;

public sealed class ProductReviewRepository : IProductReviewRepository
{
    private readonly AppDbContext _dbContext;

    public ProductReviewRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProductReview?> GetByUserAndProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductReviews
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductReview>> ListByProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductReviews
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProductReview review, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProductReviews.AddAsync(review, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
