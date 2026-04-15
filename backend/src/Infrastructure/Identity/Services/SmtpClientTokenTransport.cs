using Infrastructure.Identity.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Identity.Services;

public sealed class SmtpClientTokenTransport : ISmtpTokenTransport
{
    private readonly IdentityTokenDeliverySmtpOptions _smtp;
    private readonly ILogger<SmtpClientTokenTransport> _logger;

    public SmtpClientTokenTransport(
        IOptions<IdentityTokenDeliveryOptions> options,
        ILogger<SmtpClientTokenTransport> logger)
    {
        _smtp = options?.Value?.Smtp ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    public async Task SendAsync(SmtpOutgoingMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var mail = new MailMessage(_smtp.FromEmail, message.ToEmail)
        {
            Subject = message.Subject,
            Body = message.Body,
        };

        using var client = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            EnableSsl = _smtp.UseTls,
            Credentials = new NetworkCredential(_smtp.Username, _smtp.Password),
        };

        try
        {
            await client.SendMailAsync(mail, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "SMTP dispatch failed for recipient {Recipient} with subject {Subject}.",
                message.ToEmail,
                message.Subject);
            throw;
        }
    }
}
