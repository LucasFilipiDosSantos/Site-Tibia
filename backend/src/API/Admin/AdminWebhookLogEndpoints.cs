using API.Auth;
using Application.Payments.Contracts;

namespace API.Admin;

/// <summary>
/// Admin endpoints for viewing webhook processing logs (Phase 6)
/// </summary>
public static class AdminWebhookLogEndpoints
{
    public static IEndpointRouteBuilder MapAdminWebhookLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/webhooks")
            .WithTags("Admin - Webhooks")
            .RequireAuthorization(AuthPolicies.AdminOnly);

        group.MapGet("/logs", GetWebhookLogs);
        group.MapGet("/logs/{id:guid}", GetWebhookLogById);

        return app;
    }

    private static async Task<IResult> GetWebhookLogs(
        [AsParameters] WebhookLogQueryParams query,
        IPaymentWebhookLogRepository repo,
        CancellationToken ct)
    {
        // Get all logs and filter in memory (Phase 6 implementation)
        var allLogs = await repo.GetByProviderResourceIdAsync("*", ct);
        
        IEnumerable<PaymentWebhookLogEntry> filtered = allLogs;

        if (!string.IsNullOrEmpty(query.Status))
        {
            filtered = filtered.Where(l => l.ValidationOutcome.ToString() == query.Status);
        }

        if (!string.IsNullOrEmpty(query.PaymentId))
        {
            filtered = filtered.Where(l => l.ProviderResourceId == query.PaymentId);
        }

        if (query.From.HasValue)
        {
            filtered = filtered.Where(l => l.ReceivedAtUtc >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            filtered = filtered.Where(l => l.ReceivedAtUtc <= query.To.Value);
        }

        var paged = filtered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return Results.Ok(new
        {
            items = paged,
            page = query.Page,
            pageSize = query.PageSize,
            totalCount = paged.Count
        });
    }

    private static async Task<IResult> GetWebhookLogById(
        Guid id,
        IPaymentWebhookLogRepository repo,
        CancellationToken ct)
    {
        var log = await repo.GetByIdAsync(id, ct);
        if (log == null)
            return Results.NotFound();

        return Results.Ok(log);
    }
}

public sealed record WebhookLogQueryParams(
    DateTime? From = null,
    DateTime? To = null,
    string? Status = null,
    string? PaymentId = null,
    int Page = 1,
    int PageSize = 20);