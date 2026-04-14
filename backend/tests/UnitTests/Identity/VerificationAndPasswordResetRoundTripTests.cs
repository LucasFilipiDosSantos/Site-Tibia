using Application.Identity.Contracts;
using Application.Identity.Services;
using Domain.Identity;

namespace UnitTests.Identity;

public sealed class VerificationAndPasswordResetRoundTripTests
{
    [Fact]
    public async Task Dispatch_EmailVerificationRequest_ForExistingUser_DeliversSinglePayload()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var userRepo = new InMemoryUserRepository();
        var tokenRepo = new InMemorySecurityTokenRepository();
        var delivery = new InMemoryIdentityTokenDelivery();

        var user = new UserAccount("verify@test.com", "HASH:ValidPass123!");
        await userRepo.AddAsync(user);
        await userRepo.SaveChangesAsync();

        var service = new IdentityService(
            userRepo,
            new InMemoryRefreshSessionRepository(),
            new FakeTokenService(),
            new FakePasswordHasherService(),
            clock,
            tokenRepo,
            delivery,
            new SecurityAuditService());

        await service.RequestEmailVerificationAsync(user.Email);

        var delivered = Assert.Single(delivery.EmailVerificationDeliveries);
        Assert.Equal(user.Email, delivered.Email);
        Assert.False(string.IsNullOrWhiteSpace(delivered.RawToken));
        Assert.Equal(clock.UtcNow.AddMinutes(SecurityPolicy.PasswordResetTokenLifetimeMinutes), delivered.ExpiresAtUtc);
    }

    [Fact]
    public async Task Dispatch_PasswordResetRequest_ForExistingUser_DeliversSinglePayload()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var userRepo = new InMemoryUserRepository();
        var tokenRepo = new InMemorySecurityTokenRepository();
        var delivery = new InMemoryIdentityTokenDelivery();

        var user = new UserAccount("reset@test.com", "HASH:ValidPass123!");
        await userRepo.AddAsync(user);
        await userRepo.SaveChangesAsync();

        var service = new IdentityService(
            userRepo,
            new InMemoryRefreshSessionRepository(),
            new FakeTokenService(),
            new FakePasswordHasherService(),
            clock,
            tokenRepo,
            delivery,
            new SecurityAuditService());

        await service.RequestPasswordResetAsync(user.Email);

        var delivered = Assert.Single(delivery.PasswordResetDeliveries);
        Assert.Equal(user.Email, delivered.Email);
        Assert.False(string.IsNullOrWhiteSpace(delivered.RawToken));
        Assert.Equal(clock.UtcNow.AddMinutes(SecurityPolicy.PasswordResetTokenLifetimeMinutes), delivered.ExpiresAtUtc);
    }

    [Fact]
    public async Task Dispatch_UnknownEmail_DoesNotDeliverToken_ForEitherRequestType()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var userRepo = new InMemoryUserRepository();
        var tokenRepo = new InMemorySecurityTokenRepository();
        var delivery = new InMemoryIdentityTokenDelivery();

        var service = new IdentityService(
            userRepo,
            new InMemoryRefreshSessionRepository(),
            new FakeTokenService(),
            new FakePasswordHasherService(),
            clock,
            tokenRepo,
            delivery,
            new SecurityAuditService());

        await service.RequestEmailVerificationAsync("unknown@test.com");
        await service.RequestPasswordResetAsync("unknown@test.com");

        Assert.Empty(delivery.EmailVerificationDeliveries);
        Assert.Empty(delivery.PasswordResetDeliveries);
    }
}
