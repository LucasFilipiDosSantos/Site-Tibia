namespace API.Inventory;

/// <summary>
/// Availability response contract for a product inventory snapshot.
/// </summary>
public sealed record InventoryAvailabilityResponse(
    int Available,
    int Reserved,
    int Total);

/// <summary>
/// Reserve command payload used at checkout submit.
/// </summary>
public sealed record ReserveInventoryRequest(
    string OrderIntentKey,
    Guid OrderId,
    Guid ProductId,
    int Quantity);

/// <summary>
/// Reservation result contract for successful reserve operations.
/// </summary>
public sealed record ReserveInventoryResponse(
    string OrderIntentKey,
    Guid OrderId,
    Guid ProductId,
    int Quantity,
    DateTimeOffset ReservationExpiresAtUtc);

/// <summary>
/// Release command for explicit reservation release triggers.
/// </summary>
public sealed record ReleaseInventoryReservationRequest(
    string OrderIntentKey,
    ReservationReleaseReason Reason);

/// <summary>
/// Release result contract with released quantity details.
/// </summary>
public sealed record ReleaseInventoryReservationResponse(
    string OrderIntentKey,
    Guid ProductId,
    int ReleasedQuantity);

/// <summary>
/// Admin adjustment payload using delta-only semantics.
/// </summary>
public sealed record AdminAdjustInventoryRequest(
    Guid ProductId,
    int Delta,
    string Reason);

/// <summary>
/// Admin adjustment response containing persisted audit fields.
/// </summary>
public sealed record AdminAdjustInventoryResponse(
    Guid ProductId,
    int Delta,
    int BeforeQuantity,
    int AfterQuantity,
    string Reason,
    Guid AdminUserId,
    DateTimeOffset AdjustedAtUtc);

/// <summary>
/// Route contract for inventory availability by product id.
/// </summary>
public sealed record InventoryAvailabilityRouteRequest(Guid ProductId);

/// <summary>
/// Supported reservation release reason values.
/// </summary>
public enum ReservationReleaseReason
{
    PaymentFailed = 1,
    OrderCanceled = 2,
    Expired = 3
}
