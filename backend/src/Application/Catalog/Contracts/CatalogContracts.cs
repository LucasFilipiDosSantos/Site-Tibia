namespace Application.Catalog.Contracts;

public sealed record ListProductsRequest(int Page = 1, int PageSize = 20, string? Category = null, string? Slug = null);

public sealed record ProductSummary(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    string CategorySlug,
    string? ImageUrl,
    string? Server,
    int AvailableStock,
    decimal Rating,
    int ReviewCount,
    int SalesCount);

public sealed record ListProductsResponse(IReadOnlyList<ProductSummary> Items, int Page, int PageSize);

public sealed record ProductBySlugResponse(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    string CategorySlug,
    string? ImageUrl,
    string? Server,
    int AvailableStock,
    decimal Rating,
    int ReviewCount,
    int SalesCount);

public sealed record CreateProductRequest(string Name, string Slug, string Description, decimal Price, string CategorySlug, string? Server, string? ImageUrl = null);

public sealed record UpdateProductPutReplaceRequest(
    string RouteSlug,
    string PayloadSlug,
    string Name,
    string Description,
    decimal Price,
    string CategorySlug,
    string? Server,
    string? ImageUrl = null
);

public sealed record ProductReviewResponse(Guid UserId, Guid ProductId, decimal Rating, string? Comment, DateTimeOffset CreatedAtUtc);

public sealed record CreateProductReviewRequest(string ProductSlug, Guid UserId, decimal Rating, string? Comment);

public sealed class ProductReviewPurchaseRequiredException : InvalidOperationException
{
    public ProductReviewPurchaseRequiredException()
        : base("Você só pode avaliar produtos comprados.")
    {
    }
}

public sealed class DuplicateProductReviewException : InvalidOperationException
{
    public DuplicateProductReviewException()
        : base("Você já avaliou este produto.")
    {
    }
}

public sealed record CreateCategoryRequest(string Name, string Slug, string Description);

public sealed record ProductListQuery(string? CategorySlug, string? Slug, int Offset, int Limit);

public sealed record CatalogProductProjection(
    Domain.Catalog.Product Product,
    int AvailableStock,
    decimal AverageRating,
    int ReviewCount);
