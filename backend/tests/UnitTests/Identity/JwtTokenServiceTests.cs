using Domain.Identity;
using Infrastructure.Identity.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

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
            "Claims User",
            "claims@test.com",
            UserRole.Admin,
            true,
            now));

        var handler = new JwtSecurityTokenHandler();
        handler.MapInboundClaims = false;
        Assert.Equal(5, result.Token.Split('.').Length);

        var principal = handler.ValidateToken(result.Token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "tests",
            ValidateAudience = true,
            ValidAudience = "tests",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey)),
            TokenDecryptionKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestEncryptionKey)),
            ValidateLifetime = false,
            ClockSkew = TimeSpan.Zero
        }, out _);

        Assert.Contains(principal.Claims, c => c.Type == "role" && c.Value == "Admin");
        Assert.Contains(principal.Claims, c => c.Type == "name" && c.Value == "Claims User");
        Assert.Contains(principal.Claims, c => c.Type == "email_verified" && c.Value == "true");
    }

    [Fact]
    public void IssueAccessToken_ExpiresInFifteenMinutes()
    {
        var service = CreateService();
        var now = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

        var result = service.IssueAccessToken(new Application.Identity.Contracts.AccessTokenRequest(
            Guid.NewGuid(),
            "Claims User",
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
            SigningKey = TestSigningKey,
            EncryptionKey = TestEncryptionKey
        });
    }

    private const string TestSigningKey = "01234567890123456789012345678901";
    private const string TestEncryptionKey = "12345678901234567890123456789012";
}
