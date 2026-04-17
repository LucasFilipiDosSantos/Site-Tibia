using Application.Inventory.Contracts;
using Application.Inventory.Services;

namespace UnitTests.Inventory;

public sealed class InventoryAdjustmentInvariantTests
{
    [Fact]
    public async Task AdjustStockAsync_RejectsMissingReason()
    {
        var now = new DateTimeOffset(2026, 4, 17, 11, 0, 0, TimeSpan.Zero);
        var service = new InventoryService(new InMemoryInventoryRepository(), new FixedClock(now));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AdjustStockAsync(new AdjustStockRequest(Guid.NewGuid(), 1, "   ", Guid.NewGuid())));

        Assert.Equal("reason", ex.ParamName);
    }

    [Fact]
    public async Task AdjustStockAsync_RejectsZeroDelta_DeltaOnlyContract()
    {
        var now = new DateTimeOffset(2026, 4, 17, 11, 0, 0, TimeSpan.Zero);
        var service = new InventoryService(new InMemoryInventoryRepository(), new FixedClock(now));

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.AdjustStockAsync(new AdjustStockRequest(Guid.NewGuid(), 0, "sync", Guid.NewGuid())));

        Assert.Equal("delta", ex.ParamName);
    }

    [Fact]
    public async Task AdjustStockAsync_RejectsNegativeResultingStock()
    {
        var now = new DateTimeOffset(2026, 4, 17, 11, 0, 0, TimeSpan.Zero);
        var productId = Guid.NewGuid();
        var repository = new InMemoryInventoryRepository
        {
            Availability = new InventoryAvailabilityResponse(3, 0, 3)
        };

        var service = new InventoryService(repository, new FixedClock(now));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AdjustStockAsync(new AdjustStockRequest(productId, -4, "manual correction", Guid.NewGuid())));

        Assert.Contains("negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FixedClock : Application.Identity.Contracts.ISystemClock
    {
        public FixedClock(DateTimeOffset now) => UtcNow = now;
        public DateTimeOffset UtcNow { get; }
    }

    private sealed class InMemoryInventoryRepository : IInventoryRepository
    {
        public InventoryAvailabilityResponse Availability { get; set; } = new(0, 0, 0);

        public Task<InventoryReservationRecord?> GetReservationByIntentAndProductAsync(
            string orderIntentKey,
            Guid productId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<InventoryReservationRecord?>(null);

        public Task<InventoryReservationRecord?> GetReservationByIntentKeyAsync(string orderIntentKey, CancellationToken cancellationToken = default)
            => Task.FromResult<InventoryReservationRecord?>(null);

        public Task<ReserveInventoryResult> TryReserveAsync(ReserveInventoryAttempt attempt, CancellationToken cancellationToken = default)
            => Task.FromResult(ReserveInventoryResult.Reserved());

        public Task<int> ReleaseReservationAsync(
            string orderIntentKey,
            ReservationReleaseReason reason,
            DateTimeOffset releasedAtUtc,
            CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<InventoryAvailabilityResponse> GetAvailabilityAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(Availability);

        public Task<AdjustStockResponse> AdjustStockAsync(StockAdjustmentCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(new AdjustStockResponse(command.ProductId, command.Delta, command.BeforeQuantity, command.AfterQuantity, command.Reason, command.AdminUserId, command.AdjustedAtUtc));

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
