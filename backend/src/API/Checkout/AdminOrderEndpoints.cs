using API.Auth;
using Application.Checkout.Contracts;
using Application.Checkout.Services;
using Application.Identity.Contracts;
using Domain.Checkout;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace API.Checkout;

public sealed record ForceCompleteDeliveryDto(
    Guid OrderId,
    Guid ProductId,
    string AdminNote
);

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
                GetStatusLabel(o.Status),
                o.CustomerName,
                o.CustomerEmail,
                o.CustomerDiscord,
                o.PaymentMethod,
                o.Items.Sum(item => item.UnitPrice * item.Quantity),
                o.Items.Sum(item => item.Quantity))).ToList();
                
            return Results.Ok(new PaginatedOrderListDto(items, page, pageSize, items.Count));
        });

        // Per D-10, D-11: Admin force-complete delivery
        group.MapPost("/deliveries/complete", async (
            ForceCompleteDeliveryDto request,
            IAdminFulfillmentService service,
            CancellationToken ct) =>
        {
            await service.ForceCompleteAsync(request.OrderId, request.ProductId, request.AdminNote, ct);
            return Results.Ok();
        })
        .WithName("ForceCompleteDelivery")
        .WithTags("Admin");

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

        group.MapPut("/{orderId:guid}", async (
            Guid orderId,
            AdminUpdateOrderDto request,
            AppDbContext dbContext,
            ICheckoutInventoryGateway inventoryGateway,
            IUserRepository userRepository,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("AdminOrderUpdate");
            var order = await dbContext.Orders
                .Include(x => x.Items)
                .SingleOrDefaultAsync(x => x.Id == orderId, ct);

            if (order is null)
            {
                return Results.NotFound();
            }

            if (!TryParseOrderStatus(request.Status, out var status))
            {
                return Results.BadRequest(new { message = "Status invalido." });
            }

            var wasCancelled = order.Status == OrderStatus.Cancelled;
            order.SetAdminEditableData(
                request.CustomerName,
                request.CustomerEmail,
                request.CustomerDiscord,
                request.PaymentMethod,
                status);

            var matchedUser = await userRepository.GetByEmailAsync(request.CustomerEmail, ct);
            if (matchedUser is not null && order.CustomerId != matchedUser.Id)
            {
                logger.LogInformation(
                    "Admin update relinked order {OrderId} intent {OrderIntentKey} from customer {PreviousCustomerId} to {CustomerId} using email {CustomerEmail}.",
                    order.Id,
                    order.OrderIntentKey,
                    order.CustomerId,
                    matchedUser.Id,
                    request.CustomerEmail);
                order.RelinkCustomer(matchedUser.Id);
            }
            else if (matchedUser is null)
            {
                logger.LogWarning(
                    "Admin update could not relink order {OrderId} intent {OrderIntentKey} because no user was found for email {CustomerEmail}. CustomerId remains {CustomerId}.",
                    order.Id,
                    order.OrderIntentKey,
                    request.CustomerEmail,
                    order.CustomerId);
            }

            await dbContext.SaveChangesAsync(ct);

            if (status == OrderStatus.Cancelled && !wasCancelled)
            {
                await inventoryGateway.ReleaseCheckoutReservationAsync(
                    order.OrderIntentKey,
                    Application.Inventory.Contracts.ReservationReleaseReason.OrderCanceled,
                    ct);
            }

            return Results.Ok(new OrderListItemDto(
                order.Id,
                order.OrderIntentKey,
                order.CreatedAtUtc,
                order.Status.ToString(),
                GetStatusLabel(order.Status),
                order.CustomerName,
                order.CustomerEmail,
                order.CustomerDiscord,
                order.PaymentMethod,
                order.Items.Sum(item => item.UnitPrice * item.Quantity),
                order.Items.Sum(item => item.Quantity)));
        });

        group.MapDelete("/{orderId:guid}", async (
            Guid orderId,
            AppDbContext dbContext,
            ICheckoutInventoryGateway inventoryGateway,
            CancellationToken ct) =>
        {
            var order = await dbContext.Orders
                .SingleOrDefaultAsync(x => x.Id == orderId, ct);

            if (order is null)
            {
                return Results.NotFound();
            }

            if (!order.IsHidden)
            {
                order.Hide();
                await dbContext.SaveChangesAsync(ct);

                await inventoryGateway.ReleaseCheckoutReservationAsync(
                    order.OrderIntentKey,
                    Application.Inventory.Contracts.ReservationReleaseReason.OrderCanceled,
                    ct);
            }

            return Results.NoContent();
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

    private static bool TryParseOrderStatus(string rawStatus, out OrderStatus status)
    {
        var normalized = rawStatus.Trim().ToLowerInvariant();

        return normalized switch
        {
            "pending" or "pendente" => Match(OrderStatus.Pending, out status),
            "paid" or "pago" => Match(OrderStatus.Paid, out status),
            "cancelled" or "canceled" or "cancelado" => Match(OrderStatus.Cancelled, out status),
            _ => Enum.TryParse(rawStatus, ignoreCase: true, out status)
        };
    }

    private static bool Match(OrderStatus value, out OrderStatus status)
    {
        status = value;
        return true;
    }
}
