using API.Catalog;
using Application.Catalog.Contracts;
using Application.Inventory.Contracts;
using Domain.Catalog;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;

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

        public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(_products.SingleOrDefault(x => x.Slug == slug));

        public Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(_products.Any(x => x.Slug == slug));

        public Task<bool> ExistsByCategorySlugAsync(string categorySlug, CancellationToken cancellationToken = default)
            => Task.FromResult(_products.Any(x => x.CategorySlug == categorySlug));

        public Task<IReadOnlyList<Product>> ListAsync(ProductListQuery query, CancellationToken cancellationToken = default)
        {
            var source = _products.AsEnumerable();
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

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
