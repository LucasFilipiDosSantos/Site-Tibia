using System.Net.Mail;
using Application.Identity.Contracts;
using Application.Identity.Exceptions;
using Domain.Identity;

namespace Application.Identity.Services;

public sealed class IdentityService : IIdentityService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshSessionRepository _refreshSessionRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ISystemClock _clock;
    private readonly ISecurityTokenRepository? _securityTokenRepository;
    private readonly IIdentityTokenDelivery? _tokenDelivery;
    private readonly SecurityAuditService? _audit;

    public IdentityService(
        IUserRepository userRepository,
        IRefreshSessionRepository refreshSessionRepository,
        ITokenService tokenService,
        IPasswordHasherService passwordHasher,
        ISystemClock clock,
        ISecurityTokenRepository? securityTokenRepository = null,
        IIdentityTokenDelivery? tokenDelivery = null,
        SecurityAuditService? audit = null
    )
    {
        _userRepository = userRepository;
        _refreshSessionRepository = refreshSessionRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _securityTokenRepository = securityTokenRepository;
        _tokenDelivery = tokenDelivery;
        _audit = audit;
    }

    public async Task<RegisterResult> RegisterAsync(
        RegisterCommand command,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsValidEmail(command.Email))
        {
            throw new ArgumentException("Email format is invalid.", nameof(command.Email));
        }

        var existing = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("User already exists.");
        }

        if (!SecurityPolicy.IsPasswordCompliant(command.Password))
        {
            throw new ArgumentException(
                "Password does not meet complexity requirements.",
                nameof(command.Password)
            );
        }

        var hash = _passwordHasher.HashPassword(command.Password);
        var user = new UserAccount(command.Email, hash);
        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
        return new RegisterResult(user.Id, user.Email);
    }

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        if (!MailAddress.TryCreate(email.Trim(), out var parsed))
        {
            return false;
        }

        return string.Equals(parsed.Address, email.Trim(), StringComparison.Ordinal);
    }

    public async Task<LoginResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var now = _clock.UtcNow;
        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (user.IsLockedOut(now))
        {
            _audit?.Record(
                SecurityAuditService.LockoutApplied,
                user.Id,
                user.Email,
                command.IpAddress
            );
            throw new InvalidOperationException("User is locked out.");
        }

        var valid = _passwordHasher.VerifyHashedPassword(user.PasswordHash, command.Password);
        if (!valid)
        {
            user.RecordFailedLogin(now);
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);
            _audit?.Record(
                SecurityAuditService.LoginFailed,
                user.Id,
                user.Email,
                command.IpAddress
            );
            if (user.IsLockedOut(now))
            {
                _audit?.Record(
                    SecurityAuditService.LockoutApplied,
                    user.Id,
                    user.Email,
                    command.IpAddress
                );
            }

            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (user.LockoutEndsAtUtc.HasValue && user.LockoutEndsAtUtc.Value <= now)
        {
            _audit?.Record(
                SecurityAuditService.LockoutReleased,
                user.Id,
                user.Email,
                command.IpAddress
            );
        }

        user.ResetFailedLogin(now);
        await _userRepository.UpdateAsync(user, cancellationToken);

        var access = _tokenService.IssueAccessToken(
            new AccessTokenRequest(user.Id, user.Email, user.Role, user.EmailVerified, now)
        );
        var familyId = Guid.NewGuid();
        var refresh = _tokenService.IssueRefreshToken(
            new RefreshTokenIssueRequest(user.Id, familyId, now, command.IpAddress)
        );

        var refreshSession = new RefreshSession(
            user.Id,
            familyId,
            refresh.TokenHash,
            now,
            refresh.ExpiresAtUtc,
            command.IpAddress
        );
        await _refreshSessionRepository.AddAsync(refreshSession, cancellationToken);
        await _refreshSessionRepository.SaveChangesAsync(cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new LoginResult(
            access.Token,
            refresh.RawToken,
            access.ExpiresAtUtc,
            refresh.ExpiresAtUtc
        );
    }

    public async Task RequestEmailVerificationAsync(
        string email,
        CancellationToken cancellationToken = default
    )
    {
        if (_securityTokenRepository is null || _tokenDelivery is null)
        {
            return;
        }

        var now = _clock.UtcNow;
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            _audit?.Record(SecurityAuditService.TokenRequestUnknownEmail, null, email, null);
            return;
        }

        var rawToken = Guid.NewGuid().ToString("N");
        var tokenHash = _tokenService.HashToken(rawToken);
        var expiresAtUtc = now.AddMinutes(SecurityPolicy.PasswordResetTokenLifetimeMinutes);
        var token = new SecurityToken(
            user.Id,
            tokenHash,
            SecurityTokenPurposes.EmailVerification,
            now,
            expiresAtUtc
        );
        await _securityTokenRepository.AddAsync(token, cancellationToken);
        await _securityTokenRepository.SaveChangesAsync(cancellationToken);

        _audit?.Record(SecurityAuditService.EmailVerificationRequested, user.Id, user.Email, null);
        _audit?.Record(
            SecurityAuditService.EmailVerificationDispatchAttempted,
            user.Id,
            user.Email,
            null
        );

        try
        {
            await _tokenDelivery.DeliverEmailVerificationTokenAsync(
                new EmailVerificationTokenDeliveryPayload(user.Id, user.Email, rawToken, expiresAtUtc),
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _audit?.Record(
                SecurityAuditService.EmailVerificationDispatchFailed,
                user.Id,
                user.Email,
                null
            );

            throw new TokenDeliveryUnavailableException("email_verification_request", user.Email, ex);
        }
    }

    public async Task<bool> ConfirmEmailVerificationAsync(
        string token,
        CancellationToken cancellationToken = default
    )
    {
        if (_securityTokenRepository is null)
        {
            return false;
        }

        var now = _clock.UtcNow;
        var hash = _tokenService.HashToken(token);
        var securityToken = await _securityTokenRepository.GetActiveByTokenHashAsync(
            hash,
            SecurityTokenPurposes.EmailVerification,
            now,
            cancellationToken
        );
        if (securityToken is null || securityToken.IsExpired(now) || securityToken.IsConsumed)
        {
            return false;
        }

        var user = await _userRepository.GetByIdAsync(securityToken.UserId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.MarkEmailVerified();
        securityToken.MarkConsumed(now);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _securityTokenRepository.UpdateAsync(securityToken, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
        await _securityTokenRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task RequestPasswordResetAsync(
        string email,
        CancellationToken cancellationToken = default
    )
    {
        if (_securityTokenRepository is null || _tokenDelivery is null)
        {
            return;
        }

        var now = _clock.UtcNow;
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            _audit?.Record(SecurityAuditService.TokenRequestUnknownEmail, null, email, null);
            return;
        }

        var rawToken = Guid.NewGuid().ToString("N");
        var tokenHash = _tokenService.HashToken(rawToken);
        var expiresAtUtc = now.AddMinutes(SecurityPolicy.PasswordResetTokenLifetimeMinutes);
        var token = new SecurityToken(
            user.Id,
            tokenHash,
            SecurityTokenPurposes.PasswordReset,
            now,
            expiresAtUtc
        );

        await _securityTokenRepository.AddAsync(token, cancellationToken);
        await _securityTokenRepository.SaveChangesAsync(cancellationToken);
        _audit?.Record(SecurityAuditService.PasswordResetRequested, user.Id, user.Email, null);

        _audit?.Record(
            SecurityAuditService.PasswordResetDispatchAttempted,
            user.Id,
            user.Email,
            null
        );
        try
        {
            await _tokenDelivery.DeliverPasswordResetTokenAsync(
                new PasswordResetTokenDeliveryPayload(user.Id, user.Email, rawToken, expiresAtUtc),
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _audit?.Record(
                SecurityAuditService.PasswordResetDispatchFailed,
                user.Id,
                user.Email,
                null
            );

            throw new TokenDeliveryUnavailableException("password_reset_request", user.Email, ex);
        }
    }

    public async Task<bool> ConfirmPasswordResetAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken = default
    )
    {
        if (_securityTokenRepository is null)
        {
            return false;
        }

        if (!SecurityPolicy.IsPasswordCompliant(newPassword))
        {
            return false;
        }

        var now = _clock.UtcNow;
        var hash = _tokenService.HashToken(token);
        var securityToken = await _securityTokenRepository.GetActiveByTokenHashAsync(
            hash,
            SecurityTokenPurposes.PasswordReset,
            now,
            cancellationToken
        );

        if (securityToken is null || securityToken.IsExpired(now) || securityToken.IsConsumed)
        {
            return false;
        }

        var user = await _userRepository.GetByIdAsync(securityToken.UserId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.SetPasswordHash(_passwordHasher.HashPassword(newPassword));
        securityToken.MarkConsumed(now);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _securityTokenRepository.UpdateAsync(securityToken, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
        await _securityTokenRepository.SaveChangesAsync(cancellationToken);

        _audit?.Record(SecurityAuditService.PasswordResetCompleted, user.Id, user.Email, null);
        return true;
    }
}

public static class SecurityTokenPurposes
{
    public const string EmailVerification = "email_verification";
    public const string PasswordReset = "password_reset";
}
