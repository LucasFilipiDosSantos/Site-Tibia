using API.Checkout;
using Application.Checkout.Contracts;
using Application.Inventory.Contracts;
using Domain.Checkout;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
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
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IntegrationTests.Checkout;

[Trait("Category", "CheckoutCapture")]
[Trait("Requirement", "CHK-02")]
[Trait("Requirement", "CHK-03")]
[Trait("Suite", "Phase04CheckoutApi")]
[Trait("Plan", "04-04")]
public sealed class CheckoutEndpointsTests
{
    [Fact]
    public async Task CheckoutSubmit_Success_ReturnsFrozenSnapshotAndClearsCart()
    {
        await using var factory = new CheckoutApiFactory();
        var productId = factory.SeedProduct("gold-pack", 11.25m, FulfillmentType.Automated, 10);
        using var client = factory.CreateAuthenticatedClient();

        await client.PostAsJsonAsync("/checkout/cart/items", new AddCartItemDto(productId, 2));

        var submit = await client.PostAsJsonAsync("/checkout/submit", new SubmitCheckoutDto(
            [new CheckoutDeliveryInstructionDto(productId, "Knight", "Aurera", "whatsapp:+5511000000000", null, null)]));

        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
        var payload = await submit.Content.ReadFromJsonAsync<SubmitCheckoutResponseDto>();
        Assert.NotNull(payload);

        var item = Assert.Single(payload!.Items);
        Assert.Equal(11.25m, item.UnitPrice);
        Assert.Equal("BRL", item.Currency);
        Assert.Equal("gold-pack", item.ProductSlug);

        var cart = await client.GetFromJsonAsync<CartResponseDto>("/checkout/cart");
        Assert.NotNull(cart);
        Assert.Empty(cart!.Lines);
    }

    [Fact]
    public async Task CheckoutSubmit_ValidatesInstructionBranchByFulfillmentType()
    {
        await using var factory = new CheckoutApiFactory();
        var productId = factory.SeedProduct("manual-service", 20m, FulfillmentType.Manual, 10);
        using var client = factory.CreateAuthenticatedClient();

        await client.PostAsJsonAsync("/checkout/cart/items", new AddCartItemDto(productId, 1));

        var invalid = await client.PostAsJsonAsync("/checkout/submit", new SubmitCheckoutDto(
            [new CheckoutDeliveryInstructionDto(productId, null, null, null, "", "") ]));

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task CheckoutSubmit_OnReserveConflict_Returns409WithLineConflictsAndNoMutation()
    {
        await using var factory = new CheckoutApiFactory();
        var productA = factory.SeedProduct("line-a", 5m, FulfillmentType.Automated, 5);
        var productB = factory.SeedProduct("line-b", 7m, FulfillmentType.Automated, 5);
        using var client = factory.CreateAuthenticatedClient();

        await client.PostAsJsonAsync("/checkout/cart/items", new AddCartItemDto(productA, 1));
        await client.PostAsJsonAsync("/checkout/cart/items", new AddCartItemDto(productB, 3));
        factory.SetCheckoutAvailability(productB, 1);

        var submit = await client.PostAsJsonAsync("/checkout/submit", new SubmitCheckoutDto(
            [
                new CheckoutDeliveryInstructionDto(productA, "CharA", "Aurera", "chan-a", null, null),
                new CheckoutDeliveryInstructionDto(productB, "CharB", "Aurera", "chan-b", null, null)
            ]));

        Assert.Equal(HttpStatusCode.Conflict, submit.StatusCode);
        var details = await submit.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(details);
        Assert.Equal("Conflict.", details!.Title);

        using var doc = JsonDocument.Parse(await submit.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.TryGetProperty("lineConflicts", out var conflicts));
        Assert.True(conflicts.GetArrayLength() >= 1);
        Assert.True(conflicts[0].TryGetProperty("availableQuantity", out var available));
        Assert.Equal(1, available.GetInt32());

        var cartAfter = await client.GetFromJsonAsync<CartResponseDto>("/checkout/cart");
        Assert.NotNull(cartAfter);
        Assert.Equal(2, cartAfter!.Lines.Count);

        Assert.Equal(0, factory.CheckoutRepository.StoredOrders.Count);
        Assert.Equal(0, factory.InventoryGateway.GetReservedQuantityForLastIntent(productA));
        Assert.Equal(0, factory.InventoryGateway.GetReservedQuantityForLastIntent(productB));
    }

    [Fact]
    public async Task CheckoutSubmit_WhenCompensationFails_Returns400AndDoesNotMutateCheckoutState()
    {
        await using var factory = new CheckoutApiFactory();
        var productA = factory.SeedProduct("line-a", 5m, FulfillmentType.Automated, 5);
        var productB = factory.SeedProduct("line-b", 7m, FulfillmentType.Automated, 5);
        factory.InventoryGateway.FailRelease = true;
        using var client = factory.CreateAuthenticatedClient();

        await client.PostAsJsonAsync("/checkout/cart/items", new AddCartItemDto(productA, 1));
        await client.PostAsJsonAsync("/checkout/cart/items", new AddCartItemDto(productB, 3));
        factory.SetCheckoutAvailability(productB, 1);

        var submit = await client.PostAsJsonAsync("/checkout/submit", new SubmitCheckoutDto(
            [
                new CheckoutDeliveryInstructionDto(productA, "CharA", "Aurera", "chan-a", null, null),
                new CheckoutDeliveryInstructionDto(productB, "CharB", "Aurera", "chan-b", null, null)
            ]));

        Assert.Equal(HttpStatusCode.BadRequest, submit.StatusCode);
        var details = await submit.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(details);
        Assert.Equal("Operation failed.", details!.Title);
        Assert.Contains("compensation failed", details.Detail ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        var cartAfter = await client.GetFromJsonAsync<CartResponseDto>("/checkout/cart");
        Assert.NotNull(cartAfter);
        Assert.Equal(2, cartAfter!.Lines.Count);
        Assert.Equal(0, factory.CheckoutRepository.StoredOrders.Count);
    }

    [Fact]
    public async Task GetOrder_ReturnsStoredSnapshotsEvenAfterCatalogMutation()
    {
        await using var factory = new CheckoutApiFactory();
        var productId = factory.SeedProduct("snap-item", 9m, FulfillmentType.Automated, 10);
        using var client = factory.CreateAuthenticatedClient();

        await client.PostAsJsonAsync("/checkout/cart/items", new AddCartItemDto(productId, 1));
        var submit = await client.PostAsJsonAsync("/checkout/submit", new SubmitCheckoutDto(
            [new CheckoutDeliveryInstructionDto(productId, "Knight", "Aurera", "dm", null, null)]));
        var checkout = await submit.Content.ReadFromJsonAsync<SubmitCheckoutResponseDto>();
        Assert.NotNull(checkout);

        factory.ProductGateway.Upsert(productId, "mutated", "mutated-slug", "gold", 99m, FulfillmentType.Automated);

        var get = await client.GetAsync($"/checkout/orders/{checkout!.OrderId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var order = await get.Content.ReadFromJsonAsync<OrderResponseDto>();
        Assert.NotNull(order);
        var item = Assert.Single(order!.Items);
        Assert.Equal("snap-item name", item.ProductName);
        Assert.Equal(9m, item.UnitPrice);
    }

    [Fact]
    public void CheckoutTests_DoNotUseAnonymousPayloads()
    {
        var file = FindRepoPath("tests/IntegrationTests/Checkout/CheckoutEndpointsTests.cs");
        var source = File.ReadAllText(file);
        var hasAnonymousPayloads = Regex.IsMatch(
            source,
            @"PostAsJsonAsync\s*\([^\)]*new\s*\{|PutAsJsonAsync\s*\([^\)]*new\s*\{",
            RegexOptions.Singleline);

        Assert.False(hasAnonymousPayloads, "Checkout endpoint tests must use typed DTO payloads.");
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

    private sealed class CheckoutApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private readonly InMemoryCartRepository _cartRepository = new();
        public InMemoryCheckoutRepository CheckoutRepository { get; } = new();
        public InMemoryProductGateway ProductGateway { get; } = new();
        private readonly InMemoryAvailabilityGateway _availabilityGateway = new();
        private readonly InMemoryCheckoutInventoryGateway _inventoryGateway = new();
        public InMemoryCheckoutInventoryGateway InventoryGateway => _inventoryGateway;

        public Guid CustomerId { get; } = Guid.NewGuid();

        public Guid SeedProduct(string slug, decimal price, FulfillmentType fulfillmentType, int available)
        {
            var productId = Guid.NewGuid();
            ProductGateway.Upsert(productId, $"{slug} name", slug, "gold", price, fulfillmentType);
            _availabilityGateway.Set(productId, available);
            _inventoryGateway.SetAvailability(productId, available);
            return productId;
        }

        public void SetCheckoutAvailability(Guid productId, int available)
        {
            _inventoryGateway.SetAvailability(productId, available);
        }

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
                services.RemoveAll<ICartRepository>();
                services.RemoveAll<ICheckoutRepository>();
                services.RemoveAll<ICartProductAvailabilityGateway>();
                services.RemoveAll<ICheckoutProductCatalogGateway>();
                services.RemoveAll<ICheckoutInventoryGateway>();

                services.AddSingleton<ICartRepository>(_cartRepository);
                services.AddSingleton<ICheckoutRepository>(CheckoutRepository);
                services.AddSingleton<ICartProductAvailabilityGateway>(_availabilityGateway);
                services.AddSingleton<ICheckoutProductCatalogGateway>(ProductGateway);
                services.AddSingleton<ICheckoutInventoryGateway>(_inventoryGateway);
            });
        }

        public HttpClient CreateAuthenticatedClient()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BuildJwt(CustomerId));
            return client;
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

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
        }
    }

    private sealed class InMemoryCartRepository : ICartRepository
    {
        private readonly Dictionary<Guid, Cart> _carts = [];

        public Task<Cart?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(_carts.TryGetValue(customerId, out var cart) ? cart : null);

        public Task SaveAsync(Cart cart, CancellationToken cancellationToken = default)
        {
            _carts[cart.CustomerId] = cart;
            return Task.CompletedTask;
        }

        public Task ClearAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            _carts.Remove(customerId);
            return Task.CompletedTask;
        }
    }

    public sealed class InMemoryCheckoutRepository : ICheckoutRepository
    {
        public List<Order> StoredOrders { get; } = [];

        public Task SaveOrderAsync(Order order, CancellationToken cancellationToken = default)
        {
            StoredOrders.Add(order);
            return Task.CompletedTask;
        }

        public Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
            => Task.FromResult(StoredOrders.SingleOrDefault(x => x.Id == orderId));
    }

    public sealed class InMemoryAvailabilityGateway : ICartProductAvailabilityGateway
    {
        private readonly Dictionary<Guid, int> _available = [];

        public void Set(Guid productId, int available) => _available[productId] = available;

        public Task<ProductAvailabilityResponse> GetAvailabilityAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ProductAvailabilityResponse(productId, _available[productId]));
    }

    public sealed class InMemoryProductGateway : ICheckoutProductCatalogGateway
    {
        private readonly Dictionary<Guid, CheckoutProductSnapshot> _snapshots = [];

        public void Upsert(Guid productId, string name, string slug, string categorySlug, decimal price, FulfillmentType fulfillmentType)
            => _snapshots[productId] = new CheckoutProductSnapshot(productId, name, slug, categorySlug, price, "BRL", fulfillmentType);

        public Task<CheckoutProductSnapshot> GetSnapshotAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(_snapshots[productId]);
    }

    public sealed class InMemoryCheckoutInventoryGateway : ICheckoutInventoryGateway
    {
        private readonly Dictionary<Guid, int> _available = [];
        private readonly Dictionary<string, Dictionary<Guid, int>> _reservedByIntent = [];
        public string? LastOrderIntentKey { get; private set; }
        public bool FailRelease { get; set; }

        public void SetAvailability(Guid productId, int available) => _available[productId] = available;

        public Task ReserveStockForCheckoutAsync(Guid orderId, string orderIntentKey, Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            LastOrderIntentKey = orderIntentKey;
            var available = _available[productId];
            if (quantity > available)
            {
                throw new CheckoutReservationConflictException([new CheckoutLineConflict(productId, quantity, available)]);
            }

            _available[productId] = available - quantity;

            if (!_reservedByIntent.TryGetValue(orderIntentKey, out var perProduct))
            {
                perProduct = [];
                _reservedByIntent[orderIntentKey] = perProduct;
            }

            perProduct[productId] = perProduct.TryGetValue(productId, out var existing)
                ? existing + quantity
                : quantity;

            return Task.CompletedTask;
        }

        public Task ReleaseCheckoutReservationAsync(string orderIntentKey, ReservationReleaseReason reason, CancellationToken cancellationToken = default)
        {
            if (FailRelease)
            {
                throw new InvalidOperationException("simulated release failure");
            }

            if (_reservedByIntent.TryGetValue(orderIntentKey, out var perProduct))
            {
                foreach (var entry in perProduct)
                {
                    _available[entry.Key] = _available[entry.Key] + entry.Value;
                }
            }

            _reservedByIntent[orderIntentKey] = [];
            return Task.CompletedTask;
        }

        public int GetReservedQuantityForLastIntent(Guid productId)
        {
            if (LastOrderIntentKey is null)
            {
                return 0;
            }

            return _reservedByIntent.TryGetValue(LastOrderIntentKey, out var perProduct)
                && perProduct.TryGetValue(productId, out var quantity)
                ? quantity
                : 0;
        }
    }
}
