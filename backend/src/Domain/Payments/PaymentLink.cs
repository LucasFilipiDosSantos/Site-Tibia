namespace Domain.Payments;

public sealed class PaymentLink
{
    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public string PreferenceId { get; private set; }

    public decimal ExpectedAmount { get; private set; }

    public string ExpectedCurrency { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public PaymentLink(
        Guid id,
        Guid orderId,
        string preferenceId,
        decimal expectedAmount,
        string expectedCurrency,
        DateTimeOffset createdAtUtc)
    {
        Id = id == Guid.Empty ? throw new ArgumentException("Payment link id is required.", nameof(id)) : id;
        OrderId = orderId == Guid.Empty ? throw new ArgumentException("Order id is required.", nameof(orderId)) : orderId;
        PreferenceId = string.IsNullOrWhiteSpace(preferenceId)
            ? throw new ArgumentException("Preference id is required.", nameof(preferenceId))
            : preferenceId.Trim();
        ExpectedAmount = expectedAmount >= 0m
            ? expectedAmount
            : throw new ArgumentOutOfRangeException(nameof(expectedAmount), "Expected amount cannot be negative.");
        ExpectedCurrency = NormalizeCurrency(expectedCurrency);
        CreatedAtUtc = createdAtUtc;
    }

    private static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Expected currency is required.", nameof(currency));
        }

        var normalized = currency.Trim().ToUpperInvariant();
        if (normalized.Length != 3)
        {
            throw new ArgumentException("Expected currency must be a 3-letter ISO code.", nameof(currency));
        }

        return normalized;
    }

    private PaymentLink()
    {
        PreferenceId = string.Empty;
        ExpectedCurrency = string.Empty;
    }
}
