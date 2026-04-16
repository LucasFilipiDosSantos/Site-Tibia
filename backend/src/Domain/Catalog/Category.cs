namespace Domain.Catalog;

public sealed class Category
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string Description { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Category(string name, string slug, string description)
    {
        Name = RequireText(name, nameof(name));
        Slug = NormalizeSlug(slug, nameof(slug));
        Description = RequireText(description, nameof(description));
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    private Category()
    {
        Name = string.Empty;
        Slug = string.Empty;
        Description = string.Empty;
    }

    public void UpdateDetails(string name, string description)
    {
        Name = RequireText(name, nameof(name));
        Description = RequireText(description, nameof(description));
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

    private static string NormalizeSlug(string slug, string paramName)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Slug is required.", paramName);
        }

        return slug.Trim().ToLowerInvariant();
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
