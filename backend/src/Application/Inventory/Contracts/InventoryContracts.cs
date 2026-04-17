namespace Application.Inventory.Contracts;

public sealed record ReserveStockForCheckoutRequest(
    string OrderIntentKey,
    Guid OrderId,
    Guid ProductId,
    int Quantity
);

public sealed record ReserveStockForCheckoutResponse(
    string OrderIntentKey,
    Guid OrderId,
    Guid ProductId,
    int Quantity,
    DateTimeOffset ReservationExpiresAtUtc
);

public sealed record ReleaseReservationRequest(string OrderIntentKey, ReservationReleaseReason Reason);

public sealed record ReleaseReservationResponse(string OrderIntentKey, Guid ProductId, int ReleasedQuantity);

public sealed record GetInventoryAvailabilityRequest(Guid ProductId);

public sealed record InventoryAvailabilityResponse(int Available, int Reserved, int Total);

public sealed record AdjustStockRequest(Guid ProductId, int Delta, string Reason, Guid AdminUserId);

public sealed record AdjustStockResponse(
    Guid ProductId,
    int Delta,
    int BeforeQuantity,
    int AfterQuantity,
    string Reason,
    Guid AdminUserId,
    DateTimeOffset AdjustedAtUtc
);

public sealed record InventoryReservationRecord(
    string OrderIntentKey,
    Guid OrderId,
    Guid ProductId,
    int Quantity,
    DateTimeOffset ReservedAtUtc,
    DateTimeOffset ReservationExpiresAtUtc,
    DateTimeOffset? ReleasedAtUtc
)
{
    public bool IsReleased => ReleasedAtUtc.HasValue;
    public bool IsExpired(DateTimeOffset nowUtc) => ReservationExpiresAtUtc <= nowUtc;
}

public sealed record ReserveInventoryAttempt(
    string OrderIntentKey,
    Guid OrderId,
    Guid ProductId,
    int Quantity,
    DateTimeOffset ReservedAtUtc,
    DateTimeOffset ReservationExpiresAtUtc
);

public sealed record ReserveInventoryResult(bool Success, int AvailableQuantityAfterFailure)
{
    public static ReserveInventoryResult Reserved() => new(true, 0);
    public static ReserveInventoryResult Conflict(int availableQuantity) => new(false, availableQuantity);
}

public sealed record StockAdjustmentCommand(
    Guid ProductId,
    int Delta,
    int BeforeQuantity,
    int AfterQuantity,
    string Reason,
    Guid AdminUserId,
    DateTimeOffset AdjustedAtUtc
);

public enum ReservationReleaseReason
{
    PaymentFailed = 1,
    OrderCanceled = 2,
    Expired = 3
}

public sealed class InventoryReservationConflictException : InvalidOperationException
{
    public InventoryReservationConflictException(Guid productId, int requestedQuantity, int availableQuantity)
        : base($"Requested quantity {requestedQuantity} exceeds available stock ({availableQuantity}) for product '{productId}'.")
    {
        ProductId = productId;
        RequestedQuantity = requestedQuantity;
        AvailableQuantity = availableQuantity;
    }

    public Guid ProductId { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }
}
