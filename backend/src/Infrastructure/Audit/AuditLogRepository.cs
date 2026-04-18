using Application.Audit.Contracts;
using Domain.Audit;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Audit;

/// <summary>
/// Infrastructure repository for audit log persistence
/// </summary>
public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        var entity = AuditLog.Create(
            entry.ActorId,
            entry.ActorEmail,
            entry.Action,
            entry.EntityType,
            entry.EntityId,
            entry.BeforeValue,
            entry.AfterValue,
            entry.IpAddress);

        _context.AuditLogs.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuditLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.AuditLogs.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return null;

        return MapToEntry(entity);
    }

    public async Task<IReadOnlyList<AuditLogEntry>> QueryAsync(
        DateTime? from,
        DateTime? to,
        string? action,
        string? entityType,
        Guid? actorId,
        Guid? entityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var queryable = _context.AuditLogs.AsQueryable();

        if (from.HasValue)
        {
            queryable = queryable.Where(e => e.CreatedAtUtc >= from.Value);
        }

        if (to.HasValue)
        {
            queryable = queryable.Where(e => e.CreatedAtUtc <= to.Value);
        }

        if (!string.IsNullOrEmpty(action))
        {
            queryable = queryable.Where(e => e.Action == action);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            queryable = queryable.Where(e => e.EntityType == entityType);
        }

        if (actorId.HasValue)
        {
            queryable = queryable.Where(e => e.ActorId == actorId.Value);
        }

        if (entityId.HasValue)
        {
            queryable = queryable.Where(e => e.EntityId == entityId.Value);
        }

        var items = await queryable
            .OrderByDescending(e => e.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => MapToEntry(e))
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<long> CountAsync(
        DateTime? from,
        DateTime? to,
        string? action,
        string? entityType,
        Guid? actorId,
        Guid? entityId,
        CancellationToken cancellationToken = default)
    {
        var queryable = _context.AuditLogs.AsQueryable();

        if (from.HasValue)
        {
            queryable = queryable.Where(e => e.CreatedAtUtc >= from.Value);
        }

        if (to.HasValue)
        {
            queryable = queryable.Where(e => e.CreatedAtUtc <= to.Value);
        }

        if (!string.IsNullOrEmpty(action))
        {
            queryable = queryable.Where(e => e.Action == action);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            queryable = queryable.Where(e => e.EntityType == entityType);
        }

        if (actorId.HasValue)
        {
            queryable = queryable.Where(e => e.ActorId == actorId.Value);
        }

        if (entityId.HasValue)
        {
            queryable = queryable.Where(e => e.EntityId == entityId.Value);
        }

        return await queryable.LongCountAsync(cancellationToken);
    }

    private static AuditLogEntry MapToEntry(AuditLog entity)
    {
        return new AuditLogEntry(
            entity.Id,
            entity.ActorId,
            entity.ActorEmail,
            entity.Action,
            entity.EntityType,
            entity.EntityId,
            entity.BeforeValue,
            entity.AfterValue,
            entity.CreatedAtUtc,
            entity.IpAddress);
    }
}