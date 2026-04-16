using Application.Identity.Contracts;
using Application.Identity.Services;
using Domain.Identity;

namespace UnitTests.Identity;

public sealed class LockoutPolicyTests
{
    [Fact]
    public async Task LoginAsync_Applies15MinuteLockoutAfterThreshold()
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var clock = new FixedClock(now);
        var userRepo = new InMemoryUserRepository();
        var user = new UserAccount("lock@test.com", "HASH:ValidPass123!");
        await userRepo.AddAsync(user);
        await userRepo.SaveChangesAsync();

        var service = new IdentityService(
            userRepo,
            new InMemoryRefreshSessionRepository(),
            new FakeTokenService(),
            new FakePasswordHasherService(),
            clock);

        for (var i = 0; i < SecurityPolicy.FailedLoginThreshold; i++)
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.LoginAsync(new LoginCommand("lock@test.com", "wrong-pass", "127.0.0.1")));
        }

        Assert.True(user.LockoutEndsAtUtc.HasValue);
        Assert.Equal(now.AddMinutes(SecurityPolicy.LockoutDurationMinutes), user.LockoutEndsAtUtc.Value);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LoginAsync(new LoginCommand("lock@test.com", "ValidPass123!", "127.0.0.1")));
    }
}
