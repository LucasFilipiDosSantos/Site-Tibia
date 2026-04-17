namespace Application.Inventory.Contracts;

public interface IInventoryRepository
{
    Task<InventoryReservationRecord?> GetReservationByIntentKeyAsync(
        string orderIntentKey,
        CancellationToken cancellationToken = default
    );

    Task<ReserveInventoryResult> TryReserveAsync(
        ReserveInventoryAttempt attempt,
        CancellationToken cancellationToken = default
    );

    Task<int> ReleaseReservationAsync(
        string orderIntentKey,
        ReservationReleaseReason reason,
        DateTimeOffset releasedAtUtc,
        CancellationToken cancellationToken = default
    );

    Task<InventoryAvailabilityResponse> GetAvailabilityAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<AdjustStockResponse> AdjustStockAsync(
        StockAdjustmentCommand command,
        CancellationToken cancellationToken = default
    );

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
