namespace Domain.Checkout;

/// <summary>
/// Domain event published when a delivery instruction is marked Failed.
/// Carries notification metadata for automatic WhatsApp notification enqueue.
/// </summary>
public sealed class DeliveryFailedEvent
{
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public string? NotificationPhone { get; }
    public string FailureReason { get; }
    public DateTimeOffset StatusAtUtc { get; }
    public NotificationEventType EventType { get; } = NotificationEventType.DeliveryFailed;

    public DeliveryFailedEvent(
        Guid orderId,
        Guid customerId,
        string? notificationPhone,
        string failureReason,
        DateTimeOffset statusAtUtc)
    {
        OrderId = orderId;
        CustomerId = customerId;
        NotificationPhone = notificationPhone;
        FailureReason = failureReason;
        StatusAtUtc = statusAtUtc;
    }
}