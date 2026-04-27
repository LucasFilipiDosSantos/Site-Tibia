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

        productRepository.Seed(new Product("Gold Starter", "gold-starter", "Gold product", 5m, Guid.NewGuid(), "gold", "Antica"));
        productRepository.Seed(new Product("Magic Sword", "magic-sword", "Sword product", 9m, Guid.NewGuid(), "items", "Secura"));

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
        productRepository.Seed(new Product("Gold Starter", "gold-starter", "Gold product", 5m, Guid.NewGuid(), "gold", "Antica"));

        var service = new CatalogService(productRepository, categoryRepository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateProductPutReplace(
                new UpdateProductPutReplaceRequest(
                    RouteSlug: "gold-starter",
                    PayloadSlug: "changed-slug",
                    Name: "Gold Starter Updated",
                    Description: "Updated",
                    Price: 7m,
                    CategorySlug: "gold",
                    Server: "Lobera"
                )));
    }

    [Fact]
    public async Task UpdateProductPutReplace_RejectsUnknownCategorySlug()
    {
        var categoryRepository = new InMemoryCategoryRepository();
        var productRepository = new InMemoryProductRepository();

        categoryRepository.Seed(new Category("Gold", "gold", "Gold offers"));
        productRepository.Seed(new Product("Gold Starter", "gold-starter", "Gold product", 5m, Guid.NewGuid(), "gold", "Antica"));

        var service = new CatalogService(productRepository, categoryRepository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateProductPutReplace(
                new UpdateProductPutReplaceRequest(
                    RouteSlug: "gold-starter",
                    PayloadSlug: "gold-starter",
                    Name: "Gold Starter Updated",
                    Description: "Updated",
                    Price: 7m,
                    CategorySlug: "unknown",
                    Server: "Lobera"
                )));
    }

    [Fact]
    public async Task CreateProduct_AllowsMultipleProductsWithSameCategorySlug()
    {
        var categoryRepository = new InMemoryCategoryRepository();
        var productRepository = new InMemoryProductRepository();

        categoryRepository.Seed(new Category("Coin", "coin", "Coin offers"));

        var service = new CatalogService(productRepository, categoryRepository);

        var first = await service.CreateProduct(
            new CreateProductRequest("100kk Aurera", "100kk-aurera", "100kk package", 100m, "coin", "Lobera"));
        var second = await service.CreateProduct(
            new CreateProductRequest("250kk Aurera", "250kk-aurera", "250kk package", 250m, "coin", "Lobera"));

        Assert.Equal("100kk-aurera", first.Slug);
        Assert.Equal("250kk-aurera", second.Slug);
        Assert.Equal("coin", first.CategorySlug);
        Assert.Equal("coin", second.CategorySlug);
    }

    [Fact]
    public async Task CreateProduct_AllowsSameImageUrlAcrossDifferentProducts()
    {
        var categoryRepository = new InMemoryCategoryRepository();
        var productRepository = new InMemoryProductRepository();
        const string imageUrl = "https://cf.shopee.com.br/file/1f9d46cd71e704ba6bcb1f7058360177";

        categoryRepository.Seed(new Category("Coin", "coin", "Coin offers"));

        var service = new CatalogService(productRepository, categoryRepository);

        var first = await service.CreateProduct(
            new CreateProductRequest("100kk Aurera", "100kk-aurera", "100kk package", 100m, "coin", "Lobera", imageUrl));
        var second = await service.CreateProduct(
            new CreateProductRequest("250kk Aurera", "250kk-aurera", "250kk package", 250m, "coin", "Lobera", imageUrl));

        Assert.Equal(imageUrl, first.ImageUrl);
        Assert.Equal(imageUrl, second.ImageUrl);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task UpdateProductPutReplace_AllowsKeepingSameImageUrlOnSameProduct()
    {
        var categoryRepository = new InMemoryCategoryRepository();
        var productRepository = new InMemoryProductRepository();
        const string imageUrl = "https://cf.shopee.com.br/file/1f9d46cd71e704ba6bcb1f7058360177";

        categoryRepository.Seed(new Category("Gold", "gold", "Gold offers"));
        productRepository.Seed(new Product("Gold Starter", "gold-starter", "Gold product", 5m, Guid.NewGuid(), "gold", "Antica", imageUrl: imageUrl));

        var service = new CatalogService(productRepository, categoryRepository);

        var updated = await service.UpdateProductPutReplace(
            new UpdateProductPutReplaceRequest(
                RouteSlug: "gold-starter",
                PayloadSlug: "gold-starter",
                Name: "Gold Starter Updated",
                Description: "Updated",
                Price: 7m,
                CategorySlug: "gold",
                Server: "Lobera",
                ImageUrl: imageUrl));

        Assert.Equal(imageUrl, updated.ImageUrl);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://cdn.example.com/products/shared.png")]
    [InlineData("/images/shared.png")]
    [InlineData("data:image/jpeg;base64,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")]
    public async Task ProductMutations_InvalidImageUrl_AreRejected(string imageUrl)
    {
        var categoryRepository = new InMemoryCategoryRepository();
        var productRepository = new InMemoryProductRepository();

        categoryRepository.Seed(new Category("Gold", "gold", "Gold offers"));
        productRepository.Seed(new Product("Gold Starter", "gold-starter", "Gold product", 5m, Guid.NewGuid(), "gold", "Antica"));

        var service = new CatalogService(productRepository, categoryRepository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateProduct(new CreateProductRequest("Gold Plus", "gold-plus", "Gold plus", 10m, "gold", "Lobera", imageUrl)));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateProductPutReplace(
                new UpdateProductPutReplaceRequest(
                    RouteSlug: "gold-starter",
                    PayloadSlug: "gold-starter",
                    Name: "Gold Starter Updated",
                    Description: "Updated",
                    Price: 7m,
                    CategorySlug: "gold",
                    Server: "Lobera",
                    ImageUrl: imageUrl)));
    }

    [Fact]
    public async Task ProductMutations_PersistServerAcrossCreateAndUpdate()
    {
        var categoryRepository = new InMemoryCategoryRepository();
        var productRepository = new InMemoryProductRepository();

        categoryRepository.Seed(new Category("Gold", "gold", "Gold offers"));

        var service = new CatalogService(productRepository, categoryRepository);

        var created = await service.CreateProduct(
            new CreateProductRequest("Gold Plus", "gold-plus", "Gold plus", 10m, "gold", "Lobera"));

        Assert.Equal("Lobera", created.Server);

        var updated = await service.UpdateProductPutReplace(
            new UpdateProductPutReplaceRequest(
                RouteSlug: "gold-plus",
                PayloadSlug: "gold-plus",
                Name: "Gold Plus",
                Description: "Gold plus updated",
                Price: 11m,
                CategorySlug: "gold",
                Server: "Yovera"));

        Assert.Equal("Yovera", updated.Server);
    }

    [Fact]
    public async Task DeleteProduct_HidesProductInsteadOfRemovingIt()
    {
        var categoryRepository = new InMemoryCategoryRepository();
        var productRepository = new InMemoryProductRepository();
        var product = new Product("Gold Starter", "gold-starter", "Gold product", 5m, Guid.NewGuid(), "gold", "Antica");
        productRepository.Seed(product);

        var service = new CatalogService(productRepository, categoryRepository);

        await service.DeleteProduct("gold-starter");
        var list = await service.ListProducts(new ListProductsRequest(Page: 1, PageSize: 10));

        Assert.True(product.IsHidden);
        Assert.Empty(list.Items);
        Assert.True(await productRepository.ExistsBySlugAsync("gold-starter"));
    }

    [Fact]
    public async Task DeleteProduct_WhenIdentifierIsProductId_HidesProduct()
    {
        var categoryRepository = new InMemoryCategoryRepository();
        var productRepository = new InMemoryProductRepository();
        var product = new Product("Gold Starter", "gold-starter", "Gold product", 5m, Guid.NewGuid(), "gold", "Antica");
        productRepository.Seed(product);

        var service = new CatalogService(productRepository, categoryRepository);

        await service.DeleteProduct(product.Id.ToString());

        Assert.True(product.IsHidden);
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

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_products.SingleOrDefault(p => p.Id == id));
        }

        public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_products.SingleOrDefault(p => p.Slug == slug));
        }

        public Task<CatalogProductProjection?> GetCatalogBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            var product = _products.SingleOrDefault(p => p.Slug == slug && !p.IsHidden);
            return Task.FromResult(product is null ? null : new CatalogProductProjection(product, AvailableStock: 10));
        }

        public Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_products.Any(p => p.Slug == slug));
        }

        public Task<bool> ExistsByCategorySlugAsync(string categorySlug, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_products.Any(p => p.CategorySlug == categorySlug));
        }

        public Task<bool> HasProtectedReferencesAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<IReadOnlyList<Product>> ListAsync(ProductListQuery query, CancellationToken cancellationToken = default)
        {
            LastListQuery = query;

            IEnumerable<Product> filtered = _products.Where(p => !p.IsHidden);
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

        public async Task<IReadOnlyList<CatalogProductProjection>> ListCatalogAsync(ProductListQuery query, CancellationToken cancellationToken = default)
        {
            var products = await ListAsync(query, cancellationToken);
            return products.Select(product => new CatalogProductProjection(product, AvailableStock: 10)).ToList();
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

        public Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
        {
            product.Hide();
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
