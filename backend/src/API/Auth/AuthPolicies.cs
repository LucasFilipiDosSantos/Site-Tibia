using Microsoft.AspNetCore.Authorization;

namespace API.Auth;

public static class AuthPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string VerifiedForSensitiveActions = "VerifiedForSensitiveActions";

    public static IServiceCollection AddAuthPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminOnly, policy => policy.RequireClaim("role", "Admin"));
            options.AddPolicy(
                VerifiedForSensitiveActions,
                policy => policy.RequireClaim("email_verified", "true")
            );
        });

        return services;
    }
}
