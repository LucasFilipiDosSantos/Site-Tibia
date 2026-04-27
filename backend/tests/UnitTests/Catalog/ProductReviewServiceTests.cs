using Application.Catalog.Contracts;
using Application.Catalog.Services;
using Application.Checkout.Contracts;
using Domain.Catalog;
using Domain.Checkout;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Catalog;

public sealed class ProductReviewServiceTests
{
    [Fact]
    public async Task CreateReview_BlocksDuplicateReviewPerUserAndProduct()
    {
        var productRepository = new InMemoryProductRepository();
        var reviewRepository = new InMemoryProductReviewRepository();
        var product = new Product("Gold Starter", "gold-starter", "Starter pack", 10m, Guid.NewGuid(), "gold", "Lobera");
        var userId = Guid.NewGuid();
        productRepository.Seed(product);
        reviewRepository.Seed(new ProductReview(userId, product.Id, 4.5m, "Muito bom"));
        var orderRepository = new InMemoryOrderLifecycleRepository(hasPaidOrder: true);

        var service = new ProductReviewService(
            productRepository,
            reviewRepository,
            orderRepository,
            NullLogger<ProductReviewService>.Instance);

        var exception = await Assert.ThrowsAsync<DuplicateProductReviewException>(() =>
            service.CreateReviewAsync(new CreateProductReviewRequest("gold-starter", userId, 5m, "Nova tentativa")));

        Assert.Equal("Você já avaliou este produto.", exception.Message);
    }

    [Fact]
    public async Task CreateReview_PersistsOptionalCommentAndRating()
    {
        var productRepository = new InMemoryProductRepository();
        var reviewRepository = new InMemoryProductReviewRepository();
        var product = new Product("Gold Starter", "gold-starter", "Starter pack", 10m, Guid.NewGuid(), "gold", "Lobera");
        var userId = Guid.NewGuid();
        productRepository.Seed(product);
        var orderRepository = new InMemoryOrderLifecycleRepository(hasPaidOrder: true);

        var service = new ProductReviewService(
            productRepository,
            reviewRepository,
            orderRepository,
            NullLogger<ProductReviewService>.Instance);

        var created = await service.CreateReviewAsync(new CreateProductReviewRequest("gold-starter", userId, 4.25m, "Gostei"));

        Assert.Equal(userId, created.UserId);
        Assert.Equal(product.Id, created.ProductId);
        Assert.Equal(4.25m, created.Rating);
        Assert.Equal("Gostei", created.Comment);
    }

    [Fact]
    public async Task CreateReview_BlocksUserWithoutPaidPurchase()
    {
        var productRepository = new InMemoryProductRepository();
        var reviewRepository = new InMemoryProductReviewRepository();
        var product = new Product("Gold Starter", "gold-starter", "Starter pack", 10m, Guid.NewGuid(), "gold", "Lobera");
        var userId = Guid.NewGuid();
        productRepository.Seed(product);
        var orderRepository = new InMemoryOrderLifecycleRepository(hasPaidOrder: false);

        var service = new ProductReviewService(
            productRepository,
            reviewRepository,
            orderRepository,
            NullLogger<ProductReviewService>.Instance);

        var exception = await Assert.ThrowsAsync<ProductReviewPurchaseRequiredException>(() =>
            service.CreateReviewAsync(new CreateProductReviewRequest("gold-starter", userId, 4.25m, "Gostei")));

        Assert.Equal("Você só pode avaliar produtos comprados.", exception.Message);
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
            var product = _products.SingleOrDefault(x => x.Slug == slug);
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

    private sealed class InMemoryProductReviewRepository : IProductReviewRepository
    {
        private readonly List<ProductReview> _reviews = [];

        public void Seed(ProductReview review) => _reviews.Add(review);

        public Task<IReadOnlyList<ProductReview>> ListByProductAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ProductReview>>(_reviews.Where(x => x.ProductId == productId).ToList());

        public Task<ProductReview?> GetByUserAndProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(_reviews.SingleOrDefault(x => x.UserId == userId && x.ProductId == productId));

        public Task AddAsync(ProductReview review, CancellationToken cancellationToken = default)
        {
            _reviews.Add(review);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryOrderLifecycleRepository : IOrderLifecycleRepository
    {
        private readonly bool _hasPaidOrder;

        public InMemoryOrderLifecycleRepository(bool hasPaidOrder)
        {
            _hasPaidOrder = hasPaidOrder;
        }

        public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
            => Task.FromResult<Order?>(null);

        public Task SaveAsync(Order order, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<Order>> GetCustomerOrdersAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Order>>([]);

        public Task<bool> HasPaidOrderForProductAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(_hasPaidOrder);

        public Task<IReadOnlyList<ReviewOrderDiagnostic>> GetReviewOrderDiagnosticsAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ReviewOrderDiagnostic>>([]);
    }
}
