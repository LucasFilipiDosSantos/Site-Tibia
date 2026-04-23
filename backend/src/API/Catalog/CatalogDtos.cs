namespace API.Catalog;

/// <summary>
/// Query contract for customer catalog list requests.
/// </summary>
public sealed record ProductListQueryRequest
{
    /// <summary>
    /// Optional category slug filter.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Optional product slug filter.
    /// </summary>
    public string? Slug { get; init; }

    /// <summary>
    /// 1-based page index.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Page size for offset pagination.
    /// </summary>
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Route contract for customer canonical slug lookup.
/// </summary>
public sealed record ProductSlugRouteRequest(string Slug);

/// <summary>
/// Route contract for admin category deletion by slug.
/// </summary>
public sealed record CategorySlugRouteRequest(string Slug);

/// <summary>
/// Item contract in customer list responses.
/// </summary>
public sealed record ProductListItemResponse(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    string CategorySlug,
    string? ImageUrl,
    string Server,
    int AvailableStock,
    decimal Rating,
    int SalesCount);

/// <summary>
/// Customer/admin product response contract.
/// </summary>
public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    string CategorySlug,
    string? ImageUrl,
    string Server,
    int AvailableStock,
    decimal Rating,
    int SalesCount);

/// <summary>
/// Echo of effective list filters used to produce response items.
/// </summary>
public sealed record ProductListAppliedFiltersResponse(
    string? Category,
    string? Slug);

/// <summary>
/// Pagination metadata for list responses.
/// </summary>
public sealed record ProductListPaginationResponse(
    int Page,
    int PageSize,
    bool HasPreviousPage,
    bool HasNextPage);

/// <summary>
/// Customer list response contract with items and metadata.
/// </summary>
public sealed record ProductListResponse(
    IReadOnlyList<ProductListItemResponse> Items,
    int Page,
    int PageSize,
    ProductListAppliedFiltersResponse AppliedFilters,
    ProductListPaginationResponse Pagination);

/// <summary>
/// Customer category response contract.
/// </summary>
public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    string Description);

/// <summary>
/// Admin category create payload.
/// </summary>
public sealed record CreateCategoryRequest(string Name, string Slug, string Description);

/// <summary>
/// Admin product create payload.
/// </summary>
public sealed record CreateProductRequest(
    string Name,
    string Slug,
    string Description,
    decimal Price,
    string CategorySlug,
    string? ImageUrl = null);

/// <summary>
/// Admin product full-replace payload.
/// </summary>
public sealed record UpdateProductPutReplaceRequest(
    string Slug,
    string Name,
    string Description,
    decimal Price,
    string CategorySlug,
    string? ImageUrl = null);
