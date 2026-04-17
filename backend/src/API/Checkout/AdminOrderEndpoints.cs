using API.Auth;
using Application.Checkout.Contracts;
using Application.Checkout.Services;
using Domain.Checkout;
using System.Security.Claims;

namespace API.Checkout;

public static class AdminOrderEndpoints
{
    public static IEndpointRouteBuilder MapAdminOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/orders")
            .RequireAuthorization(AuthPolicies.AdminOnly);

        // Per D-13: Admin search endpoint
        group.MapGet("", async (
            OrderStatus? status,
            Guid? customerId,
            DateTimeOffset? createdFromUtc,
            DateTimeOffset? createdToUtc,
            int page,
            int pageSize,
            ICheckoutRepository repository,
            CancellationToken ct) =>
        {
            // TODO: Implement full filtering - D-13
            var orders = await repository.SearchOrdersAsync(status, customerId, createdFromUtc, createdToUtc, page, pageSize, ct);
            
            var items = orders.Select(o => new OrderListItemDto(
                o.Id,
                o.OrderIntentKey,
                o.CreatedAtUtc,
                o.Status.ToString(),
                GetStatusLabel(o.Status))).ToList();
                
            return Results.Ok(new PaginatedOrderListDto(items, page, pageSize, items.Count));
        });

        // Per D-14, D-16: Admin explicit cancel action
        group.MapPost("/{orderId:guid}/actions/cancel", async (
            ClaimsPrincipal user,
            Guid orderId,
            AdminCancelOrderDto request,
            OrderLifecycleService lifecycleService,
            CancellationToken ct) =>
        {
            var actorUserId = ResolveAdminUserId(user);
            var reason = request.Reason;

            try
            {
                await lifecycleService.ApplyAdminCancelAsync(orderId, actorUserId, reason, ct);
                return Results.Ok();
            }
            catch (ForbiddenStatusTransitionException ex)
            {
                // Per D-15: 409 Conflict with currentStatus and allowedTransitions
                return Results.Conflict(new
                {
                    type = "https://tools.ietf.org/html/rfc7807",
                    title = "Status Transition Conflict",
                    status = 409,
                    currentStatus = ex.CurrentStatus.ToString(),
                    allowedTransitions = ex.AllowedTransitions.Select(t => t.ToString()).ToList()
                });
            }
        });

        return app;
    }

    private static Guid ResolveAdminUserId(ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userId) || userId == Guid.Empty)
        {
            throw new ArgumentException("Admin subject claim is missing or invalid.", "sub");
        }
        return userId;
    }

    private static string GetStatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Pendente",
        OrderStatus.Paid => "Pago",
        OrderStatus.Cancelled => "Cancelado",
        _ => status.ToString()
    };
}