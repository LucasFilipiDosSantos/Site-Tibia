using API.Auth;
using Application.Inventory.Contracts;
using Application.Inventory.Services;
using System.Security.Claims;

namespace API.Inventory;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/inventory/{productId:guid}/availability", async (
            Guid productId,
            InventoryService inventoryService,
            CancellationToken ct) =>
        {
            var availability = await inventoryService.GetAvailabilityAsync(new GetInventoryAvailabilityRequest(productId), ct);
            return Results.Ok(new InventoryAvailabilityResponse(availability.Available, availability.Reserved, availability.Total));
        })
        .WithTags("Inventory");

        app.MapPost("/inventory/reservations", async (
            ReserveInventoryRequest request,
            InventoryService inventoryService,
            CancellationToken ct) =>
        {
            var result = await inventoryService.ReserveStockForCheckoutAsync(
                new ReserveStockForCheckoutRequest(request.OrderIntentKey, request.OrderId, request.ProductId, request.Quantity),
                ct);

            return Results.Ok(new ReserveInventoryResponse(
                result.OrderIntentKey,
                result.OrderId,
                result.ProductId,
                result.Quantity,
                result.ReservationExpiresAtUtc));
        })
        .WithTags("Inventory");

        app.MapPost("/inventory/reservations/release", async (
            ReleaseInventoryReservationRequest request,
            InventoryService inventoryService,
            CancellationToken ct) =>
        {
            var result = await inventoryService.ReleaseReservationAsync(
                new ReleaseReservationRequest(request.OrderIntentKey, ToContractReason(request.Reason)),
                ct);

            return Results.Ok(new ReleaseInventoryReservationResponse(
                result.OrderIntentKey,
                result.ProductId,
                result.ReleasedQuantity));
        })
        .WithTags("Inventory");

        var admin = app.MapGroup("/admin/inventory")
            .RequireAuthorization(AuthPolicies.AdminOnly);

        admin.MapPost("/adjustments", async (
            ClaimsPrincipal user,
            AdminAdjustInventoryRequest request,
            InventoryService inventoryService,
            CancellationToken ct) =>
        {
            var adminUserId = ResolveAdminUserId(user);
            var result = await inventoryService.AdjustStockAsync(
                new AdjustStockRequest(request.ProductId, request.Delta, request.Reason, adminUserId),
                ct);

            return Results.Ok(new AdminAdjustInventoryResponse(
                result.ProductId,
                result.Delta,
                result.BeforeQuantity,
                result.AfterQuantity,
                result.Reason,
                result.AdminUserId,
                result.AdjustedAtUtc));
        })
        .WithTags("Admin Inventory");

        return app;
    }

    private static Guid ResolveAdminUserId(ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var adminUserId) || adminUserId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated subject claim is missing or invalid.", "sub");
        }

        return adminUserId;
    }

    private static Application.Inventory.Contracts.ReservationReleaseReason ToContractReason(ReservationReleaseReason reason)
    {
        return reason switch
        {
            ReservationReleaseReason.PaymentFailed => Application.Inventory.Contracts.ReservationReleaseReason.PaymentFailed,
            ReservationReleaseReason.OrderCanceled => Application.Inventory.Contracts.ReservationReleaseReason.OrderCanceled,
            ReservationReleaseReason.Expired => Application.Inventory.Contracts.ReservationReleaseReason.Expired,
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unsupported release reason.")
        };
    }
}
