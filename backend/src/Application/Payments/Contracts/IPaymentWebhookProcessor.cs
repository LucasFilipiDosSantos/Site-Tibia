namespace Application.Payments.Contracts;

/// <summary>
/// Interface for async webhook processing (D-13, D-14)
/// D-17: Observability closure includes failure-path assertions (invalid signature, dedupe duplicate, notification retry exhaustion)
/// </summary>
public interface IPaymentWebhookProcessor
{
    /// <summary>
    /// Process webhook asynchronously with idempotency and dedupe.
    /// D-14: Correlation spans full chain.
    /// </summary>
    Task<WebhookProcessingOutcome> ProcessAsync(Guid webhookLogId, string? correlationId = null, CancellationToken cancellationToken = default);
}