using Microsoft.AspNetCore.Authorization;
using Domain.Identity;

namespace API.Auth;

public static class AuthPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string VerifiedForSensitiveActions = "VerifiedForSensitiveActions";

    public static IServiceCollection AddAuthPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminOnly, policy => policy.RequireRole(UserRoleExtensions.AdminRoleName));
            options.AddPolicy(
                VerifiedForSensitiveActions,
                policy => policy.RequireAuthenticatedUser().RequireClaim("email_verified", "true")
            );
        });

        return services;
    }
}
