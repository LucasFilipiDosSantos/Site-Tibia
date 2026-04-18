using Application.Payments.Contracts;

namespace Application.Payments.Services;

/// <summary>
/// Idempotent async webhook processing pipeline (D-06, D-07, D-08, D-13, D-14)
/// </summary>
public sealed class PaymentWebhookProcessor : IPaymentWebhookProcessor
{
    private readonly IPaymentWebhookLogRepository _logRepository;
    private readonly IPaymentEventDedupRepository _dedupRepository;
    private readonly IPaymentStatusEventRepository _statusEventRepository;

    public PaymentWebhookProcessor(
        IPaymentWebhookLogRepository logRepository,
        IPaymentEventDedupRepository dedupRepository,
        IPaymentStatusEventRepository statusEventRepository)
    {
        _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
        _dedupRepository = dedupRepository ?? throw new ArgumentNullException(nameof(dedupRepository));
        _statusEventRepository = statusEventRepository ?? throw new ArgumentNullException(nameof(statusEventRepository));
    }

    /// <inheritdoc/>
    public async Task<WebhookProcessingOutcome> ProcessAsync(Guid webhookLogId, CancellationToken cancellationToken = default)
    {
        // Load webhook log
        var logEntry = await _logRepository.GetByIdAsync(webhookLogId, cancellationToken);
        if (logEntry == null)
        {
            return WebhookProcessingOutcome.Failed("Webhook log not found");
        }

        // D-06: Try to claim dedupe lock
        var claimed = await _dedupRepository.TryClaimAsync(
            logEntry.ProviderResourceId, 
            logEntry.Action, 
            cancellationToken);
        
        if (!claimed)
        {
            // D-07: Duplicate - already processed, return success with duplicate flag
            return WebhookProcessingOutcome.Duplicate();
        }

        // Map provider action to payment status
        var status = MapActionToStatus(logEntry.Action);
        
        // D-08: Monotonic guard - check for regression
        var latestEvent = await _statusEventRepository.GetLatestAsync(
            logEntry.ProviderResourceId, 
            cancellationToken);
        
        if (latestEvent != null && IsRegression(latestEvent.Status, status))
        {
            // Ignore regressions - log but don't update
            return WebhookProcessingOutcome.Succeeded();
        }

        // Persist normalized payment status event
        var statusEvent = new PaymentStatusEvent(
            Id: Guid.NewGuid(),
            OrderId: Guid.Empty, // Will be resolved from payment link
            ProviderResourceId: logEntry.ProviderResourceId,
            Action: logEntry.Action,
            Status: status,
            ReceivedAtUtc: logEntry.ReceivedAtUtc,
            FailureReason: null
        );
        
        await _statusEventRepository.AddAsync(statusEvent, cancellationToken);

        // TODO: D-09-D-12: Transition order via OrderLifecycleService (in next plan)

        return WebhookProcessingOutcome.Succeeded();
    }

    private static string MapActionToStatus(string action) => action switch
    {
        "payment.created" => "pending",
        "payment.pending" => "pending",
        "payment.authorized" => "authorized",
        "payment.processed" => "processed",
        "payment.approved" => "approved",
        "payment.rejected" => "rejected",
        "payment.cancelled" => "cancelled",
        "payment.refunded" => "refunded",
        _ => "unknown"
    };

    private static bool IsRegression(string currentStatus, string newStatus)
    {
        // Define monotonic order
        var statusOrder = new Dictionary<string, int>
        {
            { "pending", 1 },
            { "authorized", 2 },
            { "in_process", 3 },
            { "processed", 4 },
            { "approved", 5 },
            { "rejected", 1 },  // Rejected resets to beginning
            { "cancelled", 0 },
            { "refunded", 0 },
            { "unknown", 0 }
        };

        var currentOrder = statusOrder.GetValueOrDefault(currentStatus, 0);
        var newOrder = statusOrder.GetValueOrDefault(newStatus, 0);
        
        // Regression if new status is lower than current
        return newOrder < currentOrder;
    }
}