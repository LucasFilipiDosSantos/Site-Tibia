using API.Auth;
using Application.Catalog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace API.Catalog;

[ApiController]
[Route("api/products/{slug}/reviews")]
public sealed class ProductReviewsController : ControllerBase
{
    private readonly ILogger<ProductReviewsController> _logger;

    public ProductReviewsController(ILogger<ProductReviewsController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductReviewResponse>>> ListReviews(
        string slug,
        [FromServices] ProductReviewService productReviewService,
        CancellationToken ct)
    {
        var reviews = await productReviewService.ListProductReviewsAsync(slug, ct);
        return Ok(reviews.Select(review => new ProductReviewResponse(
            review.UserId,
            review.ProductId,
            review.Rating,
            review.Comment,
            review.CreatedAtUtc)));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ProductReviewResponse>> GetMyReview(
        string slug,
        [FromServices] ProductReviewService productReviewService,
        CancellationToken ct)
    {
        var customerId = ResolveCustomerId(User);
        var review = await productReviewService.GetUserReviewAsync(slug, customerId, ct);
        if (review is null)
        {
            return NotFound();
        }

        return Ok(new ProductReviewResponse(
            review.UserId,
            review.ProductId,
            review.Rating,
            review.Comment,
            review.CreatedAtUtc));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ProductReviewResponse>> CreateReview(
        [FromRoute] string slug,
        [FromBody] CreateProductReviewRequest request,
        [FromServices] ProductReviewService productReviewService,
        [FromServices] IMemoryCache cache,
        CancellationToken ct)
    {
        LogUserClaims("CreateReview", slug);
        var customerId = ResolveCustomerId(User);
        var created = await productReviewService.CreateReviewAsync(
            new Application.Catalog.Contracts.CreateProductReviewRequest(slug, customerId, request.Rating, request.Comment),
            ct);

        cache.Remove($"catalog:product:{slug.Trim().ToLowerInvariant()}");

        var response = new ProductReviewResponse(
            created.UserId,
            created.ProductId,
            created.Rating,
            created.Comment,
            created.CreatedAtUtc);

        return Created($"/api/products/{slug.Trim().ToLowerInvariant()}/reviews", response);
    }

    private void LogUserClaims(string action, string slug)
    {
        var claims = User.Claims
            .Select(claim => new { claim.Type, claim.Value })
            .ToArray();

        _logger.LogInformation(
            "Review endpoint {Action} called for slug {Slug}. Authenticated={IsAuthenticated}. Claims={@Claims}",
            action,
            slug,
            User.Identity?.IsAuthenticated ?? false,
            claims);
    }

    private static Guid ResolveCustomerId(ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var customerId) || customerId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated subject claim is missing or invalid.", "sub");
        }

        return customerId;
    }
}
