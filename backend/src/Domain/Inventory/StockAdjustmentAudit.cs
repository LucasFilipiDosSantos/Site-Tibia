namespace Domain.Inventory;

public sealed class StockAdjustmentAudit
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; }
    public Guid AdminUserId { get; private set; }
    public int Delta { get; private set; }
    public int BeforeQuantity { get; private set; }
    public int AfterQuantity { get; private set; }
    public string Reason { get; private set; }
    public DateTimeOffset AdjustedAtUtc { get; private set; }

    public StockAdjustmentAudit(
        Guid productId,
        Guid adminUserId,
        int delta,
        int beforeQuantity,
        int afterQuantity,
        string reason,
        DateTimeOffset adjustedAtUtc)
    {
        ProductId = productId == Guid.Empty
            ? throw new ArgumentException("Product id is required.", nameof(productId))
            : productId;

        AdminUserId = adminUserId == Guid.Empty
            ? throw new ArgumentException("Admin user id is required.", nameof(adminUserId))
            : adminUserId;

        Delta = delta;
        BeforeQuantity = beforeQuantity;
        AfterQuantity = afterQuantity;
        Reason = string.IsNullOrWhiteSpace(reason)
            ? throw new ArgumentException("Adjustment reason is required.", nameof(reason))
            : reason.Trim();
        AdjustedAtUtc = adjustedAtUtc;
    }

    private StockAdjustmentAudit()
    {
        Reason = string.Empty;
    }
}
