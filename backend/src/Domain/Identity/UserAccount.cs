namespace Domain.Identity;

public sealed class UserAccount
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public bool EmailVerified { get; private set; }
    public int FailedLoginCount { get; private set; }
    public DateTimeOffset? LockoutEndsAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public UserAccount(string name, string email, string passwordHash, UserRole role = UserRole.Costumer)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        Name = NormalizeName(name);
        Email = NormalizeEmail(email);
        PasswordHash = passwordHash;
        Role = role;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }
    
    public UserAccount(Guid id, string name, string email, string passwordHash, UserRole role = UserRole.Costumer) : this(name, email, passwordHash, role)
    {
        Id = id;
    }

    private UserAccount()
    {
        Name = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
        Role = UserRole.Costumer;
    }

    public bool IsLockedOut(DateTimeOffset nowUtc)
    {
        return LockoutEndsAtUtc.HasValue && LockoutEndsAtUtc.Value > nowUtc;
    }

    public void MarkEmailVerified()
    {
        EmailVerified = true;
        Touch();
    }

    public void SetPasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(newPasswordHash));
        }

        PasswordHash = newPasswordHash;
        Touch();
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        Name = NormalizeName(name);
        Touch();
    }

    public void ChangeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        Email = NormalizeEmail(email);
        Touch();
    }

    public void SetRole(UserRole role)
    {
        Role = role;
        Touch();
    }

    public void SetEmailVerified(bool emailVerified)
    {
        EmailVerified = emailVerified;
        Touch();
    }

    public void RecordFailedLogin(DateTimeOffset nowUtc)
    {
        FailedLoginCount++;

        if (FailedLoginCount >= SecurityPolicy.FailedLoginThreshold)
        {
            LockoutEndsAtUtc = nowUtc.AddMinutes(SecurityPolicy.LockoutDurationMinutes);
        }

        Touch(nowUtc);
    }

    public void ResetFailedLogin(DateTimeOffset nowUtc)
    {
        FailedLoginCount = 0;
        LockoutEndsAtUtc = null;
        Touch(nowUtc);
    }

    private void Touch(DateTimeOffset? nowUtc = null)
    {
        UpdatedAtUtc = nowUtc ?? DateTimeOffset.UtcNow;
    }

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    public static string NormalizeName(string name)
    {
        return name.Trim();
    }
}
