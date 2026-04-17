namespace Domain.Checkout;

public sealed class Order
{
    private readonly List<OrderItemSnapshot> _items = [];
    private readonly List<DeliveryInstruction> _deliveryInstructions = [];

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string OrderIntentKey { get; private set; }
    public IReadOnlyList<OrderItemSnapshot> Items => _items;
    public IReadOnlyList<DeliveryInstruction> DeliveryInstructions => _deliveryInstructions;
    public DateTimeOffset CreatedAtUtc { get; private set; }

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
    }

    public void AddItemSnapshot(OrderItemSnapshot item)
    {
        _items.Add(item);
    }

    public void AddDeliveryInstruction(DeliveryInstruction instruction)
    {
        _deliveryInstructions.Add(instruction);
    }

    private Order()
    {
        OrderIntentKey = string.Empty;
    }
}
