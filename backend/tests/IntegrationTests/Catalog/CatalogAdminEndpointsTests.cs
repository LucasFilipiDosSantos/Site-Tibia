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
using System.Text.RegularExpressions;

namespace IntegrationTests.Catalog;

using ApiCatalog = API.Catalog;

[Trait("Category", "CatalogGovernance")]
[Trait("Requirement", "CAT-04")]
[Trait("Suite", "Phase02CatalogApi")]
[Trait("Plan", "02-03")]
public sealed class CatalogAdminEndpointsTests
{
    [Fact]
    public void AdminTests_DoNotUseAnonymousPayloads_ForCatalogContracts()
    {
        var currentTestFile = FindRepoPath("tests/IntegrationTests/Catalog/CatalogAdminEndpointsTests.cs");
        var source = File.ReadAllText(currentTestFile);

        var hasAnonymousPayloads = Regex.IsMatch(
            source,
            @"PostAsJsonAsync\s*\([^\)]*new\s*\{|PutAsJsonAsync\s*\([^\)]*new\s*\{",
            RegexOptions.Singleline);

        Assert.False(hasAnonymousPayloads, "Admin tests must send typed API catalog DTO payloads, not anonymous objects.");
    }

    [Fact]
    public async Task AdminMutationRoutes_UnauthenticatedAndNonAdmin_AreRejected()
    {
        await using var factory = new CatalogAdminApiFactory();
        using var unauthenticatedClient = factory.CreateClient();

        var unauthorized = await unauthenticatedClient.PostAsJsonAsync(
            "/admin/catalog/categories",
            new ApiCatalog.CreateCategoryRequest("Services", "services", "Service offers"));
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        using var nonAdminClient = factory.CreateClient();
        nonAdminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BuildJwt(ApiFactorySettings.Issuer, ApiFactorySettings.Audience, ApiFactorySettings.SigningKey, "Customer"));

        var forbidden = await nonAdminClient.PostAsJsonAsync(
            "/admin/catalog/categories",
            new ApiCatalog.CreateCategoryRequest("Services", "services", "Service offers"));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task AdminMutationRoutes_AdminUser_HasHappyPath2xx()
    {
        await using var factory = new CatalogAdminApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BuildJwt(ApiFactorySettings.Issuer, ApiFactorySettings.Audience, ApiFactorySettings.SigningKey, "Admin"));

        var createCategory = await client.PostAsJsonAsync(
            "/admin/catalog/categories",
            new ApiCatalog.CreateCategoryRequest("Services", "services", "Service offers"));
        Assert.True((int)createCategory.StatusCode >= 200 && (int)createCategory.StatusCode < 300);

        var createProduct = await client.PostAsJsonAsync(
            "/admin/catalog/products",
            new ApiCatalog.CreateProductRequest("Boost", "boost", "Boost service", 0m, "services"));
        Assert.True((int)createProduct.StatusCode >= 200 && (int)createProduct.StatusCode < 300);

        var payload = await createProduct.Content.ReadFromJsonAsync<ApiCatalog.ProductResponse>();
        Assert.NotNull(payload);
        Assert.Equal("boost", payload!.Slug);
        Assert.Equal("services", payload.CategorySlug);
    }

    [Fact]
    public async Task PutProduct_RejectsSlugMutation_AndAcceptsZeroPrice()
    {
        await using var factory = new CatalogAdminApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BuildJwt(ApiFactorySettings.Issuer, ApiFactorySettings.Audience, ApiFactorySettings.SigningKey, "Admin"));

        var createCategory = await client.PostAsJsonAsync(
            "/admin/catalog/categories",
            new ApiCatalog.CreateCategoryRequest("Gold", "gold", "Gold offers"));
        Assert.True((int)createCategory.StatusCode >= 200 && (int)createCategory.StatusCode < 300);

        var createProduct = await client.PostAsJsonAsync(
            "/admin/catalog/products",
            new ApiCatalog.CreateProductRequest("Gold Starter", "gold-starter", "Starter", 10m, "gold"));
        Assert.True((int)createProduct.StatusCode >= 200 && (int)createProduct.StatusCode < 300);

        var slugMutation = await client.PutAsJsonAsync(
            "/admin/catalog/products/gold-starter",
            new ApiCatalog.UpdateProductPutReplaceRequest("gold-changed", "Gold Starter", "Starter", 10m, "gold"));
        Assert.Equal(HttpStatusCode.BadRequest, slugMutation.StatusCode);

        var zeroPriceUpdate = await client.PutAsJsonAsync(
            "/admin/catalog/products/gold-starter",
            new ApiCatalog.UpdateProductPutReplaceRequest("gold-starter", "Gold Starter", "Starter", 0m, "gold"));
        Assert.True((int)zeroPriceUpdate.StatusCode >= 200 && (int)zeroPriceUpdate.StatusCode < 300);
    }

    [Fact]
    public async Task ProductMutations_UnknownCategorySlug_Returns400()
    {
        await using var factory = new CatalogAdminApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BuildJwt(ApiFactorySettings.Issuer, ApiFactorySettings.Audience, ApiFactorySettings.SigningKey, "Admin"));

        var create = await client.PostAsJsonAsync(
            "/admin/catalog/products",
            new ApiCatalog.CreateProductRequest("Ghost", "ghost", "Ghost", 1m, "missing"));
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);

        await client.PostAsJsonAsync(
            "/admin/catalog/categories",
            new ApiCatalog.CreateCategoryRequest("Gold", "gold", "Gold offers"));
        await client.PostAsJsonAsync(
            "/admin/catalog/products",
            new ApiCatalog.CreateProductRequest("Gold Starter", "gold-starter", "Starter", 2m, "gold"));

        var update = await client.PutAsJsonAsync(
            "/admin/catalog/products/gold-starter",
            new ApiCatalog.UpdateProductPutReplaceRequest("gold-starter", "Gold Starter", "Starter", 2m, "missing"));
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithLinkedProducts_IsBlocked()
    {
        await using var factory = new CatalogAdminApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BuildJwt(ApiFactorySettings.Issuer, ApiFactorySettings.Audience, ApiFactorySettings.SigningKey, "Admin"));

        await client.PostAsJsonAsync(
            "/admin/catalog/categories",
            new ApiCatalog.CreateCategoryRequest("Items", "items", "Item offers"));
        await client.PostAsJsonAsync(
            "/admin/catalog/products",
            new ApiCatalog.CreateProductRequest("Sword", "sword", "Sword", 3m, "items"));

        var delete = await client.DeleteAsync("/admin/catalog/categories/items");
        Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
    }

    private static string BuildJwt(string issuer, string audience, string signingKey, string role)
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString()),
            new("email", "admin@test.com"),
            new("role", role),
            new("email_verified", "true")
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
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

    private sealed class CatalogAdminApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
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
                    new KeyValuePair<string, string?>("Jwt:Issuer", ApiFactorySettings.Issuer),
                    new KeyValuePair<string, string?>("Jwt:Audience", ApiFactorySettings.Audience),
                    new KeyValuePair<string, string?>("Jwt:SigningKey", ApiFactorySettings.SigningKey),
                    new KeyValuePair<string, string?>("IdentityTokenDelivery:Provider", "inmemory")
                ]);
            });

            builder.ConfigureServices(services =>
            {
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

    private sealed class InMemoryCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> _categories = [];

        public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(_categories.SingleOrDefault(x => x.Slug == slug));

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

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly List<Product> _products = [];

        public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(_products.SingleOrDefault(x => x.Slug == slug));

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_products.SingleOrDefault(x => x.Id == id));

        public Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(_products.Any(x => x.Slug == slug));

        public Task<bool> ExistsByCategorySlugAsync(string categorySlug, CancellationToken cancellationToken = default)
            => Task.FromResult(_products.Any(x => x.CategorySlug == categorySlug));

        public Task<bool> HasProtectedReferencesAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<CatalogProductProjection?> GetCatalogBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            var product = _products.SingleOrDefault(x => x.Slug == slug && !x.IsHidden);
            return Task.FromResult(product is null ? null : new CatalogProductProjection(product, AvailableStock: 10));
        }

        public Task<IReadOnlyList<Product>> ListAsync(ProductListQuery query, CancellationToken cancellationToken = default)
        {
            var source = _products.Where(x => !x.IsHidden);
            if (!string.IsNullOrWhiteSpace(query.CategorySlug))
            {
                source = source.Where(x => x.CategorySlug == query.CategorySlug);
            }

            if (!string.IsNullOrWhiteSpace(query.Slug))
            {
                source = source.Where(x => x.Slug == query.Slug);
            }

            return Task.FromResult<IReadOnlyList<Product>>(source.Skip(query.Offset).Take(query.Limit).ToList());
        }

        public Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            _products.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
        {
            product.Hide();
            return Task.CompletedTask;
        }

        public async Task<IReadOnlyList<CatalogProductProjection>> ListCatalogAsync(ProductListQuery query, CancellationToken cancellationToken = default)
        {
            var products = await ListAsync(query, cancellationToken);
            return products.Select(product => new CatalogProductProjection(product, AvailableStock: 10)).ToList();
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
