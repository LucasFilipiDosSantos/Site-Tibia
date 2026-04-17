using API.Checkout;
using Application.Checkout.Contracts;
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
using System.Text.RegularExpressions;

namespace IntegrationTests.Checkout;

[Trait("Category", "CheckoutCapture")]
[Trait("Requirement", "CHK-01")]
[Trait("Suite", "Phase04CheckoutApi")]
[Trait("Plan", "04-04")]
public sealed class CartEndpointsTests
{
    [Fact]
    public async Task CartEndpoints_RequireAuthentication()
    {
        await using var factory = new CheckoutApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/checkout/cart");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddSameProductTwice_MergesExistingLine()
    {
        await using var factory = new CheckoutApiFactory();
        var productId = factory.SeedProduct("gold-pack", 10m, FulfillmentType.Automated, 10);
        using var client = factory.CreateAuthenticatedClient();

        var first = await client.PostAsJsonAsync("/checkout/cart/items", new AddCartItemDto(productId, 1));
        var second = await client.PostAsJsonAsync("/checkout/cart/items", new AddCartItemDto(productId, 2));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var cart = await second.Content.ReadFromJsonAsync<CartResponseDto>();
        Assert.NotNull(cart);
        var line = Assert.Single(cart!.Lines);
        Assert.Equal(productId, line.ProductId);
        Assert.Equal(3, line.Quantity);
    }

    [Fact]
    public async Task SetAndRemoveEndpoints_UseExplicitSemantics()
    {
        await using var factory = new CheckoutApiFactory();
        var productId = factory.SeedProduct("set-remove", 10m, FulfillmentType.Automated, 20);
        using var client = factory.CreateAuthenticatedClient();

        await client.PostAsJsonAsync("/checkout/cart/items", new AddCartItemDto(productId, 1));
        var set = await client.PutAsJsonAsync($"/checkout/cart/items/{productId}", new SetCartItemQuantityDto(7));
        var afterSet = await set.Content.ReadFromJsonAsync<CartResponseDto>();

        var remove = await client.DeleteAsync($"/checkout/cart/items/{productId}");
        var afterRemove = await remove.Content.ReadFromJsonAsync<CartResponseDto>();

        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        Assert.Equal(7, Assert.Single(afterSet!.Lines).Quantity);
        Assert.Equal(HttpStatusCode.OK, remove.StatusCode);
        Assert.Empty(afterRemove!.Lines);
    }

    [Fact]
    public async Task OversellAddOrSet_Returns409ProblemDetails()
    {
        await using var factory = new CheckoutApiFactory();
        var productId = factory.SeedProduct("oversell", 10m, FulfillmentType.Automated, 2);
        using var client = factory.CreateAuthenticatedClient();

        var add = await client.PostAsJsonAsync("/checkout/cart/items", new AddCartItemDto(productId, 3));
        Assert.Equal(HttpStatusCode.Conflict, add.StatusCode);
        var addDetails = await add.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(addDetails);
        Assert.Equal("Conflict.", addDetails!.Title);

        await client.PostAsJsonAsync("/checkout/cart/items", new AddCartItemDto(productId, 1));
        var set = await client.PutAsJsonAsync($"/checkout/cart/items/{productId}", new SetCartItemQuantityDto(3));
        Assert.Equal(HttpStatusCode.Conflict, set.StatusCode);
    }

    [Fact]
    public void CartTests_DoNotUseAnonymousPayloads()
    {
        var file = FindRepoPath("tests/IntegrationTests/Checkout/CartEndpointsTests.cs");
        var source = File.ReadAllText(file);
        var hasAnonymousPayloads = Regex.IsMatch(
            source,
            @"PostAsJsonAsync\s*\([^\)]*new\s*\{|PutAsJsonAsync\s*\([^\)]*new\s*\{",
            RegexOptions.Singleline);

        Assert.False(hasAnonymousPayloads, "Cart endpoint tests must use typed DTO payloads.");
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
        private readonly InMemoryCheckoutRepository _checkoutRepository = new();
        private readonly InMemoryProductGateway _productGateway = new();
        private readonly InMemoryAvailabilityGateway _availabilityGateway = new();
        private readonly InMemoryCheckoutInventoryGateway _inventoryGateway = new();

        public Guid CustomerId { get; } = Guid.NewGuid();

        public Guid SeedProduct(string slug, decimal price, FulfillmentType fulfillmentType, int available)
        {
            var productId = Guid.NewGuid();
            _productGateway.Upsert(productId, $"{slug} name", slug, "gold", price, fulfillmentType);
            _availabilityGateway.Set(productId, available);
            _inventoryGateway.SetAvailability(productId, available);
            return productId;
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
                services.AddSingleton<ICheckoutRepository>(_checkoutRepository);
                services.AddSingleton<ICartProductAvailabilityGateway>(_availabilityGateway);
                services.AddSingleton<ICheckoutProductCatalogGateway>(_productGateway);
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

    private sealed class InMemoryCheckoutRepository : ICheckoutRepository
    {
        public Task SaveOrderAsync(Order order, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
            => Task.FromResult<Order?>(null);
    }

    private sealed class InMemoryAvailabilityGateway : ICartProductAvailabilityGateway
    {
        private readonly Dictionary<Guid, int> _available = [];

        public void Set(Guid productId, int available) => _available[productId] = available;

        public Task<ProductAvailabilityResponse> GetAvailabilityAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ProductAvailabilityResponse(productId, _available[productId]));
    }

    private sealed class InMemoryProductGateway : ICheckoutProductCatalogGateway
    {
        private readonly Dictionary<Guid, CheckoutProductSnapshot> _snapshots = [];

        public void Upsert(Guid productId, string name, string slug, string categorySlug, decimal price, FulfillmentType fulfillmentType)
            => _snapshots[productId] = new CheckoutProductSnapshot(productId, name, slug, categorySlug, price, "BRL", fulfillmentType);

        public Task<CheckoutProductSnapshot> GetSnapshotAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(_snapshots[productId]);
    }

    private sealed class InMemoryCheckoutInventoryGateway : ICheckoutInventoryGateway
    {
        private readonly Dictionary<Guid, int> _available = [];

        public void SetAvailability(Guid productId, int available) => _available[productId] = available;

        public Task ReserveStockForCheckoutAsync(Guid orderId, string orderIntentKey, Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            var available = _available[productId];
            if (quantity > available)
            {
                throw new CheckoutReservationConflictException([new CheckoutLineConflict(productId, quantity, available)]);
            }

            _available[productId] = available - quantity;
            return Task.CompletedTask;
        }
    }
}
