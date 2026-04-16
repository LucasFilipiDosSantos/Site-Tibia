namespace Application.Catalog.Contracts;

public sealed record ListProductsRequest(int Page = 1, int PageSize = 20, string? Category = null, string? Slug = null);

public sealed record ProductSummary(string Name, string Slug, string Description, decimal Price, string CategorySlug);

public sealed record ListProductsResponse(IReadOnlyList<ProductSummary> Items, int Page, int PageSize);

public sealed record ProductBySlugResponse(string Name, string Slug, string Description, decimal Price, string CategorySlug);

public sealed record CreateProductRequest(string Name, string Slug, string Description, decimal Price, string CategorySlug);

public sealed record UpdateProductPutReplaceRequest(
    string RouteSlug,
    string PayloadSlug,
    string Name,
    string Description,
    decimal Price,
    string CategorySlug
);

public sealed record CreateCategoryRequest(string Name, string Slug, string Description);

public sealed record ProductListQuery(string? CategorySlug, string? Slug, int Offset, int Limit);
