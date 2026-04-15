using Application.Identity.Contracts;
using Infrastructure.Identity.Options;
using Infrastructure.Identity.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
    }
}
