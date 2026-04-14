namespace Application.Identity.Services;

public sealed class SecurityAuditService
{
    public const string LoginFailed = "login_failed";
    public const string LockoutApplied = "lockout_applied";
    public const string LockoutReleased = "lockout_released";
    public const string RefreshRotated = "refresh_rotated";
    public const string RefreshReuseBlocked = "refresh_reuse_blocked";
    public const string PasswordResetRequested = "password_reset_requested";
    public const string PasswordResetCompleted = "password_reset_completed";
    public const string EmailVerificationRequested = "email_verification_requested";
    public const string EmailVerificationDispatchAttempted = "email_verification_dispatch_attempted";
    public const string PasswordResetDispatchAttempted = "password_reset_dispatch_attempted";
    public const string TokenRequestUnknownEmail = "token_request_unknown_email";

    private readonly List<SecurityAuditEvent> _events = new();

    public IReadOnlyCollection<SecurityAuditEvent> Events => _events.AsReadOnly();

    public void Record(string eventName, Guid? userId = null, string? email = null, string? ipAddress = null)
    {
        _events.Add(new SecurityAuditEvent(eventName, userId, email, ipAddress, DateTimeOffset.UtcNow));
    }
}

public sealed record SecurityAuditEvent(
    string EventName,
    Guid? UserId,
    string? Email,
    string? IpAddress,
    DateTimeOffset OccurredAtUtc);
