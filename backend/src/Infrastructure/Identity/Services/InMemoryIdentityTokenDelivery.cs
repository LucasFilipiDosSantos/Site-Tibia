using Application.Identity.Contracts;

namespace Infrastructure.Identity.Services;

public sealed class InMemoryIdentityTokenDelivery : IIdentityTokenDelivery
{
    private readonly List<EmailVerificationTokenDeliveryPayload> _emailVerificationTokens = new();
    private readonly List<PasswordResetTokenDeliveryPayload> _passwordResetTokens = new();
    private readonly object _sync = new();

    public IReadOnlyList<EmailVerificationTokenDeliveryPayload> EmailVerificationTokens
    {
        get
        {
            lock (_sync)
            {
                return _emailVerificationTokens.ToList();
            }
        }
    }

    public IReadOnlyList<PasswordResetTokenDeliveryPayload> PasswordResetTokens
    {
        get
        {
            lock (_sync)
            {
                return _passwordResetTokens.ToList();
            }
        }
    }

    public Task DeliverEmailVerificationTokenAsync(EmailVerificationTokenDeliveryPayload payload, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _emailVerificationTokens.Add(payload);
        }

        return Task.CompletedTask;
    }

    public Task DeliverPasswordResetTokenAsync(PasswordResetTokenDeliveryPayload payload, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _passwordResetTokens.Add(payload);
        }

        return Task.CompletedTask;
    }
}
