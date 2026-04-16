using Domain.Identity;
using Infrastructure.Identity.Services;

namespace UnitTests.Identity;

public sealed class SecurityTokenExpiryTests
{
    [Fact]
    public void IssueRefreshToken_ExpiresInThirtyDays()
    {
        var service = new JwtTokenService(new JwtTokenServiceOptions
        {
            Issuer = "tests",
            Audience = "tests",
            SigningKey = "01234567890123456789012345678901"
        });

        var now = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var result = service.IssueRefreshToken(new Application.Identity.Contracts.RefreshTokenIssueRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            now,
            "127.0.0.1"));

        Assert.Equal(now.AddDays(SecurityPolicy.RefreshTokenLifetimeDays), result.ExpiresAtUtc);
    }
}
