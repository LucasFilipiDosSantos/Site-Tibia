using API.Auth;
using API.Checkout;
using Application.Checkout.Contracts;
using Application.Identity.Contracts;
using Application.Inventory.Contracts;
using Application.Payments.Contracts;
using Domain.Checkout;
using Domain.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace UnitTests.E2E;

public sealed class LoginCheckoutPurchaseFlowTests
{
    [Fact]
    public async Task CustomerCanLoginCheckoutAndCreateMockedPaymentPreference()
    {
        await using var factory = new PurchaseFlowApiFactory();
        var productId = factory.SeedProduct("gold-pack", 15.50m, FulfillmentType.Automated, available: 10);
        using var client = factory.CreateClient();

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Buyer Test",
            email = "Buyer@Test.com",
            password = "ValidPass123!"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        factory.MarkEmailVerified("buyer@test.com");

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "buyer@test.com",
            password = "ValidPass123!"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var addToCart = await client.PostAsJsonAsync("/api/cart/items", new AddCartItemDto(productId, 2));
        Assert.Equal(HttpStatusCode.OK, addToCart.StatusCode);

        var checkout = await client.PostAsJsonAsync("/api/orders/submit", new SubmitCheckoutDto(
            [new CheckoutDeliveryInstructionDto(productId, "Knight Buyer", "Antica", "whatsapp:+5511999999999", null, null)]));
        Assert.Equal(HttpStatusCode.OK, checkout.StatusCode);

        var checkoutPayload = await checkout.Content.ReadFromJsonAsync<SubmitCheckoutResponseDto>();
        Assert.NotNull(checkoutPayload);
        Assert.Equal(31.00m, checkoutPayload!.Items.Sum(item => item.UnitPrice * item.Quantity));
        Assert.Empty((await client.GetFromJsonAsync<CartResponseDto>("/api/cart"))!.Lines);

        var payment = await client.PostAsync($"/api/orders/{checkoutPayload.OrderId}/payments/preference", null);
        Assert.Equal(HttpStatusCode.OK, payment.StatusCode);

        var paymentPayload = await payment.Content.ReadFromJsonAsync<CreatePaymentPreferenceResponseDto>();
        Assert.NotNull(paymentPayload);
        Assert.Equal(checkoutPayload.OrderId.ToString(), paymentPayload!.ExternalReference);
        Assert.StartsWith("mock-pref-", paymentPayload.PreferenceId, StringComparison.Ordinal);
        Assert.StartsWith("https://mock-pay.local/", paymentPayload.InitPointUrl, StringComparison.Ordinal);

        var order = Assert.Single(factory.CheckoutRepository.StoredOrders);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Single(factory.PaymentLinkRepository.SavedLinks);
    }

    private sealed class PurchaseFlowApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private readonly InMemoryUserRepository _users = new();
        private readonly InMemoryRefreshSessionRepository _refreshSessions = new();
        private readonly InMemoryCartRepository _cartRepository = new();
        private readonly InMemoryAvailabilityGateway _availabilityGateway = new();
        private readonly InMemoryCheckoutInventoryGateway _inventoryGateway = new();
        private readonly InMemoryProductGateway _productGateway = new();

        public InMemoryCheckoutRepository CheckoutRepository { get; } = new();
        public InMemoryPaymentLinkRepository PaymentLinkRepository { get; } = new();

        public Guid SeedProduct(string slug, decimal price, FulfillmentType fulfillmentType, int available)
        {
            var productId = Guid.NewGuid();
            _productGateway.Upsert(productId, $"{slug} name", slug, "gold", price, fulfillmentType);
            _availabilityGateway.Set(productId, available);
            _inventoryGateway.SetAvailability(productId, available);
            return productId;
        }

        public void MarkEmailVerified(string email)
        {
            var user = _users.Users.Single(x => x.Email == UserAccount.NormalizeEmail(email));
            user.MarkEmailVerified();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "tibia-webstore",
                    ["Jwt:Audience"] = "tibia-webstore-client",
                    ["Jwt:SigningKey"] = "01234567890123456789012345678901",
                    ["IdentityTokenDelivery:Provider"] = "inmemory",
                    ["MercadoPago:AccessToken"] = "TEST-access-token",
                    ["MercadoPago:PublicKey"] = "TEST-public-key",
                    ["MercadoPago:NotificationUrl"] = "https://test.local/api/payments/webhook",
                    ["MercadoPago:SuccessUrl"] = "https://test.local/checkout/success",
                    ["MercadoPago:FailureUrl"] = "https://test.local/checkout/failure",
                    ["MercadoPago:PendingUrl"] = "https://test.local/checkout/pending",
                    ["WhatsApp:AccessToken"] = "test-token",
                    ["WhatsApp:PhoneNumberId"] = "test-phone",
                    ["WhatsApp:WhatsAppBusinessId"] = "test-business"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUserRepository>();
                services.RemoveAll<IRefreshSessionRepository>();
                services.RemoveAll<ICartRepository>();
                services.RemoveAll<ICheckoutRepository>();
                services.RemoveAll<IOrderLifecycleRepository>();
                services.RemoveAll<ICustomerRepository>();
                services.RemoveAll<ICartProductAvailabilityGateway>();
                services.RemoveAll<ICheckoutProductCatalogGateway>();
                services.RemoveAll<ICheckoutInventoryGateway>();
                services.RemoveAll<IPaymentLinkRepository>();
                services.RemoveAll<IMercadoPagoPreferenceGateway>();

                services.AddSingleton<IUserRepository>(_users);
                services.AddSingleton<IRefreshSessionRepository>(_refreshSessions);
                services.AddSingleton<ICartRepository>(_cartRepository);
                services.AddSingleton<ICheckoutRepository>(CheckoutRepository);
                services.AddSingleton<IOrderLifecycleRepository>(CheckoutRepository);
                services.AddSingleton<ICustomerRepository>(new InMemoryCustomerRepository("+5511999999999"));
                services.AddSingleton<ICartProductAvailabilityGateway>(_availabilityGateway);
                services.AddSingleton<ICheckoutProductCatalogGateway>(_productGateway);
                services.AddSingleton<ICheckoutInventoryGateway>(_inventoryGateway);
                services.AddSingleton<IPaymentLinkRepository>(PaymentLinkRepository);
                services.AddSingleton<IMercadoPagoPreferenceGateway>(new StubMercadoPagoPreferenceGateway());
            });
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
        }
    }

    private sealed class InMemoryCustomerRepository(string notificationPhone) : ICustomerRepository
    {
        public Task<string?> GetNotificationPhoneAsync(Guid customerId, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(notificationPhone);
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        public List<UserAccount> Users { get; } = [];

        public Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(Users.SingleOrDefault(user => user.Email == UserAccount.NormalizeEmail(email)));

        public Task<UserAccount?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult(Users.SingleOrDefault(user => user.Id == userId));

        public Task AddAsync(UserAccount user, CancellationToken cancellationToken = default)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(UserAccount user, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryRefreshSessionRepository : IRefreshSessionRepository
    {
        public List<RefreshSession> Sessions { get; } = [];

        public Task<RefreshSession?> GetActiveByTokenHashAsync(string tokenHash, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
            => Task.FromResult(Sessions.SingleOrDefault(session => session.TokenHash == tokenHash && !session.IsRevoked && !session.IsExpired(nowUtc)));

        public Task AddAsync(RefreshSession session, CancellationToken cancellationToken = default)
        {
            Sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task RevokeCurrentAndInsertNextAsync(RefreshSession currentSession, RefreshSession nextSession, DateTimeOffset revokedAtUtc, string? revokedByIp, CancellationToken cancellationToken = default)
        {
            currentSession.Revoke(revokedAtUtc, revokedByIp, nextSession.TokenHash);
            Sessions.Add(nextSession);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
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

    public sealed class InMemoryCheckoutRepository : ICheckoutRepository, IOrderLifecycleRepository
    {
        public List<Order> StoredOrders { get; } = [];

        public Task SaveOrderAsync(Order order, CancellationToken cancellationToken = default)
        {
            StoredOrders.Add(order);
            return Task.CompletedTask;
        }

        public Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
            => Task.FromResult(StoredOrders.SingleOrDefault(order => order.Id == orderId));

        public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
            => GetOrderByIdAsync(orderId, cancellationToken);

        public Task SaveAsync(Order order, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<Order>> GetCustomerOrdersAsync(Guid customerId, string? customerEmail, int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Order>>(StoredOrders.Where(order => order.CustomerId == customerId).ToList());

        public Task<bool> HasPaidOrderForProductAsync(Guid customerId, string? customerEmail, Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(StoredOrders.Any(order =>
                (order.CustomerId == customerId || (customerEmail is not null && string.Equals(order.CustomerEmail, customerEmail, StringComparison.OrdinalIgnoreCase))) &&
                order.Status.IsReviewEligible() &&
                order.Items.Any(item => item.ProductId == productId)));

        public Task<IReadOnlyList<ReviewOrderDiagnostic>> GetReviewOrderDiagnosticsAsync(Guid customerId, string? customerEmail, Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ReviewOrderDiagnostic>>(StoredOrders
                .Where(order => (order.CustomerId == customerId || (customerEmail is not null && string.Equals(order.CustomerEmail, customerEmail, StringComparison.OrdinalIgnoreCase))) && order.Items.Any(item => item.ProductId == productId))
                .Select(order => new ReviewOrderDiagnostic(
                    order.Id,
                    order.OrderIntentKey,
                    order.CustomerId,
                    order.CustomerEmail,
                    order.Status,
                    false,
                    order.Items.Count,
                    order.Items.Select(item => new ReviewOrderItemDiagnostic(item.ProductId, item.ProductSlug)).ToList()))
                .ToList());

        public Task<IReadOnlyList<Order>> SearchOrdersAsync(OrderStatus? status, Guid? customerId, DateTimeOffset? createdFromUtc, DateTimeOffset? createdToUtc, int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Order>>(StoredOrders);
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

        public Task ReleaseCheckoutReservationAsync(string orderIntentKey, ReservationReleaseReason reason, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    public sealed class InMemoryPaymentLinkRepository : IPaymentLinkRepository
    {
        public List<PaymentLinkSnapshot> SavedLinks { get; } = [];

        public Task SaveAsync(PaymentLinkSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            SavedLinks.Add(snapshot);
            return Task.CompletedTask;
        }

        public Task<PaymentLinkSnapshot?> GetByProviderPaymentIdAsync(string providerPaymentId, CancellationToken cancellationToken = default)
            => Task.FromResult(SavedLinks.SingleOrDefault(link => link.PreferenceId == providerPaymentId));
    }

    private sealed class StubMercadoPagoPreferenceGateway : IMercadoPagoPreferenceGateway
    {
        public Task<MercadoPagoPreferenceCreateResult> CreatePreferenceAsync(MercadoPagoPreferenceCreateRequest request, CancellationToken cancellationToken = default)
        {
            var preferenceId = $"mock-pref-{request.ExternalReference}";
            return Task.FromResult(new MercadoPagoPreferenceCreateResult(preferenceId, $"https://mock-pay.local/{preferenceId}"));
        }
    }
}
