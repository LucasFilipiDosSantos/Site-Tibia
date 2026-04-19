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
        if (!TryParseOutcome(query.Status, out var outcome))
        {
            return Results.BadRequest(new
            {
                error = $"Invalid status '{query.Status}'. Expected one of: {string.Join(", ", Enum.GetNames<PaymentWebhookValidationOutcome>())}"
            });
        }

        var paged = await repo.QueryAsync(
            query.From,
            query.To,
            outcome,
            query.PaymentId,
            query.Page,
            query.PageSize,
            ct);

        var totalCount = await repo.CountAsync(
            query.From,
            query.To,
            outcome,
            query.PaymentId,
            ct);

        return Results.Ok(new
        {
            items = paged,
            page = query.Page,
            pageSize = query.PageSize,
            totalCount
        });
    }

    private static bool TryParseOutcome(string? status, out PaymentWebhookValidationOutcome? outcome)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            outcome = null;
            return true;
        }

        if (Enum.TryParse<PaymentWebhookValidationOutcome>(status, ignoreCase: true, out var parsed))
        {
            outcome = parsed;
            return true;
        }

        outcome = null;
        return false;
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
