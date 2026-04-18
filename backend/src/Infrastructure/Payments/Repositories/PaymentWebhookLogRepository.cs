using Application.Payments.Contracts;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Payments.Repositories;

/// <summary>
/// Repository for webhook log persistence
/// </summary>
public sealed class PaymentWebhookLogRepository : IPaymentWebhookLogRepository
{
    private readonly AppDbContext _context;

    public PaymentWebhookLogRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task LogAsync(PaymentWebhookLogEntry entry, CancellationToken cancellationToken = default)
    {
        var entity = new PaymentWebhookLogEntity
        {
            Id = entry.Id,
            RequestId = entry.RequestId,
            Topic = entry.Topic,
            Action = entry.Action,
            ProviderResourceId = entry.ProviderResourceId,
            ReceivedAtUtc = entry.ReceivedAtUtc,
            ValidationOutcome = (int)entry.ValidationOutcome
        };
        
        _context.PaymentWebhookLogs.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaymentWebhookLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PaymentWebhookLogs.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return null;
        
        return new PaymentWebhookLogEntry(
            Id: entity.Id,
            RequestId: entity.RequestId,
            Topic: entity.Topic,
            Action: entity.Action,
            ProviderResourceId: entity.ProviderResourceId,
            ReceivedAtUtc: entity.ReceivedAtUtc,
            ValidationOutcome: (PaymentWebhookValidationOutcome)entity.ValidationOutcome);
    }

    public async Task<IReadOnlyList<PaymentWebhookLogEntry>> GetByProviderResourceIdAsync(
        string providerResourceId, 
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.PaymentWebhookLogs
            .Where(e => e.ProviderResourceId == providerResourceId)
            .OrderByDescending(e => e.ReceivedAtUtc)
            .ToListAsync(cancellationToken);
            
        return entities.Select(e => new PaymentWebhookLogEntry(
            Id: e.Id,
            RequestId: e.RequestId,
            Topic: e.Topic,
            Action: e.Action,
            ProviderResourceId: e.ProviderResourceId,
            ReceivedAtUtc: e.ReceivedAtUtc,
            ValidationOutcome: (PaymentWebhookValidationOutcome)e.ValidationOutcome)).ToList();
    }
}