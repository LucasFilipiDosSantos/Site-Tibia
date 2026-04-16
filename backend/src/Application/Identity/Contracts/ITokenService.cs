using Domain.Identity;

namespace Application.Identity.Contracts;

public interface ITokenService
{
    AccessTokenResult IssueAccessToken(AccessTokenRequest request);
    RefreshTokenIssueResult IssueRefreshToken(RefreshTokenIssueRequest request);
    string HashToken(string rawToken);
}

public sealed record AccessTokenRequest(
    Guid UserId,
    string Email,
    UserRole Role,
    bool EmailVerified,
    DateTimeOffset NowUtc);

public sealed record AccessTokenResult(
    string Token,
    DateTimeOffset ExpiresAtUtc);

public sealed record RefreshTokenIssueRequest(
    Guid UserId,
    Guid SessionFamilyId,
    DateTimeOffset NowUtc,
    string? IpAddress);

public sealed record RefreshTokenIssueResult(
    string RawToken,
    string TokenHash,
    DateTimeOffset ExpiresAtUtc);
