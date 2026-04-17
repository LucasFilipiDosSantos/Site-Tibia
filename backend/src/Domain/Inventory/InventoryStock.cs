namespace Domain.Inventory;

public sealed class InventoryStock
{
    public Guid ProductId { get; private set; }
    public int TotalQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int AvailableQuantity => TotalQuantity - ReservedQuantity;
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public int ConcurrencyVersion { get; private set; }

    public InventoryStock(Guid productId, int totalQuantity, int reservedQuantity, DateTimeOffset updatedAtUtc)
    {
        ProductId = productId == Guid.Empty
            ? throw new ArgumentException("Product id is required.", nameof(productId))
            : productId;

        if (totalQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalQuantity), "Total quantity cannot be negative.");
        }

        if (reservedQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reservedQuantity), "Reserved quantity cannot be negative.");
        }

        if (reservedQuantity > totalQuantity)
        {
            throw new ArgumentException("Reserved quantity cannot exceed total quantity.", nameof(reservedQuantity));
        }

        TotalQuantity = totalQuantity;
        ReservedQuantity = reservedQuantity;
        UpdatedAtUtc = updatedAtUtc;
    }

    public bool TryReserve(int quantity, DateTimeOffset updatedAtUtc)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Reservation quantity must be greater than zero.");
        }

        if (AvailableQuantity < quantity)
        {
            return false;
        }

        ReservedQuantity += quantity;
        Touch(updatedAtUtc);
        return true;
    }

    public int Release(int quantity, DateTimeOffset updatedAtUtc)
    {
        if (quantity <= 0)
        {
            return 0;
        }

        var released = Math.Min(ReservedQuantity, quantity);
        ReservedQuantity -= released;
        Touch(updatedAtUtc);
        return released;
    }

    public void ApplyDelta(int delta, DateTimeOffset updatedAtUtc)
    {
        var after = TotalQuantity + delta;
        if (after < 0)
        {
            throw new InvalidOperationException("Stock adjustment cannot produce negative total quantity.");
        }

        if (after < ReservedQuantity)
        {
            throw new InvalidOperationException("Stock adjustment cannot reduce total below reserved quantity.");
        }

        TotalQuantity = after;
        Touch(updatedAtUtc);
    }

    private void Touch(DateTimeOffset updatedAtUtc)
    {
        UpdatedAtUtc = updatedAtUtc;
        ConcurrencyVersion++;
    }

    private InventoryStock()
    {
    }
}
