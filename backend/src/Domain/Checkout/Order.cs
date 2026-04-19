namespace Domain.Checkout;

public sealed class Order
{
    private readonly List<OrderItemSnapshot> _items = [];
    private readonly List<DeliveryInstruction> _deliveryInstructions = [];
    private readonly List<OrderStatusTransitionEvent> _statusHistory = [];

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string OrderIntentKey { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public IReadOnlyList<OrderItemSnapshot> Items => _items;
    public IReadOnlyList<DeliveryInstruction> DeliveryInstructions => _deliveryInstructions;
    public IReadOnlyList<OrderStatusTransitionEvent> StatusHistory => _statusHistory;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    // Notification metadata snapshot (immutable after order creation)
    public string? NotificationPhone { get; private set; }
    public bool NotificationAvailable { get; private set; }
    public string? NotificationFailedReason { get; private set; }

    public Order(Guid id, Guid customerId, string orderIntentKey)
    {
        Id = id == Guid.Empty ? throw new ArgumentException("Order id is required.", nameof(id)) : id;
        CustomerId = customerId == Guid.Empty
            ? throw new ArgumentException("Customer id is required.", nameof(customerId))
            : customerId;
        OrderIntentKey = string.IsNullOrWhiteSpace(orderIntentKey)
            ? throw new ArgumentException("Order intent key is required.", nameof(orderIntentKey))
            : orderIntentKey.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
        Status = OrderStatus.Pending;
    }

    public void AddItemSnapshot(OrderItemSnapshot item)
    {
        _items.Add(item);
    }

    public void AddDeliveryInstruction(DeliveryInstruction instruction)
    {
        _deliveryInstructions.Add(instruction);
    }

    /// <summary>
    /// Set notification phone metadata at order creation time (D-08, D-09, D-10).
    /// Phone is snapshotted immutably for deterministic notification replay.
    /// </summary>
    public void SetNotificationMetadata(string? phone, bool available, string? failedReason = null)
    {
        NotificationPhone = available && !string.IsNullOrWhiteSpace(phone) ? phone : null;
        NotificationAvailable = available && NotificationPhone is not null;
        NotificationFailedReason = !NotificationAvailable ? (failedReason ?? "missing-contact") : null;
    }

    /// <summary>
    /// Apply transition - records event only if status actually changes (D-04, D-05)
    /// </summary>
    public void ApplyTransition(OrderStatus newStatus, TransitionSourceType source, DateTimeOffset occurredAtUtc, Guid? actorUserId = null, string? reason = null)
    {
        // Idempotent no-op if already at target status (D-04)
        if (Status == newStatus)
            return;

        var fromStatus = Status;
        var allowedTransitions = GetAllowedTransitionsForStatus(fromStatus, source);

        if (!allowedTransitions.Contains(newStatus))
        {
            throw new InvalidOperationException(
                $"Cannot transition from {fromStatus} to {newStatus} by {source}. Allowed: {string.Join(", ", allowedTransitions)}");
        }

        // Append history event only on real change (D-05, D-06, D-07)
        var transitionEvent = new OrderStatusTransitionEvent(Id, fromStatus, newStatus, source, occurredAtUtc, actorUserId, reason);
        _statusHistory.Add(transitionEvent);
        Status = newStatus;
    }

    /// <summary>
    /// Get allowed transitions for a given status/source (D-01, D-02, D-03)
    /// </summary>
    public static IReadOnlyList<OrderStatus> GetAllowedTransitionsForStatus(OrderStatus current, TransitionSourceType source)
    {
        return (current, source) switch
        {
            // Pending: System can set Paid (D-03)
            (OrderStatus.Pending, TransitionSourceType.System) => [OrderStatus.Paid],
            // Pending: Customer/Admin can cancel (D-02)
            (OrderStatus.Pending, TransitionSourceType.Customer) => [OrderStatus.Cancelled],
            (OrderStatus.Pending, TransitionSourceType.Admin) => [OrderStatus.Cancelled],
            // Paid: No transitions allowed (D-02)
            (OrderStatus.Paid, TransitionSourceType.System) => [],
            (OrderStatus.Paid, TransitionSourceType.Customer) => [],
            (OrderStatus.Paid, TransitionSourceType.Admin) => [],
            // Cancelled: Terminal state - no transitions
            (OrderStatus.Cancelled, TransitionSourceType.System) => [],
            (OrderStatus.Cancelled, TransitionSourceType.Customer) => [],
            (OrderStatus.Cancelled, TransitionSourceType.Admin) => [],
            _ => []
        };
    }

    private Order()
    {
        OrderIntentKey = string.Empty;
    }
}