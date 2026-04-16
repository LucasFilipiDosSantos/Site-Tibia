using Domain.Identity;
using Infrastructure.Identity.Services;
using System.IdentityModel.Tokens.Jwt;

namespace UnitTests.Identity;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void IssueAccessToken_IncludesRoleAndEmailVerifiedClaims()
    {
        var service = CreateService();
        var now = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

        var result = service.IssueAccessToken(new Application.Identity.Contracts.AccessTokenRequest(
            Guid.NewGuid(),
            "claims@test.com",
            UserRole.Admin,
            true,
            now));

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Token);

        Assert.Contains(token.Claims, c => c.Type == "role" && c.Value == "Admin");
        Assert.Contains(token.Claims, c => c.Type == "email_verified" && c.Value == "true");
    }

    [Fact]
    public void IssueAccessToken_ExpiresInFifteenMinutes()
    {
        var service = CreateService();
        var now = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

        var result = service.IssueAccessToken(new Application.Identity.Contracts.AccessTokenRequest(
            Guid.NewGuid(),
            "claims@test.com",
            UserRole.Costumer,
            false,
            now));

        Assert.Equal(now.AddMinutes(SecurityPolicy.AccessTokenLifetimeMinutes), result.ExpiresAtUtc);
    }

    private static JwtTokenService CreateService()
    {
        return new JwtTokenService(new JwtTokenServiceOptions
        {
            Issuer = "tests",
            Audience = "tests",
            SigningKey = "01234567890123456789012345678901"
        });
    }
}
