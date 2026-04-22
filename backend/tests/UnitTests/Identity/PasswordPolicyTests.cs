using Application.Identity.Contracts;
using Application.Identity.Services;
using Domain.Identity;

namespace UnitTests.Identity;

public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData("short1A!")]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoDigits!!!!")]
    [InlineData("NoSpecial123")]
    public async Task RegisterAsync_RejectsWeakPassword(string password)
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RegisterAsync(new RegisterCommand("Test User", "user@example.com", password)));
    }

    [Fact]
    public async Task RegisterAsync_AcceptsCompliantPassword()
    {
        var repo = new InMemoryUserRepository();
        var service = CreateService(repo);

        var result = await service.RegisterAsync(new RegisterCommand("Test User", "user@example.com", "ValidPass123!"));

        Assert.NotEqual(Guid.Empty, result.UserId);
        Assert.Equal("Test User", result.Name);
        Assert.Equal("user@example.com", result.Email);
        Assert.Single(repo.Users);
    }

    [Fact]
    public async Task RegisterAsync_NormalizesEmailBeforePersisting()
    {
        var repo = new InMemoryUserRepository();
        var service = CreateService(repo);

        var result = await service.RegisterAsync(new RegisterCommand("Test User", "User@Example.COM", "ValidPass123!"));

        Assert.Equal("user@example.com", result.Email);
        Assert.Equal("user@example.com", Assert.Single(repo.Users).Email);
    }

    private static IdentityService CreateService(InMemoryUserRepository? userRepo = null)
    {
        return new IdentityService(
            userRepo ?? new InMemoryUserRepository(),
            new InMemoryRefreshSessionRepository(),
            new FakeTokenService(),
            new FakePasswordHasherService(),
            new FixedClock(DateTimeOffset.UtcNow));
    }
}
