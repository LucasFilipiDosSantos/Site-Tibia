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
        var queryable = _context.PaymentWebhookLogs
            .Where(e => e.ProviderResourceId == providerResourceId)
            .OrderByDescending(e => e.ReceivedAtUtc);

        return await queryable
            .Select(e => MapToEntry(e))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentWebhookLogEntry>> QueryAsync(
        DateTime? from,
        DateTime? to,
        PaymentWebhookValidationOutcome? validationOutcome,
        string? providerResourceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var queryable = ApplyFilters(
            _context.PaymentWebhookLogs.AsQueryable(),
            from,
            to,
            validationOutcome,
            providerResourceId);

        var entities = await queryable
            .OrderByDescending(e => e.ReceivedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => MapToEntry(e))
            .ToListAsync(cancellationToken);

        return entities;
    }

    public async Task<long> CountAsync(
        DateTime? from,
        DateTime? to,
        PaymentWebhookValidationOutcome? validationOutcome,
        string? providerResourceId,
        CancellationToken cancellationToken = default)
    {
        var queryable = ApplyFilters(
            _context.PaymentWebhookLogs.AsQueryable(),
            from,
            to,
            validationOutcome,
            providerResourceId);

        return await queryable.LongCountAsync(cancellationToken);
    }

    private static IQueryable<PaymentWebhookLogEntity> ApplyFilters(
        IQueryable<PaymentWebhookLogEntity> queryable,
        DateTime? from,
        DateTime? to,
        PaymentWebhookValidationOutcome? validationOutcome,
        string? providerResourceId)
    {
        if (from.HasValue)
        {
            var fromUtc = new DateTimeOffset(DateTime.SpecifyKind(from.Value, DateTimeKind.Utc));
            queryable = queryable.Where(e => e.ReceivedAtUtc >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = new DateTimeOffset(DateTime.SpecifyKind(to.Value, DateTimeKind.Utc));
            queryable = queryable.Where(e => e.ReceivedAtUtc <= toUtc);
        }

        if (validationOutcome.HasValue)
        {
            var outcome = (int)validationOutcome.Value;
            queryable = queryable.Where(e => e.ValidationOutcome == outcome);
        }

        if (!string.IsNullOrWhiteSpace(providerResourceId))
        {
            queryable = queryable.Where(e => e.ProviderResourceId == providerResourceId);
        }

        return queryable;
    }

    private static PaymentWebhookLogEntry MapToEntry(PaymentWebhookLogEntity entity)
    {
        return new PaymentWebhookLogEntry(
            Id: entity.Id,
            RequestId: entity.RequestId,
            Topic: entity.Topic,
            Action: entity.Action,
            ProviderResourceId: entity.ProviderResourceId,
            ReceivedAtUtc: entity.ReceivedAtUtc,
            ValidationOutcome: (PaymentWebhookValidationOutcome)entity.ValidationOutcome);
    }
}
