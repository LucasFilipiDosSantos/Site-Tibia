using Application.Audit.Contracts;
using Application.Identity.Contracts;

namespace Application.Audit.Services;

/// <summary>
/// Service for logging and querying audit log entries
/// </summary>
public sealed class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;
    private readonly ISystemClock _clock;

    public AuditLogService(IAuditLogRepository repository, ISystemClock clock)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        var entryWithTimestamp = new AuditLogEntry(
            Guid.NewGuid(),
            entry.ActorId,
            entry.ActorEmail,
            entry.Action,
            entry.EntityType,
            entry.EntityId,
            entry.BeforeValue,
            entry.AfterValue,
            _clock.UtcNow.DateTime,
            entry.IpAddress);

        await _repository.AddAsync(entryWithTimestamp, cancellationToken);
    }

    public async Task<PaginatedAuditLog> QueryAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        var items = await _repository.QueryAsync(
            query.From,
            query.To,
            query.Action,
            query.EntityType,
            query.ActorId,
            query.EntityId,
            query.Page,
            query.PageSize,
            cancellationToken);

        var totalCount = await _repository.CountAsync(
            query.From,
            query.To,
            query.Action,
            query.EntityType,
            query.ActorId,
            query.EntityId,
            cancellationToken);

        return new PaginatedAuditLog(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<AuditLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }
}