using Microsoft.AspNetCore.Authorization;
using Domain.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Auth;

public static class AuthPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string VerifiedForSensitiveActions = "VerifiedForSensitiveActions";

    public static IServiceCollection AddAuthPolicies(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, AdminAuthorizationHandler>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AdminOnly,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new AdminRequirement()));
            options.AddPolicy(
                VerifiedForSensitiveActions,
                policy => policy.RequireAuthenticatedUser().RequireClaim("email_verified", "true")
            );
        });

        return services;
    }

    public static bool HasAdminRoleClaim(ClaimsPrincipal user)
    {
        return user.Claims.Any(claim =>
            (claim.Type == "role" || claim.Type == ClaimTypes.Role)
            && string.Equals(
                claim.Value,
                UserRoleExtensions.AdminRoleName,
                StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class AdminRequirement : IAuthorizationRequirement;

public sealed class AdminAuthorizationHandler : AuthorizationHandler<AdminRequirement>
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AdminAuthorizationHandler> _logger;

    public AdminAuthorizationHandler(
        AppDbContext dbContext,
        ILogger<AdminAuthorizationHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminRequirement requirement)
    {
        var subject = context.User.FindFirstValue("sub") ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(subject, out var userId) && userId != Guid.Empty)
        {
            var isAdmin = await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(user => user.Id == userId && user.Role == UserRole.Admin);

            if (isAdmin)
            {
                context.Succeed(requirement);
                return;
            }

            _logger.LogWarning("Admin authorization denied for user {UserId} because database role is not Admin.", userId);
            return;
        }

        if (AuthPolicies.HasAdminRoleClaim(context.User))
        {
            context.Succeed(requirement);
            return;
        }

        _logger.LogWarning("Admin authorization denied because subject claim is missing or invalid.");
    }
}
