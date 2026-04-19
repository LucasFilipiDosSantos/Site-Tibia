using Domain.Checkout;

namespace Application.Notifications;

/// <summary>
/// Interface for publishing notification events to background job processing.
/// D-01: Automatic notification enqueue triggers on lifecycle events.
/// D-02: Idempotency key = OrderId + EventType + StatusAtUtc
/// D-04: Failure does not rollback business transition.
/// D-14: Correlation spans full chain: Payment -> Order -> Fulfillment -> Notification.
/// </summary>
public interface INotificationPublisher
{
    /// <summary>
    /// Publishes an OrderPaidEvent for automatic notification processing with correlation ID.
    /// </summary>
    Task PublishOrderPaidAsync(Order order, DateTimeOffset statusAtUtc, string? correlationId = null, CancellationToken ct = default);

    /// <summary>
    /// Publishes a DeliveryCompletedEvent for automatic notification processing with correlation ID.
    /// </summary>
    Task PublishDeliveryCompletedAsync(Order order, DateTimeOffset statusAtUtc, string? correlationId = null, CancellationToken ct = default);

    /// <summary>
    /// Publishes a DeliveryFailedEvent for automatic notification processing with correlation ID.
    /// </summary>
    Task PublishDeliveryFailedAsync(Order order, string failureReason, DateTimeOffset statusAtUtc, string? correlationId = null, CancellationToken ct = default);
}