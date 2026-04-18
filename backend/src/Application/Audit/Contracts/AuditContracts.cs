namespace Application.Audit.Contracts;

/// <summary>
/// Read-only audit log entry for query results
/// </summary>
public sealed record AuditLogEntry(
    Guid Id,
    Guid ActorId,
    string ActorEmail,
    string Action,
    string EntityType,
    Guid EntityId,
    string? BeforeValue,
    string? AfterValue,
    DateTime CreatedAtUtc,
    string IpAddress
);

/// <summary>
/// Query parameters for audit log filtering
/// </summary>
public sealed record AuditLogQuery(
    DateTime? From,
    DateTime? To,
    string? Action,
    string? EntityType,
    Guid? ActorId,
    Guid? EntityId,
    int Page = 1,
    int PageSize = 20
);

/// <summary>
/// Paginated audit log result
/// </summary>
public sealed record PaginatedAuditLog(
    IReadOnlyList<AuditLogEntry> Items,
    int Page,
    int PageSize,
    long TotalCount
);

/// <summary>
/// Service interface for audit logging
/// </summary>
public interface IAuditLogService
{
    Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
    Task<PaginatedAuditLog> QueryAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
    Task<AuditLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}