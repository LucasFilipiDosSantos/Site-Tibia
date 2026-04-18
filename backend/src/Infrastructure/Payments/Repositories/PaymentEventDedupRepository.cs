using Application.Payments.Contracts;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Payments.Repositories;

/// <summary>
/// Repository for webhook idempotency/dedupe guard (D-06)
/// </summary>
public sealed class PaymentEventDedupRepository : IPaymentEventDedupRepository
{
    private readonly AppDbContext _context;

    public PaymentEventDedupRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<bool> TryClaimAsync(
        string providerResourceId, 
        string action, 
        CancellationToken cancellationToken = default)
    {
        // Check if already processed
        var existing = await _context.PaymentEventDedups
            .FirstOrDefaultAsync(e => e.ProviderResourceId == providerResourceId && e.Action == action, cancellationToken);
            
        if (existing != null)
        {
            return false; // Already processed
        }
        
        // Claim the lock
        var entity = new PaymentEventDedupEntity
        {
            Id = Guid.NewGuid(),
            ProviderResourceId = providerResourceId,
            Action = action,
            ProcessedAtUtc = DateTimeOffset.UtcNow
        };
        
        _context.PaymentEventDedups.Add(entity);
        
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Race condition - another process claimed it first
            return false;
        }
    }

    public async Task<bool> IsProcessedAsync(
        string providerResourceId, 
        string action, 
        CancellationToken cancellationToken = default)
    {
        return await _context.PaymentEventDedups
            .AnyAsync(e => e.ProviderResourceId == providerResourceId && e.Action == action, cancellationToken);
    }
}