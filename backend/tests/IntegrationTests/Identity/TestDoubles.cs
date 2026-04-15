using Application.Identity.Contracts;
using Domain.Identity;

namespace IntegrationTests.Identity;

internal sealed class InMemoryUserRepository : IUserRepository
{
    public List<UserAccount> Users { get; } = new();

    public Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            Users.SingleOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase))
        );
    }

    public Task<UserAccount?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Users.SingleOrDefault(u => u.Id == userId));
    }

    public Task AddAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryRefreshSessionRepository : IRefreshSessionRepository
{
    public List<RefreshSession> Sessions { get; } = new();

    public Task<RefreshSession?> GetActiveByTokenHashAsync(
        string tokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(
            Sessions.SingleOrDefault(s => s.TokenHash == tokenHash && !s.IsRevoked && !s.IsExpired(nowUtc))
        );
    }

    public Task AddAsync(RefreshSession session, CancellationToken cancellationToken = default)
    {
        Sessions.Add(session);
        return Task.CompletedTask;
    }

    public Task RevokeCurrentAndInsertNextAsync(
        RefreshSession currentSession,
        RefreshSession nextSession,
        DateTimeOffset revokedAtUtc,
        string? revokedByIp,
        CancellationToken cancellationToken = default
    )
    {
        currentSession.Revoke(revokedAtUtc, revokedByIp, nextSession.TokenHash);
        Sessions.Add(nextSession);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal sealed class InMemorySecurityTokenRepository : ISecurityTokenRepository
{
    public List<SecurityToken> Tokens { get; } = new();

    public Task AddAsync(SecurityToken token, CancellationToken cancellationToken = default)
    {
        Tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<SecurityToken?> GetActiveByTokenHashAsync(
        string tokenHash,
        string purpose,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(
            Tokens.SingleOrDefault(
                t =>
                    t.TokenHash == tokenHash
                    && t.Purpose == purpose
                    && !t.IsConsumed
                    && !t.IsExpired(nowUtc)
            )
        );
    }

    public Task UpdateAsync(SecurityToken token, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal sealed class FakePasswordHasherService : IPasswordHasherService
{
    public string HashPassword(string password) => $"HASH:{password}";

    public bool VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        return hashedPassword == HashPassword(providedPassword);
    }
}

internal sealed class FakeTokenService : ITokenService
{
    private int _refreshCounter;

    public AccessTokenResult IssueAccessToken(AccessTokenRequest request)
    {
        var expires = request.NowUtc.AddMinutes(SecurityPolicy.AccessTokenLifetimeMinutes);
        return new AccessTokenResult($"access-{request.UserId:N}", expires);
    }

    public RefreshTokenIssueResult IssueRefreshToken(RefreshTokenIssueRequest request)
    {
        _refreshCounter++;
        var raw = $"refresh-{request.UserId:N}-{_refreshCounter}";
        var hash = HashToken(raw);
        var expires = request.NowUtc.AddDays(SecurityPolicy.RefreshTokenLifetimeDays);
        return new RefreshTokenIssueResult(raw, hash, expires);
    }

    public string HashToken(string rawToken) => $"sha256:{rawToken}";
}

internal sealed class FixedClock : ISystemClock
{
    public FixedClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; set; }
}

internal sealed class InMemoryIdentityTokenDelivery : IIdentityTokenDelivery
{
    public List<EmailVerificationTokenDeliveryPayload> EmailVerificationDeliveries { get; } = new();
    public List<PasswordResetTokenDeliveryPayload> PasswordResetDeliveries { get; } = new();

    public Task DeliverEmailVerificationTokenAsync(
        EmailVerificationTokenDeliveryPayload payload,
        CancellationToken cancellationToken = default
    )
    {
        EmailVerificationDeliveries.Add(payload);
        return Task.CompletedTask;
    }

    public Task DeliverPasswordResetTokenAsync(
        PasswordResetTokenDeliveryPayload payload,
        CancellationToken cancellationToken = default
    )
    {
        PasswordResetDeliveries.Add(payload);
        return Task.CompletedTask;
    }
}
