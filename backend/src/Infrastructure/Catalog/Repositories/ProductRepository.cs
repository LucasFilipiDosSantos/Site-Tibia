using Application.Catalog.Contracts;
using Domain.Catalog;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Catalog.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _dbContext;

    public ProductRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products.SingleOrDefaultAsync(x => x.Slug == slug, cancellationToken);
    }

    public Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products.AnyAsync(x => x.Slug == slug, cancellationToken);
    }

    public Task<bool> ExistsByCategorySlugAsync(string categorySlug, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products.AnyAsync(x => x.CategorySlug == categorySlug, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> ListAsync(ProductListQuery query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.Products
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.CategorySlug))
        {
            source = source.Where(x => x.CategorySlug == query.CategorySlug);
        }

        if (!string.IsNullOrWhiteSpace(query.Slug))
        {
            source = source.Where(x => x.Slug == query.Slug);
        }

        return await source
            .OrderBy(x => x.Id)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _dbContext.Products.AddAsync(product, cancellationToken);
    }

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _dbContext.Products.Update(product);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
