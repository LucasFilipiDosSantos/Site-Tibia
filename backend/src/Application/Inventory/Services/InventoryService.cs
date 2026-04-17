using Application.Identity.Contracts;
using Application.Inventory.Contracts;

namespace Application.Inventory.Services;

public sealed class InventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ISystemClock _clock;

    public InventoryService(IInventoryRepository inventoryRepository, ISystemClock clock)
    {
        _inventoryRepository = inventoryRepository;
        _clock = clock;
    }

    public async Task<ReserveStockForCheckoutResponse> ReserveStockForCheckoutAsync(
        ReserveStockForCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.OrderIntentKey))
        {
            throw new ArgumentException("Order intent key is required.", nameof(request.OrderIntentKey));
        }

        if (request.OrderId == Guid.Empty)
        {
            throw new ArgumentException("Order id is required.", nameof(request.OrderId));
        }

        if (request.ProductId == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(request.ProductId));
        }

        if (request.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Quantity), "Quantity must be greater than zero.");
        }

        var normalizedIntentKey = request.OrderIntentKey.Trim();
        var existingReservation = await _inventoryRepository.GetReservationByIntentAndProductAsync(
            normalizedIntentKey,
            request.ProductId,
            cancellationToken);
        if (existingReservation is not null && !existingReservation.IsReleased)
        {
            if (existingReservation.Quantity != request.Quantity)
            {
                throw new InvalidOperationException(
                    "Existing reservation for order intent and product has a different quantity.");
            }

            return new ReserveStockForCheckoutResponse(
                existingReservation.OrderIntentKey,
                existingReservation.OrderId,
                existingReservation.ProductId,
                existingReservation.Quantity,
                existingReservation.ReservationExpiresAtUtc);
        }

        var nowUtc = _clock.UtcNow;
        var expiresAtUtc = nowUtc.AddMinutes(15);
        var attempt = new ReserveInventoryAttempt(
            normalizedIntentKey,
            request.OrderId,
            request.ProductId,
            request.Quantity,
            nowUtc,
            expiresAtUtc);

        var reserveResult = await _inventoryRepository.TryReserveAsync(attempt, cancellationToken);
        if (!reserveResult.Success)
        {
            throw new InventoryReservationConflictException(
                request.ProductId,
                request.Quantity,
                reserveResult.AvailableQuantityAfterFailure);
        }

        await _inventoryRepository.SaveChangesAsync(cancellationToken);

        return new ReserveStockForCheckoutResponse(
            normalizedIntentKey,
            request.OrderId,
            request.ProductId,
            request.Quantity,
            expiresAtUtc);
    }

    public async Task<ReleaseReservationResponse> ReleaseReservationAsync(
        ReleaseReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.OrderIntentKey))
        {
            throw new ArgumentException("Order intent key is required.", nameof(request.OrderIntentKey));
        }

        var normalizedIntentKey = request.OrderIntentKey.Trim();
        var reservation = await _inventoryRepository.GetReservationByIntentKeyAsync(normalizedIntentKey, cancellationToken)
            ?? throw new InvalidOperationException("Reservation not found.");

        if (reservation.IsReleased)
        {
            return new ReleaseReservationResponse(normalizedIntentKey, reservation.ProductId, 0);
        }

        var nowUtc = _clock.UtcNow;
        var releasedQuantity = await _inventoryRepository.ReleaseReservationAsync(
            normalizedIntentKey,
            request.Reason,
            nowUtc,
            cancellationToken);

        await _inventoryRepository.SaveChangesAsync(cancellationToken);

        return new ReleaseReservationResponse(normalizedIntentKey, reservation.ProductId, releasedQuantity);
    }

    public Task<InventoryAvailabilityResponse> GetAvailabilityAsync(
        GetInventoryAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProductId == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(request.ProductId));
        }

        return _inventoryRepository.GetAvailabilityAsync(request.ProductId, cancellationToken);
    }

    public async Task<AdjustStockResponse> AdjustStockAsync(
        AdjustStockRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProductId == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(request.ProductId));
        }

        if (request.AdminUserId == Guid.Empty)
        {
            throw new ArgumentException("Admin user id is required.", nameof(request.AdminUserId));
        }

        if (request.Delta == 0)
        {
            throw new ArgumentOutOfRangeException("delta", "Delta must be non-zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Adjustment reason is required.", "reason");
        }

        var nowUtc = _clock.UtcNow;
        var availability = await _inventoryRepository.GetAvailabilityAsync(request.ProductId, cancellationToken);
        var afterQuantity = availability.Total + request.Delta;
        if (afterQuantity < 0)
        {
            throw new InvalidOperationException("Stock adjustment cannot produce negative quantity.");
        }

        var command = new StockAdjustmentCommand(
            request.ProductId,
            request.Delta,
            availability.Total,
            afterQuantity,
            request.Reason.Trim(),
            request.AdminUserId,
            nowUtc);

        var result = await _inventoryRepository.AdjustStockAsync(command, cancellationToken);
        await _inventoryRepository.SaveChangesAsync(cancellationToken);
        return result;
    }
}
