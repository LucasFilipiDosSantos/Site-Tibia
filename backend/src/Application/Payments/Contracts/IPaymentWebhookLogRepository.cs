using Application.Payments.Contracts;

namespace Application.Payments.Contracts;

/// <summary>
/// Repository for inbound webhook log persistence
/// </summary>
public interface IPaymentWebhookLogRepository
{
    Task LogAsync(PaymentWebhookLogEntry entry, CancellationToken cancellationToken = default);
    Task<PaymentWebhookLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentWebhookLogEntry>> GetByProviderResourceIdAsync(string providerResourceId, CancellationToken cancellationToken = default);
}