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
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Product(string name, string slug, string description, decimal price, Guid categoryId, string categorySlug)
    {
        Name = RequireText(name, nameof(name));
        Slug = NormalizeSlug(slug, nameof(slug));
        Description = RequireText(description, nameof(description));
        Price = ValidatePrice(price, nameof(price));
        CategoryId = ValidateCategoryId(categoryId, nameof(categoryId));
        CategorySlug = NormalizeSlug(categorySlug, nameof(categorySlug));
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    private Product()
    {
        Name = string.Empty;
        Slug = string.Empty;
        Description = string.Empty;
        CategorySlug = string.Empty;
    }

    public void ReplaceDetails(string name, string description, decimal price, Guid categoryId, string categorySlug)
    {
        Name = RequireText(name, nameof(name));
        Description = RequireText(description, nameof(description));
        Price = ValidatePrice(price, nameof(price));
        CategoryId = ValidateCategoryId(categoryId, nameof(categoryId));
        CategorySlug = NormalizeSlug(categorySlug, nameof(categorySlug));
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
