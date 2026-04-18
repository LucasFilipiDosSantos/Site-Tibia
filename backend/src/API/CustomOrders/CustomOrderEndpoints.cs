using API.Auth;
using Application.Checkout.Contracts;
using Application.Checkout.Services;
using System.Security.Claims;

namespace API.CustomOrders;

public static class CustomOrderEndpoints
{
    public static IEndpointRouteBuilder MapCustomOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var customerGroup = app.MapGroup("/custom-orders")
            .RequireAuthorization(AuthPolicies.VerifiedForSensitiveActions);

        // Customer: Submit custom request
        customerGroup.MapPost("/", async (ClaimsPrincipal user, CreateCustomRequestDto request, ICustomOrderService service, CancellationToken ct) =>
        {
            var customerId = ResolveCustomerId(user);
            var result = await service.CreateRequestAsync(customerId, new CreateCustomRequestInput(request.Description, request.OrderId));
            return Results.Created($"/custom-orders/{result.Id}", ToDto(result));
        });

        // Customer: List my requests
        customerGroup.MapGet("/", async (ClaimsPrincipal user, ICustomOrderService service, CancellationToken ct) =>
        {
            var customerId = ResolveCustomerId(user);
            var requests = await service.GetCustomerRequestsAsync(customerId);
            return Results.Ok(requests.Select(ToDto));
        });

        // Customer: Get specific request
        customerGroup.MapGet("/{requestId:guid}", async (ClaimsPrincipal user, Guid requestId, ICustomOrderService service, CancellationToken ct) =>
        {
            var customerId = ResolveCustomerId(user);
            var request = await service.GetByIdAsync(requestId, customerId);
            return request is null ? Results.NotFound() : Results.Ok(ToDto(request));
        });

        var adminGroup = app.MapGroup("/admin/custom-orders")
            .RequireAuthorization(AuthPolicies.AdminOnly);

        // Admin: Start progress
        adminGroup.MapPost("/{requestId:guid}/start", async (ClaimsPrincipal user, Guid requestId, ICustomOrderService service, CancellationToken ct) =>
        {
            var adminId = ResolveAdminId(user);
            var result = await service.StartProgressAsync(requestId, adminId);
            return Results.Ok(ToDto(result));
        });

        // Admin: Mark delivered
        adminGroup.MapPost("/{requestId:guid}/deliver", async (ClaimsPrincipal user, Guid requestId, ICustomOrderService service, CancellationToken ct) =>
        {
            var adminId = ResolveAdminId(user);
            var result = await service.MarkDeliveredAsync(requestId, adminId);
            return Results.Ok(ToDto(result));
        });

        return app;
    }

    private static Guid ResolveCustomerId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing sub claim");
        return Guid.Parse(sub);
    }

    private static Guid ResolveAdminId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing sub claim");
        return Guid.Parse(sub);
    }

    private static CustomRequestDto ToDto(CustomRequestResponse r) => new(
        r.Id,
        r.OrderId,
        r.CustomerId,
        r.Description,
        r.Status,
        r.CreatedAtUtc,
        r.UpdatedAtUtc
    );
}

public record CreateCustomRequestDto(string Description, Guid? OrderId);

public record CustomRequestDto(
    Guid Id,
    Guid? OrderId,
    Guid CustomerId,
    string Description,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);