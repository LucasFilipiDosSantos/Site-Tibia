namespace Domain.Checkout;

/// <summary>
/// Domain event published when a delivery instruction is marked Completed.
/// Carries notification metadata for automatic WhatsApp notification enqueue.
/// </summary>
public sealed class DeliveryCompletedEvent
{
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public string? NotificationPhone { get; }
    public FulfillmentType DeliveryType { get; }
    public DateTimeOffset StatusAtUtc { get; }
    public NotificationEventType EventType { get; } = NotificationEventType.DeliveryCompleted;

    public DeliveryCompletedEvent(
        Guid orderId,
        Guid customerId,
        string? notificationPhone,
        FulfillmentType deliveryType,
        DateTimeOffset statusAtUtc)
    {
        OrderId = orderId;
        CustomerId = customerId;
        NotificationPhone = notificationPhone;
        DeliveryType = deliveryType;
        StatusAtUtc = statusAtUtc;
    }
}