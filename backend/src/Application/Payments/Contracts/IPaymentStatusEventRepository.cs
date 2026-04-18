using Application.Payments.Contracts;

namespace Application.Payments.Contracts;

/// <summary>
/// Repository for normalized payment status events
/// </summary>
public interface IPaymentStatusEventRepository
{
    Task AddAsync(PaymentStatusEvent statusEvent, CancellationToken cancellationToken = default);
    Task<PaymentStatusEvent?> GetLatestAsync(string providerResourceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentStatusEvent>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Normalized payment status event
/// </summary>
public sealed record PaymentStatusEvent(
    Guid Id,
    Guid OrderId,
    string ProviderResourceId,
    string Action,
    string Status,
    DateTimeOffset ReceivedAtUtc,
    string? FailureReason);