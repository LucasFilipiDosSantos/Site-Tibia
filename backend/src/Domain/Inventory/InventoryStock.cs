namespace Domain.Inventory;

public sealed class InventoryStock
{
    public Guid ProductId { get; private set; }
    public int TotalQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int AvailableQuantity => TotalQuantity - ReservedQuantity;
    public DateTimeOffset UpdatedAtUtc { get; private set; }

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

    private InventoryStock()
    {
    }
}
