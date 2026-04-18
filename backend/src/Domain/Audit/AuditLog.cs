namespace Domain.Audit;

/// <summary>
/// Immutable audit log entry for critical administrative actions
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; internal init; }
    public Guid ActorId { get; internal init; }
    public string ActorEmail { get; internal init; } = string.Empty;
    public string Action { get; internal init; } = string.Empty;
    public string EntityType { get; internal init; } = string.Empty;
    public Guid EntityId { get; internal init; }
    public string? BeforeValue { get; internal init; }
    public string? AfterValue { get; internal init; }
    public DateTime CreatedAtUtc { get; internal init; }
    public string IpAddress { get; internal init; } = string.Empty;

    internal AuditLog() { }

    public static AuditLog Create(
        Guid actorId,
        string actorEmail,
        string action,
        string entityType,
        Guid entityId,
        string? beforeValue,
        string? afterValue,
        string ipAddress)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            ActorEmail = actorEmail,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            BeforeValue = beforeValue,
            AfterValue = afterValue,
            CreatedAtUtc = DateTime.UtcNow,
            IpAddress = ipAddress
        };
    }
}