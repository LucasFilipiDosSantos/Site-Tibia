using Application.Payments.Contracts;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Payments.Repositories;

/// <summary>
/// Repository for normalized payment status events
/// </summary>
public sealed class PaymentStatusEventRepository : IPaymentStatusEventRepository
{
    private readonly AppDbContext _context;

    public PaymentStatusEventRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task AddAsync(PaymentStatusEvent statusEvent, CancellationToken cancellationToken = default)
    {
        var entity = new PaymentStatusEventEntity
        {
            Id = statusEvent.Id,
            OrderId = statusEvent.OrderId,
            ProviderResourceId = statusEvent.ProviderResourceId,
            Action = statusEvent.Action,
            Status = statusEvent.Status,
            ReceivedAtUtc = statusEvent.ReceivedAtUtc,
            FailureReason = statusEvent.FailureReason
        };
        
        _context.PaymentStatusEvents.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaymentStatusEvent?> GetLatestAsync(
        string providerResourceId, 
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.PaymentStatusEvents
            .Where(e => e.ProviderResourceId == providerResourceId)
            .OrderByDescending(e => e.ReceivedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (entity == null) return null;
        
        return new PaymentStatusEvent(
            Id: entity.Id,
            OrderId: entity.OrderId,
            ProviderResourceId: entity.ProviderResourceId,
            Action: entity.Action,
            Status: entity.Status,
            ReceivedAtUtc: entity.ReceivedAtUtc,
            FailureReason: entity.FailureReason);
    }

    public async Task<IReadOnlyList<PaymentStatusEvent>> GetByOrderIdAsync(
        Guid orderId, 
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.PaymentStatusEvents
            .Where(e => e.OrderId == orderId)
            .OrderByDescending(e => e.ReceivedAtUtc)
            .ToListAsync(cancellationToken);
            
        return entities.Select(e => new PaymentStatusEvent(
            Id: e.Id,
            OrderId: e.OrderId,
            ProviderResourceId: e.ProviderResourceId,
            Action: e.Action,
            Status: e.Status,
            ReceivedAtUtc: e.ReceivedAtUtc,
            FailureReason: e.FailureReason)).ToList();
    }
}