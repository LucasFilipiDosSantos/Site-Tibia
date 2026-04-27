using Application.Catalog.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Catalog;

using ApiCatalog = API.Catalog;

[Trait("Category", "CatalogGovernance")]
[Trait("Requirement", "CAT-02")]
[Trait("Requirement", "CAT-03")]
[Trait("Suite", "Phase02CatalogApi")]
[Trait("Plan", "02-03")]
public sealed class CatalogCustomerEndpointsTests
{
    [Fact]
    public async Task GetProducts_WithCategoryAndSlug_AppliesAndFilters()
    {
        await using var factory = new CatalogCustomerApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/products?category=gold&slug=gold-pro&page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiCatalog.ProductListResponse>();
        Assert.NotNull(payload);
        var product = Assert.Single(payload!.Items);
        Assert.Equal("gold-pro", product.Slug);
        Assert.Equal("gold", product.CategorySlug);
    }

    [Fact]
    public async Task GetProductBySlug_ReturnsCanonicalProduct()
    {
        await using var factory = new CatalogCustomerApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/products/gold-starter");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiCatalog.ProductResponse>();
        Assert.NotNull(payload);
        Assert.Equal("gold-starter", payload!.Slug);
        Assert.Equal("Gold Starter", payload.Name);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    public async Task GetProducts_InvalidPagination_ReturnsProblemDetails400(int page, int pageSize)
    {
        await using var factory = new CatalogCustomerApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/products?page={page}&pageSize={pageSize}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public void CatalogDtos_File_IsSubstantiveForContractSurface()
    {
        var dtoFile = FindRepoPath("src/API/Catalog/CatalogDtos.cs");
        var lineCount = File.ReadAllLines(dtoFile).Length;

        Assert.True(
            lineCount >= 80,
            $"CatalogDtos.cs must be substantive (>= 80 lines). Current line count: {lineCount}.");
    }

    [Fact]
    public void CustomerTests_DoNotDuplicateApiCatalogDtoContracts()
    {
        var currentTestFile = FindRepoPath("tests/IntegrationTests/Catalog/CatalogCustomerEndpointsTests.cs");
        var source = File.ReadAllText(currentTestFile);

        var hasLocalDuplicateDtoRecords = Regex.IsMatch(
            source,
            @"^\s*private\s+sealed\s+record\s+Product(ListResponse|Response)\b",
            RegexOptions.Multiline);

        Assert.False(hasLocalDuplicateDtoRecords, "Customer tests must not declare local ProductListResponse/ProductResponse records.");
    }

    private sealed class CatalogCustomerApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private readonly InMemoryProductRepository _products = new();
        private readonly InMemoryCategoryRepository _categories = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(
                [
                    new KeyValuePair<string, string?>("Jwt:Issuer", "tibia-webstore"),
                    new KeyValuePair<string, string?>("Jwt:Audience", "tibia-webstore-client"),
                    new KeyValuePair<string, string?>("Jwt:SigningKey", "01234567890123456789012345678901"),
                    new KeyValuePair<string, string?>("IdentityTokenDelivery:Provider", "inmemory")
                ]);
            });

            builder.ConfigureServices(services =>
            {
                _products.Seed(
                [
                    new Domain.Catalog.Product("Gold Starter", "gold-starter", "Starter pack", 5m, Guid.NewGuid(), "gold"),
                    new Domain.Catalog.Product("Gold Pro", "gold-pro", "Pro pack", 10m, Guid.NewGuid(), "gold"),
                    new Domain.Catalog.Product("Magic Sword", "magic-sword", "Sword", 20m, Guid.NewGuid(), "items")
                ]);

                services.RemoveAll<IProductRepository>();
                services.RemoveAll<ICategoryRepository>();
                services.AddSingleton<IProductRepository>(_products);
                services.AddSingleton<ICategoryRepository>(_categories);
            });
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
        }
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly List<Domain.Catalog.Product> _items = [];

        public void Seed(IEnumerable<Domain.Catalog.Product> products)
        {
            _items.Clear();
            _items.AddRange(products);
        }

        public Task<Domain.Catalog.Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.SingleOrDefault(x => x.Slug == slug));

        public Task<Domain.Catalog.Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.SingleOrDefault(x => x.Id == id));

        public Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Any(x => x.Slug == slug));

        public Task<bool> ExistsByCategorySlugAsync(string categorySlug, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Any(x => x.CategorySlug == categorySlug));

        public Task<bool> HasProtectedReferencesAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<CatalogProductProjection?> GetCatalogBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            var product = _items.SingleOrDefault(x => x.Slug == slug && !x.IsHidden);
            return Task.FromResult(product is null ? null : new CatalogProductProjection(product, AvailableStock: 10, AverageRating: 0m, ReviewCount: 0));
        }

        public Task<IReadOnlyList<Domain.Catalog.Product>> ListAsync(ProductListQuery query, CancellationToken cancellationToken = default)
        {
            var source = _items.Where(x => !x.IsHidden);
            if (!string.IsNullOrWhiteSpace(query.CategorySlug))
            {
                source = source.Where(x => x.CategorySlug == query.CategorySlug);
            }

            if (!string.IsNullOrWhiteSpace(query.Slug))
            {
                source = source.Where(x => x.Slug == query.Slug);
            }

            return Task.FromResult<IReadOnlyList<Domain.Catalog.Product>>(source.Skip(query.Offset).Take(query.Limit).ToList());
        }

        public Task AddAsync(Domain.Catalog.Product product, CancellationToken cancellationToken = default)
        {
            _items.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Domain.Catalog.Product product, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Domain.Catalog.Product product, CancellationToken cancellationToken = default)
        {
            product.Hide();
            return Task.CompletedTask;
        }

        public async Task<IReadOnlyList<CatalogProductProjection>> ListCatalogAsync(ProductListQuery query, CancellationToken cancellationToken = default)
        {
            var products = await ListAsync(query, cancellationToken);
            return products.Select(product => new CatalogProductProjection(product, AvailableStock: 10, AverageRating: 0m, ReviewCount: 0)).ToList();
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryCategoryRepository : ICategoryRepository
    {
        public Task<Domain.Catalog.Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult<Domain.Catalog.Category?>(null);

        public Task AddAsync(Domain.Catalog.Category category, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Domain.Catalog.Category category, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static string FindRepoPath(string relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException($"Could not locate repository file: {relativePath}");
    }
}
