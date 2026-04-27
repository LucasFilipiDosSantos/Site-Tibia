namespace Domain.Catalog;

public sealed class ProductReview
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ProductReview(Guid userId, Guid productId, decimal rating, string? comment = null)
    {
        UserId = userId == Guid.Empty
            ? throw new ArgumentException("User id is required.", nameof(userId))
            : userId;
        ProductId = productId == Guid.Empty
            ? throw new ArgumentException("Product id is required.", nameof(productId))
            : productId;
        Rating = ValidateRating(rating, nameof(rating));
        Comment = NormalizeOptionalComment(comment);
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private ProductReview()
    {
    }

    private static decimal ValidateRating(decimal rating, string paramName)
    {
        if (rating < 0m || rating > 5m)
        {
            throw new ArgumentOutOfRangeException(paramName, "Rating must be between 0 and 5.");
        }

        return rating;
    }

    private static string? NormalizeOptionalComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return null;
        }

        var normalized = comment.Trim();
        if (normalized.Length > 2048)
        {
            throw new ArgumentException("Comment must be 2048 characters or fewer.", nameof(comment));
        }

        return normalized;
    }
}
