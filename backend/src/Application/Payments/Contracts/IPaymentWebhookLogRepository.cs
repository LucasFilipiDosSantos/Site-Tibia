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
    Task<IReadOnlyList<PaymentWebhookLogEntry>> QueryAsync(
        DateTime? from,
        DateTime? to,
        PaymentWebhookValidationOutcome? validationOutcome,
        string? providerResourceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<long> CountAsync(
        DateTime? from,
        DateTime? to,
        PaymentWebhookValidationOutcome? validationOutcome,
        string? providerResourceId,
        CancellationToken cancellationToken = default);
}
