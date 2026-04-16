using API.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests.Identity;

public sealed class AuthorizationPolicyTests
{
    [Fact]
    public async Task AdminOnlyPolicy_RequiresAdminRoleClaim()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthPolicies();
        var provider = services.BuildServiceProvider();

        var auth = provider.GetRequiredService<IAuthorizationService>();
        var principal = TestPrincipals.WithClaims(("role", "Customer"));
        var result = await auth.AuthorizeAsync(principal, null, AuthPolicies.AdminOnly);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task VerifiedPolicy_RequiresEmailVerifiedTrue()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthPolicies();
        var provider = services.BuildServiceProvider();

        var auth = provider.GetRequiredService<IAuthorizationService>();
        var principal = TestPrincipals.WithClaims(("role", "Admin"), ("email_verified", "false"));
        var result = await auth.AuthorizeAsync(principal, null, AuthPolicies.VerifiedForSensitiveActions);

        Assert.False(result.Succeeded);
    }
}
