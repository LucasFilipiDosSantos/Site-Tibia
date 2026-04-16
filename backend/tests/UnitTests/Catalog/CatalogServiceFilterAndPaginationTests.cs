using Application.Catalog.Contracts;
using Application.Catalog.Services;
using Domain.Catalog;

namespace UnitTests.Catalog;

public sealed class CatalogServiceFilterAndPaginationTests
{
    [Fact]
    public async Task ListProducts_WithCategoryAndSlug_UsesAndSemantics()
    {
        var categoryRepository = new InMemoryCategoryRepository();
        var productRepository = new InMemoryProductRepository();

        categoryRepository.Seed(new Category("Gold", "gold", "Gold offers"));
        categoryRepository.Seed(new Category("Items", "items", "Items offers"));

        productRepository.Seed(new Product("Gold Starter", "gold-starter", "Gold product", 5m, "gold"));
        productRepository.Seed(new Product("Magic Sword", "magic-sword", "Sword product", 9m, "items"));

        var service = new CatalogService(productRepository, categoryRepository);
        var request = new ListProductsRequest(Page: 1, PageSize: 10, Category: "gold", Slug: "magic-sword");

        var result = await service.ListProducts(request);

        Assert.Empty(result.Items);
        Assert.NotNull(productRepository.LastListQuery);
        Assert.Equal("gold", productRepository.LastListQuery!.CategorySlug);
        Assert.Equal("magic-sword", productRepository.LastListQuery.Slug);
    }

    [Fact]
    public async Task ListProducts_ComputesOffsetPaginationWithGuardrails()
    {
        var productRepository = new InMemoryProductRepository();
        var service = new CatalogService(productRepository, new InMemoryCategoryRepository());

        var result = await service.ListProducts(new ListProductsRequest(Page: 2, PageSize: 200, Category: null, Slug: null));

        Assert.NotNull(productRepository.LastListQuery);
        Assert.Equal(100, productRepository.LastListQuery!.Offset);
        Assert.Equal(100, productRepository.LastListQuery!.Limit);
        Assert.Equal(2, result.Page);
        Assert.Equal(100, result.PageSize);
    }

    [Fact]
    public async Task ListProducts_RejectsInvalidPage()
    {
        var service = new CatalogService(new InMemoryProductRepository(), new InMemoryCategoryRepository());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.ListProducts(new ListProductsRequest(Page: 0, PageSize: 10, Category: null, Slug: null)));
    }

    [Fact]
    public async Task UpdateProductPutReplace_RejectsSlugMutation()
    {
        var categoryRepository = new InMemoryCategoryRepository();
        var productRepository = new InMemoryProductRepository();

        categoryRepository.Seed(new Category("Gold", "gold", "Gold offers"));
        productRepository.Seed(new Product("Gold Starter", "gold-starter", "Gold product", 5m, "gold"));

        var service = new CatalogService(productRepository, categoryRepository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateProductPutReplace(
                new UpdateProductPutReplaceRequest(
                    RouteSlug: "gold-starter",
                    PayloadSlug: "changed-slug",
                    Name: "Gold Starter Updated",
                    Description: "Updated",
                    Price: 7m,
                    CategorySlug: "gold"
                )));
    }

    [Fact]
    public async Task UpdateProductPutReplace_RejectsUnknownCategorySlug()
    {
        var categoryRepository = new InMemoryCategoryRepository();
        var productRepository = new InMemoryProductRepository();

        categoryRepository.Seed(new Category("Gold", "gold", "Gold offers"));
        productRepository.Seed(new Product("Gold Starter", "gold-starter", "Gold product", 5m, "gold"));

        var service = new CatalogService(productRepository, categoryRepository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateProductPutReplace(
                new UpdateProductPutReplaceRequest(
                    RouteSlug: "gold-starter",
                    PayloadSlug: "gold-starter",
                    Name: "Gold Starter Updated",
                    Description: "Updated",
                    Price: 7m,
                    CategorySlug: "unknown"
                )));
    }

    private sealed class InMemoryCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> _categories = new();

        public void Seed(Category category) => _categories.Add(category);

        public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_categories.SingleOrDefault(c => c.Slug == slug));
        }

        public Task AddAsync(Category category, CancellationToken cancellationToken = default)
        {
            _categories.Add(category);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Category category, CancellationToken cancellationToken = default)
        {
            _categories.Remove(category);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly List<Product> _products = new();

        public ProductListQuery? LastListQuery { get; private set; }

        public void Seed(Product product) => _products.Add(product);

        public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_products.SingleOrDefault(p => p.Slug == slug));
        }

        public Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_products.Any(p => p.Slug == slug));
        }

        public Task<bool> ExistsByCategorySlugAsync(string categorySlug, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_products.Any(p => p.CategorySlug == categorySlug));
        }

        public Task<IReadOnlyList<Product>> ListAsync(ProductListQuery query, CancellationToken cancellationToken = default)
        {
            LastListQuery = query;

            IEnumerable<Product> filtered = _products;
            if (!string.IsNullOrWhiteSpace(query.CategorySlug))
            {
                filtered = filtered.Where(p => p.CategorySlug == query.CategorySlug);
            }

            if (!string.IsNullOrWhiteSpace(query.Slug))
            {
                filtered = filtered.Where(p => p.Slug == query.Slug);
            }

            filtered = filtered.Skip(query.Offset).Take(query.Limit);

            return Task.FromResult<IReadOnlyList<Product>>(filtered.ToList());
        }

        public Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            _products.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
