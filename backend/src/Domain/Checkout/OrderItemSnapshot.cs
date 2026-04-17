namespace Domain.Checkout;

public sealed class OrderItemSnapshot
{
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string Currency { get; private set; }
    public string ProductName { get; private set; }
    public string ProductSlug { get; private set; }
    public string CategorySlug { get; private set; }

    public OrderItemSnapshot(
        Guid productId,
        int quantity,
        decimal unitPrice,
        string currency,
        string productName,
        string productSlug,
        string categorySlug)
    {
        ProductId = productId == Guid.Empty
            ? throw new ArgumentException("Product id is required.", nameof(productId))
            : productId;

        Quantity = quantity > 0
            ? quantity
            : throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

        UnitPrice = unitPrice >= 0m
            ? unitPrice
            : throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");

        Currency = NormalizeCurrency(currency);
        ProductName = RequireText(productName, nameof(productName));
        ProductSlug = RequireText(productSlug, nameof(productSlug));
        CategorySlug = RequireText(categorySlug, nameof(categorySlug));
    }

    private static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        var normalized = currency.Trim().ToUpperInvariant();
        if (normalized.Length != 3)
        {
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));
        }

        return normalized;
    }

    private static string RequireText(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", paramName);
        }

        return value.Trim();
    }

    private OrderItemSnapshot()
    {
        Currency = string.Empty;
        ProductName = string.Empty;
        ProductSlug = string.Empty;
        CategorySlug = string.Empty;
    }
}
