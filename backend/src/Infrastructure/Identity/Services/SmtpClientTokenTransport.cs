namespace Infrastructure.Identity.Services;

public sealed class SmtpClientTokenTransport : ISmtpTokenTransport
{
    public Task SendAsync(SmtpOutgoingMessage message, CancellationToken cancellationToken = default)
    {
        // Placeholder transport for phase scope: external provider integration surface.
        // Reliability concerns (retries/backoff) are planned for a later reliability phase.
        return Task.CompletedTask;
    }
}
