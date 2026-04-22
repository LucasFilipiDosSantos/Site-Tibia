using Application.Inventory.Contracts;
using Domain.Inventory;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Inventory.Repositories;

public sealed class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _dbContext;

    public InventoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InventoryReservationRecord?> GetReservationByIntentAndProductAsync(
        string orderIntentKey,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var normalized = orderIntentKey.Trim();
        var reservation = (await _dbContext.InventoryReservations
            .AsNoTracking()
            .Where(x => x.OrderIntentKey == normalized && x.ProductId == productId)
            .ToListAsync(cancellationToken))
            .OrderByDescending(x => x.ReservedAtUtc)
            .FirstOrDefault();

        if (reservation is null)
        {
            return null;
        }

        return new InventoryReservationRecord(
            reservation.OrderIntentKey,
            reservation.OrderId,
            reservation.ProductId,
            reservation.Quantity,
            reservation.ReservedAtUtc,
            reservation.ReservationExpiresAtUtc,
            reservation.ReleasedAtUtc);
    }

    public async Task<InventoryReservationRecord?> GetReservationByIntentKeyAsync(
        string orderIntentKey,
        CancellationToken cancellationToken = default)
    {
        var normalized = orderIntentKey.Trim();
        var reservation = (await _dbContext.InventoryReservations
            .AsNoTracking()
            .Where(x => x.OrderIntentKey == normalized)
            .ToListAsync(cancellationToken))
            .OrderByDescending(x => x.ReservedAtUtc)
            .FirstOrDefault();

        if (reservation is null)
        {
            return null;
        }

        return new InventoryReservationRecord(
            reservation.OrderIntentKey,
            reservation.OrderId,
            reservation.ProductId,
            reservation.Quantity,
            reservation.ReservedAtUtc,
            reservation.ReservationExpiresAtUtc,
            reservation.ReleasedAtUtc);
    }

    public async Task<ReserveInventoryResult> TryReserveAsync(
        ReserveInventoryAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var duplicate = await _dbContext.InventoryReservations
            .AsNoTracking()
            .AnyAsync(x => x.OrderIntentKey == attempt.OrderIntentKey && x.ProductId == attempt.ProductId, cancellationToken);

        if (duplicate)
        {
            await tx.CommitAsync(cancellationToken);
            return ReserveInventoryResult.Reserved();
        }

        var stock = await _dbContext.InventoryStocks.SingleAsync(x => x.ProductId == attempt.ProductId, cancellationToken);
        if (!stock.TryReserve(attempt.Quantity, attempt.ReservedAtUtc))
        {
            var available = stock.AvailableQuantity;
            await tx.RollbackAsync(cancellationToken);
            return ReserveInventoryResult.Conflict(available);
        }

        var reservation = new InventoryReservation(
            attempt.OrderIntentKey,
            attempt.OrderId,
            attempt.ProductId,
            attempt.Quantity,
            attempt.ReservedAtUtc,
            attempt.ReservationExpiresAtUtc);

        _dbContext.InventoryReservations.Add(reservation);
        _dbContext.InventoryStocks.Update(stock);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return ReserveInventoryResult.Reserved();
    }

    public async Task<int> ReleaseReservationAsync(
        string orderIntentKey,
        ReservationReleaseReason reason,
        DateTimeOffset releasedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var normalized = orderIntentKey.Trim();

        var activeReservations = await _dbContext.InventoryReservations
            .Where(x => x.OrderIntentKey == normalized && x.ReleasedAtUtc == null)
            .ToListAsync(cancellationToken);

        if (activeReservations.Count == 0)
        {
            await tx.CommitAsync(cancellationToken);
            return 0;
        }

        var releasedByProduct = new Dictionary<Guid, int>();
        foreach (var reservation in activeReservations)
        {
            var released = reservation.Release(releasedAtUtc, reason.ToString());
            if (released <= 0)
            {
                continue;
            }

            if (releasedByProduct.TryGetValue(reservation.ProductId, out var existing))
            {
                releasedByProduct[reservation.ProductId] = existing + released;
            }
            else
            {
                releasedByProduct[reservation.ProductId] = released;
            }
        }

        if (releasedByProduct.Count == 0)
        {
            await tx.CommitAsync(cancellationToken);
            return 0;
        }

        var productIds = releasedByProduct.Keys.ToList();
        var stocksByProduct = await _dbContext.InventoryStocks
            .Where(x => productIds.Contains(x.ProductId))
            .ToDictionaryAsync(x => x.ProductId, cancellationToken);

        var totalReleased = 0;
        foreach (var (productId, quantityToRelease) in releasedByProduct)
        {
            if (!stocksByProduct.TryGetValue(productId, out var stock))
            {
                throw new InvalidOperationException(
                    $"Inventory stock not found for product '{productId}' while releasing reservation intent '{normalized}'.");
            }

            totalReleased += stock.Release(quantityToRelease, releasedAtUtc);
        }

        _dbContext.InventoryReservations.UpdateRange(activeReservations);
        _dbContext.InventoryStocks.UpdateRange(stocksByProduct.Values);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return totalReleased;
    }

    public async Task<InventoryAvailabilityResponse> GetAvailabilityAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var stock = await _dbContext.InventoryStocks
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);

        return stock is null
            ? new InventoryAvailabilityResponse(0, 0, 0)
            : new InventoryAvailabilityResponse(stock.AvailableQuantity, stock.ReservedQuantity, stock.TotalQuantity);
    }

    public async Task<AdjustStockResponse> AdjustStockAsync(
        StockAdjustmentCommand command,
        CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var stock = await _dbContext.InventoryStocks.SingleOrDefaultAsync(x => x.ProductId == command.ProductId, cancellationToken);
                var isNewStock = stock is null;
                if (stock is null)
                {
                    stock = new InventoryStock(command.ProductId, totalQuantity: 0, reservedQuantity: 0, command.AdjustedAtUtc);
                    await _dbContext.InventoryStocks.AddAsync(stock, cancellationToken);
                }

                stock.ApplyDelta(command.Delta, command.AdjustedAtUtc);

                var audit = new StockAdjustmentAudit(
                    command.ProductId,
                    command.AdminUserId,
                    command.Delta,
                    command.BeforeQuantity,
                    command.AfterQuantity,
                    command.Reason,
                    command.AdjustedAtUtc);

                if (!isNewStock)
                {
                    _dbContext.InventoryStocks.Update(stock);
                }

                _dbContext.StockAdjustmentAudits.Add(audit);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return new AdjustStockResponse(
                    command.ProductId,
                    command.Delta,
                    command.BeforeQuantity,
                    command.AfterQuantity,
                    command.Reason,
                    command.AdminUserId,
                    command.AdjustedAtUtc);
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                foreach (var entry in _dbContext.ChangeTracker.Entries().ToList())
                {
                    entry.State = EntityState.Detached;
                }
            }
        }

        throw new DbUpdateConcurrencyException("Inventory stock adjustment exceeded retry limit due to concurrent writes.");
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
