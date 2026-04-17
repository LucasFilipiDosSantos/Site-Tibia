using Application.Inventory.Contracts;
using Application.Inventory.Services;

namespace UnitTests.Inventory;

public sealed class InventoryReservationServiceTests
{
    [Fact]
    public async Task ReserveStockForCheckoutAsync_ReplaySameIntentAndProductAndQuantity_DoesNotDoubleReserve()
    {
        var now = new DateTimeOffset(2026, 4, 17, 11, 0, 0, TimeSpan.Zero);
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var repository = new InMemoryInventoryRepository
        {
            ReservationByIntent = new InventoryReservationRecord(
                "intent-001",
                orderId,
                productId,
                3,
                now,
                now.AddMinutes(15),
                null)
        };

        var service = new InventoryService(repository, new FixedClock(now));

        var response = await service.ReserveStockForCheckoutAsync(
            new ReserveStockForCheckoutRequest("intent-001", orderId, productId, 3));

        Assert.Equal("intent-001", response.OrderIntentKey);
        Assert.Equal(3, response.Quantity);
        Assert.Equal(now.AddMinutes(15), response.ReservationExpiresAtUtc);
        Assert.Equal(0, repository.TryReserveCallCount);
    }

    [Fact]
    public async Task ReserveStockForCheckoutAsync_SameIntentDifferentProduct_PerformsRealReserveAttempt()
    {
        var now = new DateTimeOffset(2026, 4, 17, 11, 0, 0, TimeSpan.Zero);
        var orderId = Guid.NewGuid();
        var reservedProductId = Guid.NewGuid();
        var requestedProductId = Guid.NewGuid();

        var repository = new InMemoryInventoryRepository
        {
            ReservationByIntent = new InventoryReservationRecord(
                "intent-001",
                orderId,
                reservedProductId,
                3,
                now,
                now.AddMinutes(15),
                null),
            TryReserveResult = ReserveInventoryResult.Reserved()
        };

        var service = new InventoryService(repository, new FixedClock(now));

        var response = await service.ReserveStockForCheckoutAsync(
            new ReserveStockForCheckoutRequest("intent-001", orderId, requestedProductId, 2));

        Assert.Equal(requestedProductId, response.ProductId);
        Assert.Equal(2, response.Quantity);
        Assert.Equal(1, repository.TryReserveCallCount);
        Assert.NotNull(repository.LastReserveAttempt);
        Assert.Equal(requestedProductId, repository.LastReserveAttempt!.ProductId);
    }

    [Fact]
    public async Task ReserveStockForCheckoutAsync_SameIntentAndProductDifferentQuantity_ThrowsInvalidOperation()
    {
        var now = new DateTimeOffset(2026, 4, 17, 11, 0, 0, TimeSpan.Zero);
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var repository = new InMemoryInventoryRepository
        {
            ReservationByIntent = new InventoryReservationRecord(
                "intent-001",
                orderId,
                productId,
                3,
                now,
                now.AddMinutes(15),
                null)
        };

        var service = new InventoryService(repository, new FixedClock(now));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReserveStockForCheckoutAsync(
                new ReserveStockForCheckoutRequest("intent-001", orderId, productId, 4)));

        Assert.Equal(0, repository.TryReserveCallCount);
    }

    [Fact]
    public async Task ReserveStockForCheckoutAsync_CalculatesFixedFifteenMinuteExpiry()
    {
        var now = new DateTimeOffset(2026, 4, 17, 11, 0, 0, TimeSpan.Zero);
        var repository = new InMemoryInventoryRepository
        {
            TryReserveResult = ReserveInventoryResult.Reserved()
        };

        var service = new InventoryService(repository, new FixedClock(now));
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var response = await service.ReserveStockForCheckoutAsync(
            new ReserveStockForCheckoutRequest("intent-002", orderId, productId, 4));

        Assert.Equal(now.AddMinutes(15), response.ReservationExpiresAtUtc);
        Assert.NotNull(repository.LastReserveAttempt);
        Assert.Equal(now.AddMinutes(15), repository.LastReserveAttempt!.ReservationExpiresAtUtc);
    }

    [Fact]
    public async Task ReserveStockForCheckoutAsync_WhenReserveFails_ThrowsConflictWithAvailableQuantity()
    {
        var now = new DateTimeOffset(2026, 4, 17, 11, 0, 0, TimeSpan.Zero);
        var productId = Guid.NewGuid();
        var repository = new InMemoryInventoryRepository
        {
            TryReserveResult = ReserveInventoryResult.Conflict(2)
        };

        var service = new InventoryService(repository, new FixedClock(now));

        var ex = await Assert.ThrowsAsync<InventoryReservationConflictException>(() =>
            service.ReserveStockForCheckoutAsync(
                new ReserveStockForCheckoutRequest("intent-003", Guid.NewGuid(), productId, 5)));

        Assert.Equal(2, ex.AvailableQuantity);
        Assert.Equal(5, ex.RequestedQuantity);
        Assert.Equal(productId, ex.ProductId);
    }

    [Fact]
    public async Task ReleaseReservationAsync_ExpiredReservationReleasesReservedQuantity()
    {
        var now = new DateTimeOffset(2026, 4, 17, 11, 16, 0, TimeSpan.Zero);
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var repository = new InMemoryInventoryRepository
        {
            ReservationByIntent = new InventoryReservationRecord(
                "intent-004",
                orderId,
                productId,
                6,
                now.AddMinutes(-16),
                now.AddMinutes(-1),
                null),
            ReleasedQuantity = 6
        };

        var service = new InventoryService(repository, new FixedClock(now));

        var released = await service.ReleaseReservationAsync(new ReleaseReservationRequest("intent-004", ReservationReleaseReason.Expired));

        Assert.Equal("intent-004", released.OrderIntentKey);
        Assert.Equal(6, released.ReleasedQuantity);
        Assert.Equal(ReservationReleaseReason.Expired, repository.LastReleaseReason);
    }

    [Fact]
    public async Task ReleaseReservationAsync_NotExpiredAndExplicitReason_StillReleasesImmediately()
    {
        var now = new DateTimeOffset(2026, 4, 17, 11, 10, 0, TimeSpan.Zero);
        var repository = new InMemoryInventoryRepository
        {
            ReservationByIntent = new InventoryReservationRecord(
                "intent-005",
                Guid.NewGuid(),
                Guid.NewGuid(),
                2,
                now.AddMinutes(-2),
                now.AddMinutes(13),
                null),
            ReleasedQuantity = 2
        };

        var service = new InventoryService(repository, new FixedClock(now));

        var released = await service.ReleaseReservationAsync(new ReleaseReservationRequest("intent-005", ReservationReleaseReason.OrderCanceled));

        Assert.Equal(2, released.ReleasedQuantity);
        Assert.Equal(ReservationReleaseReason.OrderCanceled, repository.LastReleaseReason);
    }

    [Fact]
    public async Task GetAvailabilityAsync_ReturnsRepositoryTruth()
    {
        var now = new DateTimeOffset(2026, 4, 17, 11, 0, 0, TimeSpan.Zero);
        var productId = Guid.NewGuid();
        var repository = new InMemoryInventoryRepository
        {
            Availability = new InventoryAvailabilityResponse(7, 3, 10)
        };

        var service = new InventoryService(repository, new FixedClock(now));

        var availability = await service.GetAvailabilityAsync(new GetInventoryAvailabilityRequest(productId));

        Assert.Equal(7, availability.Available);
        Assert.Equal(3, availability.Reserved);
        Assert.Equal(10, availability.Total);
        Assert.Equal(productId, repository.LastAvailabilityProductId);
    }

    private sealed class FixedClock : Application.Identity.Contracts.ISystemClock
    {
        public FixedClock(DateTimeOffset now) => UtcNow = now;
        public DateTimeOffset UtcNow { get; }
    }

    private sealed class InMemoryInventoryRepository : IInventoryRepository
    {
        public InventoryReservationRecord? ReservationByIntent { get; set; }
        public ReserveInventoryAttempt? LastReserveAttempt { get; private set; }
        public ReserveInventoryResult TryReserveResult { get; set; } = ReserveInventoryResult.Reserved();
        public int TryReserveCallCount { get; private set; }
        public int ReleasedQuantity { get; set; }
        public ReservationReleaseReason? LastReleaseReason { get; private set; }
        public Guid LastAvailabilityProductId { get; private set; }
        public InventoryAvailabilityResponse Availability { get; set; } = new(0, 0, 0);
        public StockAdjustmentCommand? LastAdjustmentCommand { get; private set; }
        public AdjustStockResponse AdjustmentResponse { get; set; } =
            new(Guid.Empty, 0, 0, 0, string.Empty, Guid.Empty, DateTimeOffset.UnixEpoch);

        public Task<InventoryReservationRecord?> GetReservationByIntentAndProductAsync(
            string orderIntentKey,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            if (ReservationByIntent is null)
            {
                return Task.FromResult<InventoryReservationRecord?>(null);
            }

            var matchesIntent = string.Equals(ReservationByIntent.OrderIntentKey, orderIntentKey.Trim(), StringComparison.Ordinal);
            var matchesProduct = ReservationByIntent.ProductId == productId;
            return Task.FromResult(matchesIntent && matchesProduct ? ReservationByIntent : null);
        }

        public Task<InventoryReservationRecord?> GetReservationByIntentKeyAsync(string orderIntentKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ReservationByIntent);
        }

        public Task<ReserveInventoryResult> TryReserveAsync(ReserveInventoryAttempt attempt, CancellationToken cancellationToken = default)
        {
            LastReserveAttempt = attempt;
            TryReserveCallCount++;
            return Task.FromResult(TryReserveResult);
        }

        public Task<int> ReleaseReservationAsync(
            string orderIntentKey,
            ReservationReleaseReason reason,
            DateTimeOffset releasedAtUtc,
            CancellationToken cancellationToken = default)
        {
            LastReleaseReason = reason;
            return Task.FromResult(ReleasedQuantity);
        }

        public Task<InventoryAvailabilityResponse> GetAvailabilityAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            LastAvailabilityProductId = productId;
            return Task.FromResult(Availability);
        }

        public Task<AdjustStockResponse> AdjustStockAsync(StockAdjustmentCommand command, CancellationToken cancellationToken = default)
        {
            LastAdjustmentCommand = command;
            return Task.FromResult(AdjustmentResponse);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
