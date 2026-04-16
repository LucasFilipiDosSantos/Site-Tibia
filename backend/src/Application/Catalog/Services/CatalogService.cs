using Application.Catalog.Contracts;
using Domain.Catalog;

namespace Application.Catalog.Services;

public sealed class CatalogService
{
    private const int MaxPageSize = 100;

    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CatalogService(IProductRepository productRepository, ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<ProductBySlugResponse?> GetBySlug(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = NormalizeSlugOrNull(slug)
            ?? throw new ArgumentException("Product slug is required.", nameof(slug));

        var product = await _productRepository.GetBySlugAsync(normalizedSlug, cancellationToken);
        if (product is null)
        {
            return null;
        }

        return new ProductBySlugResponse(product.Name, product.Slug, product.Description, product.Price, product.CategorySlug);
    }

    public async Task<ListProductsResponse> ListProducts(ListProductsRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Page), "Page must be greater than or equal to 1.");
        }

        if (request.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request.PageSize), "PageSize must be greater than or equal to 1.");
        }

        var boundedPageSize = Math.Min(request.PageSize, MaxPageSize);
        var query = new ProductListQuery(
            CategorySlug: NormalizeSlugOrNull(request.Category),
            Slug: NormalizeSlugOrNull(request.Slug),
            Offset: (request.Page - 1) * boundedPageSize,
            Limit: boundedPageSize
        );

        var products = await _productRepository.ListAsync(query, cancellationToken);
        var items = products
            .Select(product => new ProductSummary(product.Name, product.Slug, product.Description, product.Price, product.CategorySlug))
            .ToList();

        return new ListProductsResponse(items, request.Page, boundedPageSize);
    }

    public async Task<ProductBySlugResponse> CreateProduct(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = NormalizeSlugOrNull(request.Slug)
            ?? throw new ArgumentException("Product slug is required.", nameof(request.Slug));
        var normalizedCategorySlug = NormalizeSlugOrNull(request.CategorySlug)
            ?? throw new ArgumentException("Category slug is required.", nameof(request.CategorySlug));

        var category = await _categoryRepository.GetBySlugAsync(normalizedCategorySlug, cancellationToken);
        if (category is null)
        {
            throw new ArgumentException("Category slug does not exist.", nameof(request.CategorySlug));
        }

        if (await _productRepository.ExistsBySlugAsync(normalizedSlug, cancellationToken))
        {
            throw new ArgumentException("Product slug already exists.", nameof(request.Slug));
        }

        var product = new Product(request.Name, normalizedSlug, request.Description, request.Price, normalizedCategorySlug);
        await _productRepository.AddAsync(product, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);

        return new ProductBySlugResponse(product.Name, product.Slug, product.Description, product.Price, product.CategorySlug);
    }

    public async Task<ProductBySlugResponse> UpdateProductPutReplace(UpdateProductPutReplaceRequest request, CancellationToken cancellationToken = default)
    {
        var routeSlug = NormalizeSlugOrNull(request.RouteSlug)
            ?? throw new ArgumentException("Route slug is required.", nameof(request.RouteSlug));
        var payloadSlug = NormalizeSlugOrNull(request.PayloadSlug)
            ?? throw new ArgumentException("Payload slug is required.", nameof(request.PayloadSlug));

        if (!string.Equals(routeSlug, payloadSlug, StringComparison.Ordinal))
        {
            throw new ArgumentException("Product slug cannot be changed after creation.", nameof(request.PayloadSlug));
        }

        var normalizedCategorySlug = NormalizeSlugOrNull(request.CategorySlug)
            ?? throw new ArgumentException("Category slug is required.", nameof(request.CategorySlug));

        var category = await _categoryRepository.GetBySlugAsync(normalizedCategorySlug, cancellationToken);
        if (category is null)
        {
            throw new ArgumentException("Category slug does not exist.", nameof(request.CategorySlug));
        }

        var product = await _productRepository.GetBySlugAsync(routeSlug, cancellationToken)
            ?? throw new ArgumentException("Product slug not found.", nameof(request.RouteSlug));

        product.ReplaceDetails(request.Name, request.Description, request.Price, normalizedCategorySlug);
        await _productRepository.UpdateAsync(product, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);

        return new ProductBySlugResponse(product.Name, product.Slug, product.Description, product.Price, product.CategorySlug);
    }

    public async Task CreateCategory(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = new Category(request.Name, request.Slug, request.Description);
        if (await _categoryRepository.GetBySlugAsync(category.Slug, cancellationToken) is not null)
        {
            throw new ArgumentException("Category slug already exists.", nameof(request.Slug));
        }

        await _categoryRepository.AddAsync(category, cancellationToken);
        await _categoryRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCategory(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = NormalizeSlugOrNull(slug)
            ?? throw new ArgumentException("Category slug is required.", nameof(slug));

        var category = await _categoryRepository.GetBySlugAsync(normalizedSlug, cancellationToken)
            ?? throw new ArgumentException("Category slug not found.", nameof(slug));

        if (await _productRepository.ExistsByCategorySlugAsync(normalizedSlug, cancellationToken))
        {
            throw new InvalidOperationException("Cannot delete category with linked products.");
        }

        await _categoryRepository.DeleteAsync(category, cancellationToken);
        await _categoryRepository.SaveChangesAsync(cancellationToken);
    }

    private static string? NormalizeSlugOrNull(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        return slug.Trim().ToLowerInvariant();
    }
}
