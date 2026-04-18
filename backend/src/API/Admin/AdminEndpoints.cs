using API.Auth;
using Application.Audit.Contracts;
using Application.Catalog.Services;
using System.Security.Claims;
using System.Text.Json;

namespace API.Admin;

/// <summary>
/// Centralized admin endpoints for CRUD operations with audit logging
/// </summary>
public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin")
            .RequireAuthorization(AuthPolicies.AdminOnly);

        // Products CRUD - existing via CatalogEndpoints
        // Product management is available via /admin/catalog/products
        
        // Inventory adjustments - existing via /admin/inventory/adjustments
        
        // Orders - existing via /admin/orders
        
        // Users - list and get individual user
        admin.MapGet("/users", GetUsers);
        admin.MapGet("/users/{id:guid}", GetUser);

        return app;
    }

    private static async Task<IResult> GetProducts(
        [AsParameters] ProductListQuery query,
        CatalogService catalogService,
        CancellationToken ct)
    {
        var request = new Application.Catalog.Contracts.ListProductsRequest(
            query.Page,
            query.PageSize,
            query.Category,
            null);

        var result = await catalogService.ListProducts(request, ct);
        var hasNextPage = result.Items.Count == result.PageSize;

        return Results.Ok(new
        {
            items = result.Items.Select(p => new ProductDto(p.Name, p.Slug, p.Description, p.Price, p.CategorySlug)).ToList(),
            page = result.Page,
            pageSize = result.PageSize,
            hasNextPage
        });
    }

    private static Task<IResult> GetUser(Guid id, CancellationToken ct)
    {
        // User details endpoint - returns stub for now, can be extended with IUserRepository
        return Task.FromResult<IResult>(Results.Ok(new { id, message = "User details - to be implemented" }));
    }

    private static Task<IResult> GetUsers([AsParameters] UserListQuery query, CancellationToken ct)
    {
        // Users list endpoint - returns stub for now, can be extended with IUserRepository
        return Task.FromResult<IResult>(Results.Ok(new { page = query.Page, pageSize = query.PageSize, items = Array.Empty<object>() }));
    }

    public static async Task LogWriteAction<T>(
        IAuditLogService audit,
        ClaimsPrincipal user,
        HttpContext context,
        string action,
        string entityType,
        Guid entityId,
        T? before,
        T? after,
        CancellationToken ct)
    {
        if (audit == null) return;

        var adminId = ResolveAdminUserId(user);
        var email = ResolveAdminEmail(user);
        var ip = ResolveIpAddress(context);

        var beforeJson = before != null ? JsonSerializer.Serialize(before) : null;
        var afterJson = after != null ? JsonSerializer.Serialize(after) : null;

        var entry = new AuditLogEntry(
            Guid.NewGuid(),
            adminId,
            email,
            action,
            entityType,
            entityId,
            beforeJson,
            afterJson,
            DateTime.UtcNow,
            ip);

        await audit.LogAsync(entry, ct);
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

    private static string ResolveAdminEmail(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email") ?? "unknown";
    }

    private static string ResolveIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

public sealed record ProductListQuery(int Page = 1, int PageSize = 20, string? Category = null);
public sealed record ProductDto(string Name, string Slug, string Description, decimal Price, string? CategorySlug);
public sealed record UserListQuery(int Page = 1, int PageSize = 20);