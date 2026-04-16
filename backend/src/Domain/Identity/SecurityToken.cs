namespace Domain.Identity;

public sealed class SecurityToken
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public string Purpose { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    public bool IsConsumed => ConsumedAtUtc.HasValue;

    public SecurityToken(Guid userId, string tokenHash, string purpose, DateTimeOffset createdAtUtc, DateTimeOffset expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        }

        if (string.IsNullOrWhiteSpace(purpose))
        {
            throw new ArgumentException("Token purpose is required.", nameof(purpose));
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException("Token expiry must be after creation.", nameof(expiresAtUtc));
        }

        UserId = userId;
        TokenHash = tokenHash;
        Purpose = purpose;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    private SecurityToken()
    {
        TokenHash = string.Empty;
        Purpose = string.Empty;
    }

    public bool IsExpired(DateTimeOffset nowUtc) => ExpiresAtUtc <= nowUtc;

    public void MarkConsumed(DateTimeOffset consumedAtUtc)
    {
        ConsumedAtUtc = consumedAtUtc;
    }
}
