using Application.Identity.Contracts;
using Application.Identity.Services;
using Domain.Identity;
using Infrastructure.Identity.Options;
using Infrastructure.Identity.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Identity;

[Trait("Category", "IdentitySecurity")]
[Trait("Requirement", "AUTH-02")]
[Trait("Suite", "Phase01AuthRegression")]
[Trait("Plan", "01-07")]
public sealed class ExternalTokenDeliveryRoundTripTests
{
    [Fact]
    public async Task Adapter_DeliverVerificationAndReset_ForwardsBothPayloadsToTransport()
    {
        var transport = new CapturingSmtpTokenTransport();
        var sut = new SmtpIdentityTokenDelivery(
            Options.Create(ValidOptions()),
            transport,
            NullLogger<SmtpIdentityTokenDelivery>.Instance);

        await sut.DeliverEmailVerificationTokenAsync(
            new EmailVerificationTokenDeliveryPayload(
                Guid.NewGuid(),
                "verify@test.com",
                "verify-token",
                new DateTimeOffset(2026, 1, 1, 12, 30, 0, TimeSpan.Zero)));

        await sut.DeliverPasswordResetTokenAsync(
            new PasswordResetTokenDeliveryPayload(
                Guid.NewGuid(),
                "reset@test.com",
                "reset-token",
                new DateTimeOffset(2026, 1, 1, 13, 0, 0, TimeSpan.Zero)));

        Assert.Equal(2, transport.Messages.Count);
        Assert.Contains(transport.Messages, m => m.ToEmail == "verify@test.com" && m.Subject.Contains("Verify", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(transport.Messages, m => m.ToEmail == "reset@test.com" && m.Subject.Contains("Reset", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Adapter_Constructor_WithMissingSmtpSettings_ThrowsDeterministicError()
    {
        var invalid = new IdentityTokenDeliveryOptions
        {
            Provider = "smtp",
            Smtp = new IdentityTokenDeliverySmtpOptions
            {
                Host = "",
                Port = 0,
                Username = "",
                Password = "",
                FromEmail = "",
                UseTls = true
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new SmtpIdentityTokenDelivery(
                Options.Create(invalid),
                new CapturingSmtpTokenTransport(),
                NullLogger<SmtpIdentityTokenDelivery>.Instance));

        Assert.Contains("IdentityTokenDelivery", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Adapter_MessageBody_IncludesExpiryMetadata()
    {
        var transport = new CapturingSmtpTokenTransport();
        var sut = new SmtpIdentityTokenDelivery(
            Options.Create(ValidOptions()),
            transport,
            NullLogger<SmtpIdentityTokenDelivery>.Instance);

        var expiresAt = new DateTimeOffset(2026, 1, 1, 14, 45, 0, TimeSpan.Zero);
        await sut.DeliverEmailVerificationTokenAsync(
            new EmailVerificationTokenDeliveryPayload(
                Guid.NewGuid(),
                "verify@test.com",
                "verify-token",
                expiresAt));

        var sent = Assert.Single(transport.Messages);
        Assert.Contains(expiresAt.ToString("O"), sent.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task External_VerificationRequest_CapturesToken_ConfirmsOnce_AndRejectsReplay()
    {
        await using var factory = new ExternalDeliveryApiFactory();
        using var client = factory.CreateClient();

        var request = await client.PostAsJsonAsync("/auth/verify-email/request", new { email = "verify@test.com" });
        Assert.Equal(HttpStatusCode.OK, request.StatusCode);

        var token = Assert.Single(factory.Transport.Messages).ExtractToken();

        var confirm = await client.PostAsJsonAsync("/auth/verify-email/confirm", new { token });
        var replay = await client.PostAsJsonAsync("/auth/verify-email/confirm", new { token });

        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
    }

    [Fact]
    public async Task External_PasswordResetRequest_CapturesToken_ConfirmsOnce_AndRejectsReplay()
    {
        await using var factory = new ExternalDeliveryApiFactory();
        using var client = factory.CreateClient();

        var request = await client.PostAsJsonAsync("/auth/password-reset/request", new { email = "verify@test.com" });
        Assert.Equal(HttpStatusCode.OK, request.StatusCode);

        var token = Assert.Single(factory.Transport.Messages).ExtractToken();

        var confirm = await client.PostAsJsonAsync("/auth/password-reset/confirm", new { token, newPassword = "ValidPass999!" });
        var replay = await client.PostAsJsonAsync("/auth/password-reset/confirm", new { token, newPassword = "ValidPass999!" });

        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
    }

    [Fact]
    public async Task External_UnknownEmail_RequestIsGeneric_AndDoesNotDispatchToken()
    {
        await using var factory = new ExternalDeliveryApiFactory();
        using var client = factory.CreateClient();

        var existing = await client.PostAsJsonAsync("/auth/verify-email/request", new { email = "verify@test.com" });
        var unknown = await client.PostAsJsonAsync("/auth/verify-email/request", new { email = "unknown@test.com" });

        Assert.Equal(HttpStatusCode.OK, existing.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
        Assert.Equal(
            await existing.Content.ReadAsStringAsync(),
            await unknown.Content.ReadAsStringAsync());
        Assert.Single(factory.Transport.Messages);
    }

    private static IdentityTokenDeliveryOptions ValidOptions() =>
        new()
        {
            Provider = "smtp",
            Smtp = new IdentityTokenDeliverySmtpOptions
            {
                Host = "smtp.test.local",
                Port = 2525,
                Username = "smtp-user",
                Password = "smtp-password",
                FromEmail = "noreply@test.local",
                UseTls = true
            }
        };

    private sealed class CapturingSmtpTokenTransport : ISmtpTokenTransport
    {
        public List<SmtpOutgoingMessage> Messages { get; } = new();

        public Task SendAsync(SmtpOutgoingMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public void Clear() => Messages.Clear();
    }

    private sealed class ExternalDeliveryApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private readonly InMemoryUserRepository _users = new();
        private readonly InMemoryRefreshSessionRepository _refreshSessions = new();
        private readonly InMemorySecurityTokenRepository _tokens = new();
        private readonly FixedClock _clock = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        private readonly FakeTokenService _tokenService = new();
        private readonly FakePasswordHasherService _passwordHasher = new();

        public CapturingSmtpTokenTransport Transport { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            SeedDefaultUser();

            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Jwt:Issuer"] = "tibia-webstore",
                        ["Jwt:Audience"] = "tibia-webstore-client",
                        ["Jwt:SigningKey"] = "01234567890123456789012345678901",
                    });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUserRepository>();
                services.RemoveAll<IRefreshSessionRepository>();
                services.RemoveAll<ISecurityTokenRepository>();
                services.RemoveAll<ITokenService>();
                services.RemoveAll<IPasswordHasherService>();
                services.RemoveAll<ISystemClock>();

                services.AddSingleton<IUserRepository>(_users);
                services.AddSingleton<IRefreshSessionRepository>(_refreshSessions);
                services.AddSingleton<ISecurityTokenRepository>(_tokens);
                services.AddSingleton<ITokenService>(_tokenService);
                services.AddSingleton<IPasswordHasherService>(_passwordHasher);
                services.AddSingleton<ISystemClock>(_clock);
                services.RemoveAll<ISmtpTokenTransport>();
                services.AddSingleton<ISmtpTokenTransport>(Transport);
            });
        }

        private void SeedDefaultUser()
        {
            if (_users.Users.Any())
            {
                return;
            }

            var user = new UserAccount("verify@test.com", _passwordHasher.HashPassword("ValidPass123!"));
            _users.Users.Add(user);
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
        }
    }
}

internal static class SmtpOutgoingMessageExtensions
{
    public static string ExtractToken(this SmtpOutgoingMessage message)
    {
        var marker = "token is:";
        var idx = message.Body.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return string.Empty;
        }

        var start = idx + marker.Length;
        var end = message.Body.IndexOf(Environment.NewLine, start, StringComparison.Ordinal);
        if (end < 0)
        {
            end = message.Body.Length;
        }

        return message.Body[start..end].Trim();
    }
}
