using API.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace UnitTests.Identity;

public sealed class AuthorizationPolicyTests
{
    [Fact]
    public void AdminOnlyPolicy_AllowsRoleClaimWithAdminValue()
    {
        var principal = TestPrincipals.WithClaims(("role", "Admin"));

        Assert.True(AuthPolicies.HasAdminRoleClaim(principal));
    }

    [Fact]
    public void AdminOnlyPolicy_AllowsDotNetRoleClaimWithAdminValue()
    {
        var principal = TestPrincipals.WithClaims((ClaimTypes.Role, "Admin"));

        Assert.True(AuthPolicies.HasAdminRoleClaim(principal));
    }

    [Fact]
    public void AdminOnlyPolicy_AllowsLowercaseAdminValue()
    {
        var principal = TestPrincipals.WithClaims(("role", "admin"));

        Assert.True(AuthPolicies.HasAdminRoleClaim(principal));
    }

    [Fact]
    public void AdminOnlyPolicy_RejectsCustomerRoleClaim()
    {
        var principal = TestPrincipals.WithClaims(("role", "Customer"));

        Assert.False(AuthPolicies.HasAdminRoleClaim(principal));
    }

    [Fact]
    public async Task VerifiedPolicy_RequiresEmailVerifiedTrue()
    {
        var auth = BuildAuthorizationService();
        var principal = TestPrincipals.WithClaims(("role", "Admin"), ("email_verified", "false"));
        var result = await auth.AuthorizeAsync(principal, null, AuthPolicies.VerifiedForSensitiveActions);

        Assert.False(result.Succeeded);
    }

    private static IAuthorizationService BuildAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthPolicies.VerifiedForSensitiveActions,
                policy => policy.RequireAuthenticatedUser().RequireClaim("email_verified", "true"));
        });
        var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IAuthorizationService>();
    }
}
