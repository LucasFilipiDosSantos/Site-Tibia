using API.Auth;
using Application.Audit.Contracts;
using Application.Catalog.Services;
using Application.Identity.Contracts;
using Domain.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
        admin.MapPut("/users/{id:guid}", UpdateUser);

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

    private static async Task<IResult> GetUser(Guid id, AppDbContext dbContext, CancellationToken ct)
    {
        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return user is null
            ? Results.NotFound()
            : Results.Ok(ToAdminUserDto(user));
    }

    private static async Task<IResult> UpdateUser(
        Guid id,
        AdminUpdateUserDto request,
        AppDbContext dbContext,
        IPasswordHasherService passwordHasher,
        CancellationToken ct)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (user is null)
        {
            return Results.NotFound();
        }

        var normalizedEmail = UserAccount.NormalizeEmail(request.Email);
        var emailInUse = await dbContext.Users.AnyAsync(x => x.Id != id && x.Email == normalizedEmail, ct);
        if (emailInUse)
        {
            return Results.BadRequest(new { message = "E-mail ja esta em uso." });
        }

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        {
            role = request.Role.Equals("customer", StringComparison.OrdinalIgnoreCase)
                ? UserRole.Costumer
                : throw new ArgumentException("Invalid user role.", nameof(request.Role));
        }

        user.Rename(request.Name);
        user.ChangeEmail(request.Email);
        user.SetRole(role);
        user.SetEmailVerified(request.EmailVerified);

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            user.SetPasswordHash(passwordHasher.HashPassword(request.NewPassword));
            user.ResetFailedLogin(DateTimeOffset.UtcNow);
        }

        await dbContext.SaveChangesAsync(ct);
        return Results.Ok(ToAdminUserDto(user));
    }

    private static async Task<IResult> GetUsers([AsParameters] UserListQuery query, AppDbContext dbContext, CancellationToken ct)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var users = await dbContext.Users
            .AsNoTracking()
            .OrderByDescending(user => user.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = users.Select(ToAdminUserDto).ToList();
        return Results.Ok(new AdminUserListDto(items, page, pageSize, items.Count));
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

    private static AdminUserDto ToAdminUserDto(UserAccount user)
    {
        return new AdminUserDto(
            user.Id,
            user.Name,
            user.Email,
            user.Role == UserRole.Admin ? "admin" : "customer",
            user.EmailVerified,
            user.CreatedAtUtc);
    }
}

public sealed record ProductListQuery(int Page = 1, int PageSize = 20, string? Category = null);
public sealed record ProductDto(string Name, string Slug, string Description, decimal Price, string? CategorySlug);
public sealed record UserListQuery(int Page = 1, int PageSize = 20);
public sealed record AdminUserDto(Guid Id, string Name, string Email, string Role, bool EmailVerified, DateTimeOffset CreatedAtUtc);
public sealed record AdminUserListDto(IReadOnlyList<AdminUserDto> Items, int Page, int PageSize, int TotalCount);
public sealed record AdminUpdateUserDto(string Name, string Email, string Role, bool EmailVerified, string? NewPassword);
