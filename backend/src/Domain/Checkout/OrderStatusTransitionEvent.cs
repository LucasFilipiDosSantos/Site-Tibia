namespace Domain.Checkout;

public enum TransitionSourceType
{
    System = 0,
    Admin = 1,
    Customer = 2
}

public sealed class OrderStatusTransitionEvent
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public OrderStatus FromStatus { get; private set; }
    public OrderStatus ToStatus { get; private set; }
    public TransitionSourceType SourceType { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? Reason { get; private set; }

    public OrderStatusTransitionEvent(
        Guid orderId,
        OrderStatus fromStatus,
        OrderStatus toStatus,
        TransitionSourceType sourceType,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId = null,
        string? reason = null)
    {
        Id = Guid.NewGuid();
        OrderId = orderId == Guid.Empty ? throw new ArgumentException("Order id is required.", nameof(orderId)) : orderId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        SourceType = sourceType;
        OccurredAtUtc = occurredAtUtc;
        ActorUserId = actorUserId;
        Reason = reason;
    }

    private OrderStatusTransitionEvent()
    {
    }
}