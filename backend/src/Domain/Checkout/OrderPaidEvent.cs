namespace Domain.Checkout;

/// <summary>
/// Domain event published when an order transitions to Paid status.
/// Carries notification metadata for automatic WhatsApp notification enqueue.
/// </summary>
public sealed class OrderPaidEvent
{
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public string? NotificationPhone { get; }
    public string OrderNumber { get; }
    public DateTimeOffset StatusAtUtc { get; }
    public NotificationEventType EventType { get; } = NotificationEventType.OrderPaid;

    public OrderPaidEvent(
        Guid orderId,
        Guid customerId,
        string? notificationPhone,
        string orderNumber,
        DateTimeOffset statusAtUtc)
    {
        OrderId = orderId;
        CustomerId = customerId;
        NotificationPhone = notificationPhone;
        OrderNumber = orderNumber;
        StatusAtUtc = statusAtUtc;
    }
}

/// <summary>
/// Event type enum for notification idempotency keys.
/// </summary>
public enum NotificationEventType
{
    OrderPaid,
    DeliveryCompleted,
    DeliveryFailed
}