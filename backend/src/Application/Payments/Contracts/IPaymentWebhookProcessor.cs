using Application.Payments.Contracts;

namespace Application.Payments.Contracts;

/// <summary>
/// Interface for async webhook processing (D-13, D-14)
/// </summary>
public interface IPaymentWebhookProcessor
{
    /// <summary>
    /// Process webhook asynchronously with idempotency and dedupe.
    /// </summary>
    Task<WebhookProcessingOutcome> ProcessAsync(Guid webhookLogId, CancellationToken cancellationToken = default);
}