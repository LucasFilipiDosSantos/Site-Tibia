using Application.Catalog.Contracts;
using Domain.Catalog;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Catalog.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _dbContext;

    public ProductRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products.SingleOrDefaultAsync(x => x.Slug == slug, cancellationToken);
    }

    public async Task<CatalogProductProjection?> GetCatalogBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await (
                from product in _dbContext.Products.AsNoTracking()
                join stock in _dbContext.InventoryStocks.AsNoTracking()
                    on product.Id equals stock.ProductId into stockJoin
                from stock in stockJoin.DefaultIfEmpty()
                join review in _dbContext.ProductReviews.AsNoTracking()
                    on product.Id equals review.ProductId into reviewJoin
                where product.Slug == slug && !product.IsHidden
                select new
                {
                    Product = product,
                    AvailableStock = stock == null ? 0 : stock.TotalQuantity - stock.ReservedQuantity,
                    AverageRating = reviewJoin.Select(x => (decimal?)x.Rating).Average() ?? 0m,
                    ReviewCount = reviewJoin.Count()
                })
                .SingleOrDefaultAsync(cancellationToken);

            return result is null
                ? null
                : new CatalogProductProjection(result.Product, result.AvailableStock, result.AverageRating, result.ReviewCount);
        }
        catch (PostgresException ex) when (IsMissingProductReviewsTable(ex))
        {
            var result = await (
                from product in _dbContext.Products.AsNoTracking()
                join stock in _dbContext.InventoryStocks.AsNoTracking()
                    on product.Id equals stock.ProductId into stockJoin
                from stock in stockJoin.DefaultIfEmpty()
                where product.Slug == slug && !product.IsHidden
                select new CatalogProductProjection(
                    product,
                    stock == null ? 0 : stock.TotalQuantity - stock.ReservedQuantity,
                    0m,
                    0))
                .SingleOrDefaultAsync(cancellationToken);

            return result;
        }
    }

    public Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products.AnyAsync(x => x.Slug == slug, cancellationToken);
    }

    public Task<bool> ExistsByCategorySlugAsync(string categorySlug, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products.AnyAsync(x => x.CategorySlug == categorySlug, cancellationToken);
    }

    public async Task<bool> HasProtectedReferencesAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrderItemSnapshots.AnyAsync(x => x.ProductId == productId, cancellationToken)
            || await _dbContext.DeliveryInstructions.AnyAsync(x => x.ProductId == productId, cancellationToken)
            || await _dbContext.InventoryReservations.AnyAsync(x => x.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> ListAsync(ProductListQuery query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.Products
            .AsNoTracking()
            .Where(x => !x.IsHidden)
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

    public async Task<IReadOnlyList<CatalogProductProjection>> ListCatalogAsync(ProductListQuery query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.Products
            .AsNoTracking()
            .Where(x => !x.IsHidden)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.CategorySlug))
        {
            source = source.Where(x => x.CategorySlug == query.CategorySlug);
        }

        if (!string.IsNullOrWhiteSpace(query.Slug))
        {
            source = source.Where(x => x.Slug == query.Slug);
        }

        try
        {
            return await (
                from product in source
                join stock in _dbContext.InventoryStocks.AsNoTracking()
                    on product.Id equals stock.ProductId into stockJoin
                from stock in stockJoin.DefaultIfEmpty()
                join review in _dbContext.ProductReviews.AsNoTracking()
                    on product.Id equals review.ProductId into reviewJoin
                orderby product.Id
                select new CatalogProductProjection(
                    product,
                    stock == null ? 0 : stock.TotalQuantity - stock.ReservedQuantity,
                    reviewJoin.Select(x => (decimal?)x.Rating).Average() ?? 0m,
                    reviewJoin.Count()))
                .Skip(query.Offset)
                .Take(query.Limit)
                .ToListAsync(cancellationToken);
        }
        catch (PostgresException ex) when (IsMissingProductReviewsTable(ex))
        {
            return await (
                from product in source
                join stock in _dbContext.InventoryStocks.AsNoTracking()
                    on product.Id equals stock.ProductId into stockJoin
                from stock in stockJoin.DefaultIfEmpty()
                orderby product.Id
                select new CatalogProductProjection(
                    product,
                    stock == null ? 0 : stock.TotalQuantity - stock.ReservedQuantity,
                    0m,
                    0))
                .Skip(query.Offset)
                .Take(query.Limit)
                .ToListAsync(cancellationToken);
        }
    }

    private static bool IsMissingProductReviewsTable(PostgresException ex)
        => ex.SqlState == PostgresErrorCodes.UndefinedTable
           && (string.Equals(ex.TableName, "product_reviews", StringComparison.Ordinal)
               || ex.MessageText.Contains("product_reviews", StringComparison.OrdinalIgnoreCase));

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _dbContext.Products.AddAsync(product, cancellationToken);
    }

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _dbContext.Products.Update(product);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
    {
        product.Hide();
        _dbContext.Products.Update(product);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
