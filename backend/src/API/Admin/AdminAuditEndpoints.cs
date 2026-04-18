using API.Auth;
using Application.Audit.Contracts;

namespace API.Admin;

/// <summary>
/// Admin endpoints for viewing audit logs
/// </summary>
public static class AdminAuditEndpoints
{
    public static IEndpointRouteBuilder MapAdminAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/audit")
            .WithTags("Admin - Audit")
            .RequireAuthorization(AuthPolicies.AdminOnly);

        group.MapGet("/logs", GetAuditLogs);
        group.MapGet("/logs/{id:guid}", GetAuditLogById);

        return app;
    }

    private static async Task<IResult> GetAuditLogs(
        [AsParameters] AuditLogQueryParams query,
        IAuditLogService auditService,
        CancellationToken ct)
    {
        var auditQuery = new AuditLogQuery(
            query.From,
            query.To,
            query.Action,
            query.EntityType,
            query.ActorId,
            query.EntityId,
            query.Page,
            query.PageSize);

        var result = await auditService.QueryAsync(auditQuery, ct);

        return Results.Ok(new
        {
            items = result.Items,
            page = result.Page,
            pageSize = result.PageSize,
            totalCount = result.TotalCount
        });
    }

    private static async Task<IResult> GetAuditLogById(
        Guid id,
        IAuditLogService auditService,
        CancellationToken ct)
    {
        var entry = await auditService.GetByIdAsync(id, ct);
        if (entry == null)
            return Results.NotFound();

        return Results.Ok(entry);
    }
}

public sealed record AuditLogQueryParams(
    DateTime? From = null,
    DateTime? To = null,
    string? Action = null,
    string? EntityType = null,
    Guid? ActorId = null,
    Guid? EntityId = null,
    int Page = 1,
    int PageSize = 20);