using Application.Identity.Contracts;
using Domain.Identity;

namespace Application.Identity.Services;

public sealed class TokenRotationService
{
    private readonly IRefreshSessionRepository _refreshSessionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ISystemClock _clock;
    private readonly SecurityAuditService? _audit;

    public TokenRotationService(
        IRefreshSessionRepository refreshSessionRepository,
        IUserRepository userRepository,
        ITokenService tokenService,
        ISystemClock clock,
        SecurityAuditService? audit = null)
    {
        _refreshSessionRepository = refreshSessionRepository;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _clock = clock;
        _audit = audit;
    }

    public async Task<LoginResult> RotateAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var incomingHash = _tokenService.HashToken(refreshToken);
        var current = await _refreshSessionRepository.GetActiveByTokenHashAsync(incomingHash, now, cancellationToken);
        if (current is null)
        {
            _audit?.Record(SecurityAuditService.RefreshReuseBlocked, null, null, ipAddress);
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var user = await _userRepository.GetByIdAsync(current.UserId, cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var nextRefresh = _tokenService.IssueRefreshToken(new RefreshTokenIssueRequest(user.Id, current.SessionFamilyId, now, ipAddress));
        var nextSession = new RefreshSession(
            user.Id,
            current.SessionFamilyId,
            nextRefresh.TokenHash,
            now,
            nextRefresh.ExpiresAtUtc,
            ipAddress);

        await _refreshSessionRepository.RevokeCurrentAndInsertNextAsync(current, nextSession, now, ipAddress, cancellationToken);
        await _refreshSessionRepository.SaveChangesAsync(cancellationToken);

        var access = _tokenService.IssueAccessToken(new AccessTokenRequest(user.Id, user.Email, user.Role, user.EmailVerified, now));
        _audit?.Record(SecurityAuditService.RefreshRotated, user.Id, user.Email, ipAddress);

        return new LoginResult(access.Token, nextRefresh.RawToken, access.ExpiresAtUtc, nextRefresh.ExpiresAtUtc);
    }
}
