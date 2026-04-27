using API.Auth;
using Application.Catalog.Contracts;
using Domain.Catalog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;

namespace IntegrationTests.Catalog;

using ApiCatalog = API.Catalog;

public sealed class ProductReviewEndpointsTests
{
    [Fact]
    public async Task PostProductReview_AuthenticatedUser_CanCreateReview_AtExpectedRoute()
    {
        await using var factory = new ProductReviewApiFactory();
        using var client = factory.CreateClient();
        var customerId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BuildJwt(customerId));

        var response = await client.PostAsJsonAsync(
            "/api/products/teste-imagem/reviews",
            new ApiCatalog.CreateProductReviewRequest(4.25m, "texto opcional"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiCatalog.ProductReviewResponse>();
        Assert.NotNull(payload);
        Assert.Equal(customerId, payload!.UserId);
        Assert.Equal(4.25m, payload.Rating);
        Assert.Equal("texto opcional", payload.Comment);
    }

    [Fact]
    public async Task PostProductReview_DuplicateReview_ReturnsHandledError()
    {
        await using var factory = new ProductReviewApiFactory();
        using var client = factory.CreateClient();
        var customerId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BuildJwt(customerId));

        await client.PostAsJsonAsync(
            "/api/products/teste-imagem/reviews",
            new ApiCatalog.CreateProductReviewRequest(4.25m, "primeira"));

        var duplicate = await client.PostAsJsonAsync(
            "/api/products/teste-imagem/reviews",
            new ApiCatalog.CreateProductReviewRequest(5m, "segunda"));

        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        var body = await duplicate.Content.ReadAsStringAsync();
        Assert.Contains("Você já avaliou este produto.", body);
    }

    [Fact]
    public async Task GetProductReviews_ReturnsProductReviewsForExpectedRoute()
    {
        await using var factory = new ProductReviewApiFactory();
        var customerId = Guid.NewGuid();
        factory.Reviews.Seed(new ProductReview(customerId, factory.Product.Id, 3.5m, "review seeded"));

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/products/teste-imagem/reviews");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<ApiCatalog.ProductReviewResponse>>();
        Assert.NotNull(payload);
        var review = Assert.Single(payload!);
        Assert.Equal(customerId, review.UserId);
        Assert.Equal(3.5m, review.Rating);
        Assert.Equal("review seeded", review.Comment);
    }

    private static string BuildJwt(Guid customerId)
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new List<Claim>
        {
            new("sub", customerId.ToString()),
            new("email", "customer@test.com"),
            new("role", "Customer"),
            new("email_verified", "true")
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiFactorySettings.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: ApiFactorySettings.Issuer,
            audience: ApiFactorySettings.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(10).UtcDateTime,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static class ApiFactorySettings
    {
        public const string Issuer = "tibia-webstore";
        public const string Audience = "tibia-webstore-client";
        public const string SigningKey = "01234567890123456789012345678901";
    }

    private sealed class ProductReviewApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private readonly InMemoryProductRepository _products = new();
        private readonly InMemoryCategoryRepository _categories = new();
        public InMemoryProductReviewRepository Reviews { get; } = new();
        public Product Product { get; } = new("Teste Imagem", "teste-imagem", "Produto com review", 10m, Guid.NewGuid(), "items", "Lobera");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(
                [
                    new KeyValuePair<string, string?>("Jwt:Issuer", ApiFactorySettings.Issuer),
                    new KeyValuePair<string, string?>("Jwt:Audience", ApiFactorySettings.Audience),
                    new KeyValuePair<string, string?>("Jwt:SigningKey", ApiFactorySettings.SigningKey),
                    new KeyValuePair<string, string?>("IdentityTokenDelivery:Provider", "inmemory")
                ]);
            });

            builder.ConfigureServices(services =>
            {
                _products.Seed(Product);

                services.RemoveAll<IProductRepository>();
                services.RemoveAll<ICategoryRepository>();
                services.RemoveAll<IProductReviewRepository>();

                services.AddSingleton<IProductRepository>(_products);
                services.AddSingleton<ICategoryRepository>(_categories);
                services.AddSingleton<IProductReviewRepository>(Reviews);
            });
        }
    }

    private sealed class InMemoryProductReviewRepository : IProductReviewRepository
    {
        private readonly List<ProductReview> _reviews = [];

        public void Seed(ProductReview review) => _reviews.Add(review);

        public Task<IReadOnlyList<ProductReview>> ListByProductAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ProductReview>>(_reviews.Where(x => x.ProductId == productId).OrderByDescending(x => x.CreatedAtUtc).ToList());

        public Task<ProductReview?> GetByUserAndProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(_reviews.SingleOrDefault(x => x.UserId == userId && x.ProductId == productId));

        public Task AddAsync(ProductReview review, CancellationToken cancellationToken = default)
        {
            _reviews.Add(review);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryCategoryRepository : ICategoryRepository
    {
        public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult<Category?>(null);

        public Task AddAsync(Category category, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Category category, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly List<Product> _products = [];

        public void Seed(Product product) => _products.Add(product);

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_products.SingleOrDefault(x => x.Id == id));

        public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(_products.SingleOrDefault(x => x.Slug == slug));

        public Task<CatalogProductProjection?> GetCatalogBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            var product = _products.SingleOrDefault(x => x.Slug == slug && !x.IsHidden);
            return Task.FromResult(product is null ? null : new CatalogProductProjection(product, 0, 0m, 0));
        }

        public Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(_products.Any(x => x.Slug == slug));

        public Task<bool> ExistsByCategorySlugAsync(string categorySlug, CancellationToken cancellationToken = default)
            => Task.FromResult(_products.Any(x => x.CategorySlug == categorySlug));

        public Task<bool> HasProtectedReferencesAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<IReadOnlyList<Product>> ListAsync(ProductListQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Product>>(_products);

        public Task<IReadOnlyList<CatalogProductProjection>> ListCatalogAsync(ProductListQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CatalogProductProjection>>(_products.Select(x => new CatalogProductProjection(x, 0, 0m, 0)).ToList());

        public Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            _products.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Product product, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
