using Application.Identity.Contracts;
using Application.Identity.Services;
using Domain.Identity;

namespace UnitTests.Identity;

public sealed class TokenRotationTests
{
    [Fact]
    public async Task RotateAsync_RevokesOldTokenAndBlocksReuse()
    {
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var clock = new FixedClock(now);
        var userRepo = new InMemoryUserRepository();
        var sessionRepo = new InMemoryRefreshSessionRepository();
        var tokenService = new FakeTokenService();

        var user = new UserAccount("Test User", "rotate@test.com", "HASH:ValidPass123!");
        await userRepo.AddAsync(user);
        await userRepo.SaveChangesAsync();

        var originalRefresh = tokenService.IssueRefreshToken(new RefreshTokenIssueRequest(user.Id, Guid.NewGuid(), now, "127.0.0.1"));
        var originalSession = new RefreshSession(
            user.Id,
            Guid.NewGuid(),
            originalRefresh.TokenHash,
            now,
            now.AddDays(SecurityPolicy.RefreshTokenLifetimeDays),
            "127.0.0.1");
        await sessionRepo.AddAsync(originalSession);
        await sessionRepo.SaveChangesAsync();

        var rotation = new TokenRotationService(sessionRepo, userRepo, tokenService, clock);

        var rotated = await rotation.RotateAsync(originalRefresh.RawToken, "127.0.0.2");
        Assert.NotEqual(originalRefresh.RawToken, rotated.RefreshToken);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            rotation.RotateAsync(originalRefresh.RawToken, "127.0.0.3"));
    }
}

