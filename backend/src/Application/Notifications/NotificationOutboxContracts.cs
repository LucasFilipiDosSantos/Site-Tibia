using Domain.Checkout;

namespace Application.Notifications;

/// <summary>
/// Repository for notification outbox to store failed notifications for retry.
/// D-04: Persist failure to outbox, do not rollback business transition.
/// </summary>
public interface INotificationOutboxRepository
{
    /// <summary>
    /// Checks if an idempotency key already exists (dedupe).
    /// </summary>
    Task<bool> ExistsAsync(string idempotencyKey, CancellationToken ct = default);

    /// <summary>
    /// Saves a failed notification for later retry.
    /// </summary>
    Task SaveFailedAsync(NotificationOutboxEntry entry, CancellationToken ct = default);
}

/// <summary>
/// Outbox entry for failed notifications.
/// D-02: Idempotency key = OrderId + EventType + StatusAtUtc
/// </summary>
public sealed class NotificationOutboxEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public NotificationEventType EventType { get; set; }
    public DateTimeOffset StatusAtUtc { get; set; }
    public string? NotificationPhone { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? NextRetryAtUtc { get; set; }
}