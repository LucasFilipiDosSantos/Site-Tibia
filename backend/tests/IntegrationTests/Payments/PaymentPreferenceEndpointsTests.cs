using API.Checkout;
using Application.Payments.Contracts;
using Domain.Checkout;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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

namespace IntegrationTests.Payments;

[Trait("Category", "PaymentConfirmation")]
[Trait("Requirement", "PAY-01")]
[Trait("Suite", "Phase06PaymentInit")]
[Trait("Plan", "06-01")]
public sealed class PaymentPreferenceEndpointsTests
{
    [Fact]
    public async Task CreatePreference_PersistsPaymentLinkSnapshot_AndReturnsOrderBoundExternalReference()
    {
        await using var factory = await PaymentPreferenceApiFactory.CreateAsync();
        var orderId = await factory.SeedOrderAsync(factory.CustomerId, quantity: 2, unitPrice: 12.50m, currency: "BRL");
        using var client = factory.CreateAuthenticatedClient(factory.CustomerId);

        var response = await client.PostAsync($"/checkout/orders/{orderId}/payments/preference", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CreatePaymentPreferenceResponseDto>();
        Assert.NotNull(payload);
        Assert.Equal(orderId.ToString(), payload!.ExternalReference);

        await using var db = factory.CreateDbContext();
        var saved = await db.PaymentLinks.SingleAsync(x => x.PreferenceId == payload.PreferenceId);
        Assert.Equal(orderId, saved.OrderId);
        Assert.Equal(25.00m, saved.ExpectedAmount);
        Assert.Equal("BRL", saved.ExpectedCurrency);
    }

    [Fact]
    public async Task CreatePreference_WhenOrderBelongsToAnotherCustomer_ReturnsNotFound()
    {
        await using var factory = await PaymentPreferenceApiFactory.CreateAsync();
        var ownerId = Guid.NewGuid();
        var orderId = await factory.SeedOrderAsync(ownerId, quantity: 1, unitPrice: 20m, currency: "BRL");
        using var client = factory.CreateAuthenticatedClient(Guid.NewGuid());

        var response = await client.PostAsync($"/checkout/orders/{orderId}/payments/preference", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var db = factory.CreateDbContext();
        Assert.False(await db.PaymentLinks.AnyAsync(x => x.OrderId == orderId));
    }

    private sealed class PaymentPreferenceApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        private DbContextOptions<AppDbContext>? _options;

        public Guid CustomerId { get; } = Guid.NewGuid();

        public static async Task<PaymentPreferenceApiFactory> CreateAsync()
        {
            var factory = new PaymentPreferenceApiFactory();
            await factory._connection.OpenAsync();

            factory._options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(factory._connection)
                .Options;

            await using var setup = new AppDbContext(factory._options);
            await setup.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
            await setup.Database.EnsureCreatedAsync();

            return factory;
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
                    new KeyValuePair<string, string?>("IdentityTokenDelivery:Provider", "inmemory"),
                    new KeyValuePair<string, string?>("MercadoPago:AccessToken", "TEST-123"),
                    new KeyValuePair<string, string?>("MercadoPago:PublicKey", "TEST-456"),
                    new KeyValuePair<string, string?>("MercadoPago:NotificationUrl", "https://example.com/webhook"),
                    new KeyValuePair<string, string?>("MercadoPago:SuccessUrl", "https://example.com/success"),
                    new KeyValuePair<string, string?>("MercadoPago:FailureUrl", "https://example.com/failure"),
                    new KeyValuePair<string, string?>("MercadoPago:PendingUrl", "https://example.com/pending")
                ]);
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<AppDbContext>();
                services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

                services.RemoveAll<IMercadoPagoPreferenceGateway>();
                services.AddSingleton<IMercadoPagoPreferenceGateway>(new StubMercadoPagoPreferenceGateway());
            });
        }

        public AppDbContext CreateDbContext()
        {
            if (_options is null)
            {
                throw new InvalidOperationException("Factory not initialized.");
            }

            var db = new AppDbContext(_options);
            db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
            return db;
        }

        public async Task<Guid> SeedOrderAsync(Guid customerId, int quantity, decimal unitPrice, string currency)
        {
            var order = new Order(Guid.NewGuid(), customerId, $"checkout-{Guid.NewGuid():N}");
            order.AddItemSnapshot(new OrderItemSnapshot(
                Guid.NewGuid(),
                quantity,
                unitPrice,
                currency,
                "Gold Pack",
                "gold-pack",
                "gold"));

            await using var db = CreateDbContext();
            await db.Orders.AddAsync(order);
            await db.SaveChangesAsync();

            return order.Id;
        }

        public HttpClient CreateAuthenticatedClient(Guid customerId)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BuildJwt(customerId));
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
            await _connection.DisposeAsync();
        }
    }

    private sealed class StubMercadoPagoPreferenceGateway : IMercadoPagoPreferenceGateway
    {
        public Task<MercadoPagoPreferenceCreateResult> CreatePreferenceAsync(
            MercadoPagoPreferenceCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            var preferenceId = $"pref-{request.ExternalReference}";
            return Task.FromResult(new MercadoPagoPreferenceCreateResult(preferenceId, $"https://init/{preferenceId}"));
        }
    }
}
