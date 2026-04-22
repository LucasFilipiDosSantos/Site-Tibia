using API.Catalog;
using Application.Catalog.Contracts;
using Application.Inventory.Contracts;
using Domain.Catalog;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.RegularExpressions;

namespace IntegrationTests.Inventory;

using ApiInventory = API.Inventory;

[Trait("Category", "InventoryIntegrity")]
[Trait("Requirement", "INV-01")]
[Trait("Requirement", "INV-02")]
[Trait("Requirement", "INV-03")]
[Trait("Requirement", "INV-04")]
[Trait("Suite", "Phase03InventoryApi")]
[Trait("Plan", "03-03")]
public sealed class InventoryEndpointsTests
{
    [Fact]
    public async Task GetAvailability_ReturnsAvailableReservedAndTotal()
    {
        await using var factory = new InventoryApiFactory();
        factory.Inventory.SeedProduct(InventoryApiFactory.ProductId, total: 12, reserved: 5);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/inventory/{InventoryApiFactory.ProductId}/availability");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiInventory.InventoryAvailabilityResponse>();
        Assert.NotNull(payload);
        Assert.Equal(7, payload!.Available);
        Assert.Equal(5, payload.Reserved);
        Assert.Equal(12, payload.Total);
    }

    [Fact]
    public async Task Reserve_WhenInsufficient_Returns409WithAvailableQuantity()
    {
        await using var factory = new InventoryApiFactory();
        factory.Inventory.SeedProduct(InventoryApiFactory.ProductId, total: 5, reserved: 4);
        using var client = factory.CreateClient();

        var reserveResponse = await client.PostAsJsonAsync(
            "/inventory/reservations",
            new ApiInventory.ReserveInventoryRequest("intent-conflict", Guid.NewGuid(), InventoryApiFactory.ProductId, 3));

        Assert.Equal(HttpStatusCode.Conflict, reserveResponse.StatusCode);
        var details = await reserveResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(details);
        Assert.Equal("Conflict.", details!.Title);

        using var doc = System.Text.Json.JsonDocument.Parse(await reserveResponse.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.TryGetProperty("availableQuantity", out var available));
        Assert.Equal(1, available.GetInt32());
    }

    [Fact]
    public async Task ReleaseReservation_ForOrderCancel_ReleasesImmediately()
    {
        await using var factory = new InventoryApiFactory();
        factory.Inventory.SeedProduct(InventoryApiFactory.ProductId, total: 8, reserved: 0);
        using var client = factory.CreateClient();

        var reserve = await client.PostAsJsonAsync(
            "/inventory/reservations",
            new ApiInventory.ReserveInventoryRequest("intent-release", Guid.NewGuid(), InventoryApiFactory.ProductId, 2));
        Assert.Equal(HttpStatusCode.OK, reserve.StatusCode);

        var release = await client.PostAsJsonAsync(
            "/inventory/reservations/release",
            new ApiInventory.ReleaseInventoryReservationRequest("intent-release", ApiInventory.ReservationReleaseReason.OrderCanceled));

        Assert.Equal(HttpStatusCode.OK, release.StatusCode);
        var releasePayload = await release.Content.ReadFromJsonAsync<ApiInventory.ReleaseInventoryReservationResponse>();
        Assert.NotNull(releasePayload);
        Assert.Equal(2, releasePayload!.ReleasedQuantity);
    }

    [Fact]
    public async Task AdminAdjustment_RequiresAuthenticationAndAdminRole()
    {
        await using var factory = new InventoryApiFactory();
        factory.Inventory.SeedProduct(InventoryApiFactory.ProductId, total: 8, reserved: 0);

        using var unauthenticated = factory.CreateClient();
        var unauthorized = await unauthenticated.PostAsJsonAsync(
            "/admin/inventory/adjustments",
            new ApiInventory.AdminAdjustInventoryRequest(InventoryApiFactory.ProductId, 1, "sync"));
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        using var nonAdmin = factory.CreateClient();
        nonAdmin.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BuildJwt("Customer"));
        var forbidden = await nonAdmin.PostAsJsonAsync(
            "/admin/inventory/adjustments",
            new ApiInventory.AdminAdjustInventoryRequest(InventoryApiFactory.ProductId, 1, "sync"));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task AdminAdjustment_AdminUser_SucceedsAndAvailabilityReflectsDelta()
    {
        await using var factory = new InventoryApiFactory();
        factory.Inventory.SeedProduct(InventoryApiFactory.ProductId, total: 8, reserved: 2);
        using var client = factory.CreateClient();
        var tokenAdminUserId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BuildJwt("Admin", tokenAdminUserId));

        var adjust = await client.PostAsJsonAsync(
            "/admin/inventory/adjustments",
            new ApiInventory.AdminAdjustInventoryRequest(InventoryApiFactory.ProductId, 4, "restock"));

        Assert.Equal(HttpStatusCode.OK, adjust.StatusCode);
        var adjustPayload = await adjust.Content.ReadFromJsonAsync<ApiInventory.AdminAdjustInventoryResponse>();
        Assert.NotNull(adjustPayload);
        Assert.Equal(InventoryApiFactory.ProductId, adjustPayload!.ProductId);
        Assert.Equal(4, adjustPayload.Delta);
        Assert.Equal("restock", adjustPayload.Reason);
        Assert.Equal(tokenAdminUserId, adjustPayload.AdminUserId);
        Assert.Equal(8, adjustPayload.BeforeQuantity);
        Assert.Equal(12, adjustPayload.AfterQuantity);
        Assert.NotEqual(default, adjustPayload.AdjustedAtUtc);

        var availability = await client.GetFromJsonAsync<ApiInventory.InventoryAvailabilityResponse>(
            $"/inventory/{InventoryApiFactory.ProductId}/availability");
        Assert.NotNull(availability);
        Assert.Equal(10, availability!.Available);
        Assert.Equal(2, availability.Reserved);
        Assert.Equal(12, availability.Total);
    }

    [Fact]
    public async Task OversellAttempt_ReturnsDeterministic409WithAvailableQuantity()
    {
        await using var factory = new InventoryApiFactory();
        factory.Inventory.SeedProduct(InventoryApiFactory.ProductId, total: 3, reserved: 0);
        using var client = factory.CreateClient();

        var first = await client.PostAsJsonAsync(
            "/inventory/reservations",
            new ApiInventory.ReserveInventoryRequest("intent-first", Guid.NewGuid(), InventoryApiFactory.ProductId, 2));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PostAsJsonAsync(
            "/inventory/reservations",
            new ApiInventory.ReserveInventoryRequest("intent-second", Guid.NewGuid(), InventoryApiFactory.ProductId, 2));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var json = await second.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("availableQuantity", out var available));
        Assert.Equal(1, available.GetInt32());
    }

    [Fact]
    public void InventoryTests_DoNotUseAnonymousPayloads_ForInventoryContracts()
    {
        var currentTestFile = FindRepoPath("tests/IntegrationTests/Inventory/InventoryEndpointsTests.cs");
        var source = File.ReadAllText(currentTestFile);

        var hasAnonymousPayloads = Regex.IsMatch(
            source,
            @"PostAsJsonAsync\s*\([^\)]*new\s*\{|PutAsJsonAsync\s*\([^\)]*new\s*\{",
            RegexOptions.Singleline);

        Assert.False(hasAnonymousPayloads, "Inventory endpoint tests must use typed API inventory DTO payloads, not anonymous objects.");
    }

    private static string BuildJwt(string role, Guid? subjectId = null)
    {
        var now = DateTimeOffset.UtcNow;
        var sub = (subjectId ?? Guid.NewGuid()).ToString();
        var claims = new List<Claim>
        {
            new("sub", sub),
            new("email", "inventory-admin@test.com"),
            new("role", role),
            new("email_verified", "true")
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes("01234567890123456789012345678901")),
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: "tibia-webstore",
            audience: "tibia-webstore-client",
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(10).UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
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

    private sealed class InventoryApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        public static readonly Guid ProductId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        public InMemoryInventoryRepository Inventory { get; } = new();
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
                services.RemoveAll<IInventoryRepository>();
                services.RemoveAll<IProductRepository>();
                services.RemoveAll<ICategoryRepository>();

                _categories.Seed(new Category("Gold", "gold", "Gold offers"));
                _products.Seed(new Product("Gold Starter", "gold-starter", "Starter", 5m, Guid.NewGuid(), "gold"));

                services.AddSingleton<IInventoryRepository>(Inventory);
                services.AddSingleton<IProductRepository>(_products);
                services.AddSingleton<ICategoryRepository>(_categories);
            });
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
        }
    }

    private sealed class InMemoryInventoryRepository : IInventoryRepository
    {
        private readonly Dictionary<Guid, (int total, int reserved)> _stocks = new();
        private readonly Dictionary<string, InventoryReservationRecord> _reservations = new(StringComparer.Ordinal);

        public void SeedProduct(Guid productId, int total, int reserved)
        {
            _stocks[productId] = (total, reserved);
        }

        public Task<InventoryReservationRecord?> GetReservationByIntentAndProductAsync(
            string orderIntentKey,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            if (_reservations.TryGetValue(orderIntentKey, out var reservation) && reservation.ProductId == productId)
            {
                return Task.FromResult<InventoryReservationRecord?>(reservation);
            }

            return Task.FromResult<InventoryReservationRecord?>(null);
        }

        public Task<InventoryReservationRecord?> GetReservationByIntentKeyAsync(string orderIntentKey, CancellationToken cancellationToken = default)
        {
            _reservations.TryGetValue(orderIntentKey, out var reservation);
            return Task.FromResult(reservation);
        }

        public Task<ReserveInventoryResult> TryReserveAsync(ReserveInventoryAttempt attempt, CancellationToken cancellationToken = default)
        {
            if (_reservations.ContainsKey(attempt.OrderIntentKey))
            {
                return Task.FromResult(ReserveInventoryResult.Reserved());
            }

            var stock = _stocks[attempt.ProductId];
            var available = stock.total - stock.reserved;
            if (available < attempt.Quantity)
            {
                return Task.FromResult(ReserveInventoryResult.Conflict(available));
            }

            _stocks[attempt.ProductId] = (stock.total, stock.reserved + attempt.Quantity);
            _reservations[attempt.OrderIntentKey] = new InventoryReservationRecord(
                attempt.OrderIntentKey,
                attempt.OrderId,
                attempt.ProductId,
                attempt.Quantity,
                attempt.ReservedAtUtc,
                attempt.ReservationExpiresAtUtc,
                null);

            return Task.FromResult(ReserveInventoryResult.Reserved());
        }

        public Task<int> ReleaseReservationAsync(string orderIntentKey, ReservationReleaseReason reason, DateTimeOffset releasedAtUtc, CancellationToken cancellationToken = default)
        {
            if (!_reservations.TryGetValue(orderIntentKey, out var reservation) || reservation.IsReleased)
            {
                return Task.FromResult(0);
            }

            var stock = _stocks[reservation.ProductId];
            _stocks[reservation.ProductId] = (stock.total, Math.Max(0, stock.reserved - reservation.Quantity));
            _reservations[orderIntentKey] = reservation with { ReleasedAtUtc = releasedAtUtc };
            return Task.FromResult(reservation.Quantity);
        }

        public Task<InventoryAvailabilityResponse> GetAvailabilityAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var stock = _stocks[productId];
            return Task.FromResult(new InventoryAvailabilityResponse(stock.total - stock.reserved, stock.reserved, stock.total));
        }

        public Task<AdjustStockResponse> AdjustStockAsync(StockAdjustmentCommand command, CancellationToken cancellationToken = default)
        {
            var stock = _stocks[command.ProductId];
            _stocks[command.ProductId] = (stock.total + command.Delta, stock.reserved);
            return Task.FromResult(new AdjustStockResponse(
                command.ProductId,
                command.Delta,
                command.BeforeQuantity,
                command.AfterQuantity,
                command.Reason,
                command.AdminUserId,
                command.AdjustedAtUtc));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> _categories = [];

        public void Seed(Category category) => _categories.Add(category);

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

        public void Seed(Product product) => _products.Add(product);

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_products.SingleOrDefault(x => x.Id == id));

        public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(_products.SingleOrDefault(x => x.Slug == slug));

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
