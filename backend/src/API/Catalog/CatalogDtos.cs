namespace API.Catalog;

public sealed record ProductListQueryRequest(string? Category, string? Slug, int Page = 1, int PageSize = 20);

public sealed record ProductResponse(string Name, string Slug, string Description, decimal Price, string CategorySlug);

public sealed record ProductListResponse(IReadOnlyList<ProductResponse> Items, int Page, int PageSize);

public sealed record CreateCategoryRequest(string Name, string Slug, string Description);

public sealed record CreateProductRequest(string Name, string Slug, string Description, decimal Price, string CategorySlug);

public sealed record UpdateProductPutReplaceRequest(string Slug, string Name, string Description, decimal Price, string CategorySlug);
