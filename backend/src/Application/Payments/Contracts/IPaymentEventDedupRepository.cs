using Application.Payments.Contracts;

namespace Application.Payments.Contracts;

/// <summary>
/// Repository for webhook idempotency/dedupe guard (D-06)
/// </summary>
public interface IPaymentEventDedupRepository
{
    /// <summary>
    /// Try to claim dedupe lock. Returns false if already processed.
    /// </summary>
    Task<bool> TryClaimAsync(string providerResourceId, string action, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if event was already processed.
    /// </summary>
    Task<bool> IsProcessedAsync(string providerResourceId, string action, CancellationToken cancellationToken = default);
}