using Application.Audit.Contracts;

namespace Application.Audit.Contracts;

/// <summary>
/// Repository interface for audit log persistence
/// </summary>
public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
    Task<AuditLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogEntry>> QueryAsync(
        DateTime? from,
        DateTime? to,
        string? action,
        string? entityType,
        Guid? actorId,
        Guid? entityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<long> CountAsync(
        DateTime? from,
        DateTime? to,
        string? action,
        string? entityType,
        Guid? actorId,
        Guid? entityId,
        CancellationToken cancellationToken = default);
}