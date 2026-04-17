namespace Domain.Inventory;

public sealed class InventoryReservation
{
    public string OrderIntentKey { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public DateTimeOffset ReservedAtUtc { get; private set; }
    public DateTimeOffset ReservationExpiresAtUtc { get; private set; }
    public DateTimeOffset? ReleasedAtUtc { get; private set; }
    public string? ReleaseReason { get; private set; }

    public bool IsReleased => ReleasedAtUtc.HasValue;

    public InventoryReservation(
        string orderIntentKey,
        Guid orderId,
        Guid productId,
        int quantity,
        DateTimeOffset reservedAtUtc,
        DateTimeOffset reservationExpiresAtUtc,
        DateTimeOffset? releasedAtUtc = null,
        string? releaseReason = null)
    {
        OrderIntentKey = string.IsNullOrWhiteSpace(orderIntentKey)
            ? throw new ArgumentException("Order intent key is required.", nameof(orderIntentKey))
            : orderIntentKey.Trim();

        OrderId = orderId == Guid.Empty
            ? throw new ArgumentException("Order id is required.", nameof(orderId))
            : orderId;

        ProductId = productId == Guid.Empty
            ? throw new ArgumentException("Product id is required.", nameof(productId))
            : productId;

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Reservation quantity must be greater than zero.");
        }

        if (reservationExpiresAtUtc <= reservedAtUtc)
        {
            throw new ArgumentException("Reservation expiry must be after reserve timestamp.", nameof(reservationExpiresAtUtc));
        }

        Quantity = quantity;
        ReservedAtUtc = reservedAtUtc;
        ReservationExpiresAtUtc = reservationExpiresAtUtc;
        ReleasedAtUtc = releasedAtUtc;
        ReleaseReason = releaseReason;
    }

    private InventoryReservation()
    {
        OrderIntentKey = string.Empty;
    }
}
