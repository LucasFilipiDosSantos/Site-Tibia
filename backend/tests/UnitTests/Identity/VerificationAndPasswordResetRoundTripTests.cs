using Application.Identity.Contracts;
using Application.Identity.Services;
using Infrastructure.Identity.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;
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

    [Fact]
    public async Task Endpoint_RequestEndpoints_ReturnGenericSuccess_ForExistingAndUnknownUsers()
    {
        await using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        var existingResponse = await client.PostAsJsonAsync("/auth/verify-email/request", new { email = "verify@test.com" });
        var unknownResponse = await client.PostAsJsonAsync("/auth/verify-email/request", new { email = "unknown@test.com" });

        Assert.Equal(HttpStatusCode.OK, existingResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unknownResponse.StatusCode);
        Assert.Equal(await existingResponse.Content.ReadAsStringAsync(), await unknownResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Endpoint_VerificationTokenRoundTrip_SucceedsOnce_ThenRejectsReplay()
    {
        await using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        var request = await client.PostAsJsonAsync("/auth/verify-email/request", new { email = "verify@test.com" });
        Assert.Equal(HttpStatusCode.OK, request.StatusCode);

        var token = factory.TokenDelivery.EmailVerificationDeliveries.Last().RawToken;

        var confirm = await client.PostAsJsonAsync("/auth/verify-email/confirm", new { token });
        var replay = await client.PostAsJsonAsync("/auth/verify-email/confirm", new { token });

        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
    }

    [Fact]
    public async Task Endpoint_PasswordResetRoundTrip_SucceedsOnce_ThenRejectsReplay()
    {
        await using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        var request = await client.PostAsJsonAsync("/auth/password-reset/request", new { email = "verify@test.com" });
        Assert.Equal(HttpStatusCode.OK, request.StatusCode);

        var token = factory.TokenDelivery.PasswordResetDeliveries.Last().RawToken;

        var confirm = await client.PostAsJsonAsync("/auth/password-reset/confirm", new { token, newPassword = "ValidPass999!" });
        var replay = await client.PostAsJsonAsync("/auth/password-reset/confirm", new { token, newPassword = "ValidPass999!" });

        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
    }

    private sealed class IdentityApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private readonly InMemoryUserRepository _users = new();
        private readonly InMemoryRefreshSessionRepository _refreshSessions = new();
        private readonly InMemorySecurityTokenRepository _tokens = new();
        private readonly FixedClock _clock = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        private readonly FakeTokenService _tokenService = new();
        private readonly FakePasswordHasherService _passwordHasher = new();

        public InMemoryIdentityTokenDelivery TokenDelivery { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUserRepository>();
                services.RemoveAll<IRefreshSessionRepository>();
                services.RemoveAll<ISecurityTokenRepository>();
                services.RemoveAll<ITokenService>();
                services.RemoveAll<IPasswordHasherService>();
                services.RemoveAll<ISystemClock>();
                services.RemoveAll<IIdentityTokenDelivery>();

                services.AddSingleton<IUserRepository>(_users);
                services.AddSingleton<IRefreshSessionRepository>(_refreshSessions);
                services.AddSingleton<ISecurityTokenRepository>(_tokens);
                services.AddSingleton<ITokenService>(_tokenService);
                services.AddSingleton<IPasswordHasherService>(_passwordHasher);
                services.AddSingleton<ISystemClock>(_clock);
                services.AddSingleton<IIdentityTokenDelivery>(TokenDelivery);
            });
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
        }
    }
}
