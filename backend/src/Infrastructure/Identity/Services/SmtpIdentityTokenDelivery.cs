using Application.Identity.Contracts;
using Infrastructure.Identity.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Identity.Services;

public sealed class SmtpIdentityTokenDelivery : IIdentityTokenDelivery
{
    private readonly ISmtpTokenTransport _transport;
    private readonly ILogger<SmtpIdentityTokenDelivery> _logger;
    private readonly IdentityTokenDeliveryOptions _options;

    public SmtpIdentityTokenDelivery(
        IOptions<IdentityTokenDeliveryOptions> options,
        ISmtpTokenTransport transport,
        ILogger<SmtpIdentityTokenDelivery> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _transport = transport;
        _logger = logger;

        Validate(_options);
    }

    public Task DeliverEmailVerificationTokenAsync(
        EmailVerificationTokenDeliveryPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        _logger.LogInformation(
            "Dispatching email verification token for user {UserId} expiring at {ExpiresAtUtc}.",
            payload.UserId,
            payload.ExpiresAtUtc);

        var message = new SmtpOutgoingMessage(
            ToEmail: payload.Email,
            Subject: "Verify your email",
            Body:
                $"Your verification token is: {payload.RawToken}{Environment.NewLine}" +
                $"This token expires at: {payload.ExpiresAtUtc:O}",
            ExpiresAtUtc: payload.ExpiresAtUtc,
            Metadata: new Dictionary<string, string>
            {
                ["purpose"] = "email_verification",
                ["userId"] = payload.UserId.ToString("N"),
            });

        return _transport.SendAsync(message, cancellationToken);
    }

    public Task DeliverPasswordResetTokenAsync(
        PasswordResetTokenDeliveryPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        _logger.LogInformation(
            "Dispatching password reset token for user {UserId} expiring at {ExpiresAtUtc}.",
            payload.UserId,
            payload.ExpiresAtUtc);

        var message = new SmtpOutgoingMessage(
            ToEmail: payload.Email,
            Subject: "Reset your password",
            Body:
                $"Your password reset token is: {payload.RawToken}{Environment.NewLine}" +
                $"This token expires at: {payload.ExpiresAtUtc:O}",
            ExpiresAtUtc: payload.ExpiresAtUtc,
            Metadata: new Dictionary<string, string>
            {
                ["purpose"] = "password_reset",
                ["userId"] = payload.UserId.ToString("N"),
            });

        return _transport.SendAsync(message, cancellationToken);
    }

    private static void Validate(IdentityTokenDeliveryOptions options)
    {
        if (!string.Equals(options.Provider, "smtp", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"IdentityTokenDelivery provider '{options.Provider}' is not supported by SmtpIdentityTokenDelivery.");
        }

        var smtp = options.Smtp ?? new IdentityTokenDeliverySmtpOptions();
        if (string.IsNullOrWhiteSpace(smtp.Host)
            || smtp.Port <= 0
            || string.IsNullOrWhiteSpace(smtp.Username)
            || string.IsNullOrWhiteSpace(smtp.Password)
            || string.IsNullOrWhiteSpace(smtp.FromEmail))
        {
            throw new InvalidOperationException(
                "IdentityTokenDelivery SMTP configuration is invalid. Required keys: Smtp:Host, Smtp:Port, Smtp:Username, Smtp:Password, Smtp:FromEmail.");
        }
    }
}

public interface ISmtpTokenTransport
{
    Task SendAsync(SmtpOutgoingMessage message, CancellationToken cancellationToken = default);
}

public sealed record SmtpOutgoingMessage(
    string ToEmail,
    string Subject,
    string Body,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyDictionary<string, string> Metadata);
