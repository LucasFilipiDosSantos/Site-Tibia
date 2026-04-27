using API.Auth;
using Application.Catalog.Contracts;
using Application.Catalog.Services;
using Application.Checkout.Contracts;
using Application.Identity.Contracts;
using Domain.Catalog;
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

namespace UnitTests.Catalog;

public sealed class ProductReviewsControllerRouteTests
{
    [Fact]
    public async Task PostReview_WithoutAuthentication_ReturnsUnauthorizedInsteadOfNotFound()
    {
        await using var factory = new ProductReviewsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products/teste-imagem/reviews", new
        {
            rating = 4.25m,
            comment = "texto opcional"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostReview_WithAuthenticatedEligibleUser_ReturnsCreated()
    {
        await using var factory = new ProductReviewsApiFactory();
        using var client = factory.CreateClient();

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Review Buyer",
            email = "review@test.com",
            password = "ValidPass123!"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        factory.MarkEmailVerified("review@test.com");

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "review@test.com",
            password = "ValidPass123!"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var response = await client.PostAsJsonAsync("/api/products/teste-imagem/reviews", new
        {
            rating = 4.25m,
            comment = "texto opcional"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<API.Catalog.ProductReviewResponse>();
        Assert.NotNull(payload);
        Assert.Equal(factory.ReviewRepository.CreatedReviews.Single().UserId, payload!.UserId);
        Assert.Equal(factory.Product.Id, payload.ProductId);
        Assert.Equal(4.25m, payload.Rating);
    }

    private sealed class ProductReviewsApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private readonly InMemoryUserRepository _users = new();
        private readonly InMemoryRefreshSessionRepository _refreshSessions = new();

        public InMemoryProductReviewRepository ReviewRepository { get; } = new();
        public Product Product { get; } = new("Teste Imagem", "teste-imagem", "Produto para review", 10m, Guid.NewGuid(), "gold", "Antica");

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
                services.RemoveAll<IProductRepository>();
                services.RemoveAll<IProductReviewRepository>();
                services.RemoveAll<IOrderLifecycleRepository>();

                services.AddSingleton<IUserRepository>(_users);
                services.AddSingleton<IRefreshSessionRepository>(_refreshSessions);
                services.AddSingleton<IProductRepository>(new InMemoryProductRepository(Product));
                services.AddSingleton<IProductReviewRepository>(ReviewRepository);
                services.AddSingleton<IOrderLifecycleRepository>(new InMemoryOrderLifecycleRepository(Product.Id));
            });
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
        }
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

    private sealed class InMemoryProductRepository(Product product) : IProductRepository
    {
        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(id == product.Id ? product : null);

        public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Equals(product.Slug, slug, StringComparison.Ordinal) ? product : null);

        public Task<CatalogProductProjection?> GetCatalogBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult<CatalogProductProjection?>(string.Equals(product.Slug, slug, StringComparison.Ordinal)
                ? new CatalogProductProjection(product, 0, 0m, 0)
                : null);

        public Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Equals(product.Slug, slug, StringComparison.Ordinal));

        public Task<bool> ExistsByCategorySlugAsync(string categorySlug, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Equals(product.CategorySlug, categorySlug, StringComparison.Ordinal));

        public Task<bool> HasProtectedReferencesAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<IReadOnlyList<Product>> ListAsync(ProductListQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Product>>([product]);

        public Task<IReadOnlyList<CatalogProductProjection>> ListCatalogAsync(ProductListQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CatalogProductProjection>>([new CatalogProductProjection(product, 0, 0m, 0)]);

        public Task AddAsync(Product product, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(Product product, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class InMemoryProductReviewRepository : IProductReviewRepository
    {
        public List<ProductReview> CreatedReviews { get; } = [];

        public Task<IReadOnlyList<ProductReview>> ListByProductAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ProductReview>>(CreatedReviews.Where(review => review.ProductId == productId).ToList());

        public Task<ProductReview?> GetByUserAndProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(CreatedReviews.SingleOrDefault(review => review.UserId == userId && review.ProductId == productId));

        public Task AddAsync(ProductReview review, CancellationToken cancellationToken = default)
        {
            CreatedReviews.Add(review);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryOrderLifecycleRepository(Guid productId) : IOrderLifecycleRepository
    {
        public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
            => Task.FromResult<Order?>(null);

        public Task SaveAsync(Order order, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Order>> GetCustomerOrdersAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Order>>([]);

        public Task<bool> HasPaidOrderForProductAsync(Guid customerId, Guid requestedProductId, CancellationToken cancellationToken = default)
            => Task.FromResult(requestedProductId == productId);

        public Task<IReadOnlyList<ReviewOrderDiagnostic>> GetReviewOrderDiagnosticsAsync(Guid customerId, Guid requestedProductId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ReviewOrderDiagnostic>>([]);
    }
}
