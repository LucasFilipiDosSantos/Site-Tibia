namespace Application.Payments.Contracts;

public sealed record PaymentPreferenceItem(
    string Title,
    int Quantity,
    decimal UnitPrice,
    string CurrencyId);

public sealed record MercadoPagoPreferenceCreateRequest(
    string ExternalReference,
    string NotificationUrl,
    string SuccessUrl,
    string FailureUrl,
    string PendingUrl,
    IReadOnlyList<PaymentPreferenceItem> Items);

public sealed record MercadoPagoPreferenceCreateResult(
    string PreferenceId,
    string InitPointUrl);

public sealed record CreatePaymentPreferenceResponse(
    string PreferenceId,
    string InitPointUrl,
    string ExternalReference);

public sealed record PaymentLinkSnapshot(
    Guid OrderId,
    string PreferenceId,
    decimal ExpectedAmount,
    string ExpectedCurrency,
    DateTimeOffset CreatedAtUtc);

public sealed record PaymentPreferenceSettings(
    string NotificationUrl,
    string SuccessUrl,
    string FailureUrl,
    string PendingUrl);

public sealed class PaymentPreferenceOrderNotFoundException : InvalidOperationException
{
    public PaymentPreferenceOrderNotFoundException(Guid orderId)
        : base($"Order '{orderId}' was not found.")
    {
        OrderId = orderId;
    }

    public Guid OrderId { get; }
}

public sealed class PaymentPreferenceProviderException : InvalidOperationException
{
    public PaymentPreferenceProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
