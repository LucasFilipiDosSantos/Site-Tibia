using Application.Catalog.Contracts;
using Application.Checkout.Contracts;
using Domain.Catalog;
using Microsoft.Extensions.Logging;

namespace Application.Catalog.Services;

public sealed class ProductReviewService
{
    private readonly IProductRepository _productRepository;
    private readonly IProductReviewRepository _productReviewRepository;
    private readonly IOrderLifecycleRepository _orderRepository;
    private readonly ILogger<ProductReviewService> _logger;

    public ProductReviewService(
        IProductRepository productRepository,
        IProductReviewRepository productReviewRepository,
        IOrderLifecycleRepository orderRepository,
        ILogger<ProductReviewService> logger)
    {
        _productRepository = productRepository;
        _productReviewRepository = productReviewRepository;
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<ProductReviewResponse?> GetUserReviewAsync(
        string productSlug,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = NormalizeSlug(productSlug);
        var product = await _productRepository.GetBySlugAsync(normalizedSlug, cancellationToken);
        if (product is null || product.IsHidden)
        {
            return null;
        }

        var review = await _productReviewRepository.GetByUserAndProductAsync(userId, product.Id, cancellationToken);
        return review is null
            ? null
            : new ProductReviewResponse(review.UserId, review.ProductId, review.Rating, review.Comment, review.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<ProductReviewResponse>> ListProductReviewsAsync(
        string productSlug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = NormalizeSlug(productSlug);
        var product = await _productRepository.GetBySlugAsync(normalizedSlug, cancellationToken);
        if (product is null || product.IsHidden)
        {
            return [];
        }

        var reviews = await _productReviewRepository.ListByProductAsync(product.Id, cancellationToken);
        return reviews
            .Select(review => new ProductReviewResponse(
                review.UserId,
                review.ProductId,
                review.Rating,
                review.Comment,
                review.CreatedAtUtc))
            .ToList();
    }

    public async Task<ProductReviewResponse> CreateReviewAsync(
        CreateProductReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(request.UserId));
        }

        var normalizedSlug = NormalizeSlug(request.ProductSlug);
        var product = await _productRepository.GetBySlugAsync(normalizedSlug, cancellationToken)
            ?? throw new ArgumentException("Product slug not found.", nameof(request.ProductSlug));

        if (product.IsHidden)
        {
            throw new ArgumentException("Product slug not found.", nameof(request.ProductSlug));
        }

        _logger.LogInformation(
            "Validating review eligibility for user {UserId}, product {ProductId}, slug {ProductSlug}",
            request.UserId,
            product.Id,
            normalizedSlug);

        var hasPaidOrder = await _orderRepository.HasPaidOrderForProductAsync(
            request.UserId,
            product.Id,
            cancellationToken);

        if (!hasPaidOrder)
        {
            var diagnostics = await _orderRepository.GetReviewOrderDiagnosticsAsync(
                request.UserId,
                product.Id,
                cancellationToken);

            _logger.LogWarning(
                "Review blocked because no eligible order was found for user {UserId}, product {ProductId}. Matching orders: {@Orders}. Eligible statuses: {EligibleStatuses}",
                request.UserId,
                product.Id,
                diagnostics.Select(order => new
                {
                    order.OrderId,
                    order.OrderIntentKey,
                    Status = order.Status.ToString(),
                    order.IsHidden,
                    order.ItemCount,
                    Items = order.Items.Select(item => new
                    {
                        item.ProductId,
                        item.ProductSlug
                    }).ToArray()
                }).ToArray(),
                string.Join(", ", Domain.Checkout.OrderStatusExtensions.GetReviewEligibleStatuses()));
            throw new ProductReviewPurchaseRequiredException();
        }

        var existing = await _productReviewRepository.GetByUserAndProductAsync(
            request.UserId,
            product.Id,
            cancellationToken);

        if (existing is not null)
        {
            _logger.LogInformation(
                "Duplicate review blocked for user {UserId} and product {ProductId}",
                request.UserId,
                product.Id);
            throw new DuplicateProductReviewException();
        }

        var review = new ProductReview(request.UserId, product.Id, request.Rating, request.Comment);
        await _productReviewRepository.AddAsync(review, cancellationToken);
        await _productReviewRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Review created for user {UserId}, product {ProductId}, rating {Rating}",
            request.UserId,
            product.Id,
            review.Rating);

        return new ProductReviewResponse(
            review.UserId,
            review.ProductId,
            review.Rating,
            review.Comment,
            review.CreatedAtUtc);
    }

    private static string NormalizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Product slug is required.", nameof(slug));
        }

        return slug.Trim().ToLowerInvariant();
    }
}
