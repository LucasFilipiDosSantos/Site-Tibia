namespace Domain.Catalog;

public sealed class Product
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public string CategorySlug { get; private set; }
    public string? Server { get; private set; }
    public string? ImageUrl { get; private set; }
    public decimal Rating { get; private set; }
    public int SalesCount { get; private set; }
    public bool IsHidden { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Product(
        string name,
        string slug,
        string description,
        decimal price,
        Guid categoryId,
        string categorySlug,
        string? server,
        decimal rating = 0m,
        int salesCount = 0,
        string? imageUrl = null)
    {
        Name = RequireText(name, nameof(name));
        Slug = NormalizeSlug(slug, nameof(slug));
        Description = RequireText(description, nameof(description));
        Price = ValidatePrice(price, nameof(price));
        CategoryId = ValidateCategoryId(categoryId, nameof(categoryId));
        CategorySlug = NormalizeSlug(categorySlug, nameof(categorySlug));
        ImageUrl = NormalizeOptionalImageUrl(imageUrl, nameof(imageUrl));
        Server = NormalizeOptionalServer(server);
        Rating = ValidateRating(rating, nameof(rating));
        SalesCount = ValidateSalesCount(salesCount, nameof(salesCount));
        IsHidden = false;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    private Product()
    {
        Name = string.Empty;
        Slug = string.Empty;
        Description = string.Empty;
        CategorySlug = string.Empty;
        Server = string.Empty;
    }

    public void ReplaceDetails(string name, string description, decimal price, Guid categoryId, string categorySlug, string? server, string? imageUrl = null)
    {
        Name = RequireText(name, nameof(name));
        Description = RequireText(description, nameof(description));
        Price = ValidatePrice(price, nameof(price));
        CategoryId = ValidateCategoryId(categoryId, nameof(categoryId));
        CategorySlug = NormalizeSlug(categorySlug, nameof(categorySlug));
        Server = NormalizeOptionalServer(server);
        ImageUrl = NormalizeOptionalImageUrl(imageUrl, nameof(imageUrl));
        Touch();
    }

    public void UpdateCatalogMetadata(string? server, decimal rating, int salesCount)
    {
        Server = NormalizeOptionalServer(server);
        Rating = ValidateRating(rating, nameof(rating));
        SalesCount = ValidateSalesCount(salesCount, nameof(salesCount));
        Touch();
    }

    public void Hide()
    {
        if (IsHidden)
        {
            return;
        }

        IsHidden = true;
        Touch();
    }

    private static string RequireText(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeOptionalServer(string? value)
    {
        var normalized = NormalizeOptionalText(value);
        return normalized is null ? null : normalized;
    }

    private static string? NormalizeOptionalImageUrl(string? value, string paramName)
    {
        var normalized = NormalizeOptionalText(value);
        if (normalized is null)
        {
            return null;
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Image URL must be a valid absolute HTTP/HTTPS URL.", paramName);
        }

        return normalized;
    }

    private static string NormalizeSlug(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Slug is required.", paramName);
        }

        return value.Trim().ToLowerInvariant();
    }

    private static decimal ValidatePrice(decimal price, string paramName)
    {
        if (price < 0m)
        {
            throw new ArgumentOutOfRangeException(paramName, "Price cannot be negative.");
        }

        return price;
    }

    private static decimal ValidateRating(decimal rating, string paramName)
    {
        if (rating < 0m || rating > 5m)
        {
            throw new ArgumentOutOfRangeException(paramName, "Rating must be between 0 and 5.");
        }

        return rating;
    }

    private static int ValidateSalesCount(int salesCount, string paramName)
    {
        if (salesCount < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Sales count cannot be negative.");
        }

        return salesCount;
    }

    private static Guid ValidateCategoryId(Guid categoryId, string paramName)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category id is required.", paramName);
        }

        return categoryId;
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
