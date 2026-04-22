using Application.Identity.Contracts;
using Domain.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Identity.Services;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtTokenServiceOptions _options;

    public JwtTokenService(JwtTokenServiceOptions options)
    {
        _options = options;
    }

    public AccessTokenResult IssueAccessToken(AccessTokenRequest request)
    {
        var expiresAtUtc = request.NowUtc.AddMinutes(SecurityPolicy.AccessTokenLifetimeMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", request.UserId.ToString()),
            new Claim("name", request.Name),
            new Claim("email", request.Email),
            new Claim("role", request.Role.ToString()),
            new Claim("email_verified", request.EmailVerified ? "true" : "false")
        };

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: request.NowUtc.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: creds);

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return new AccessTokenResult(token, expiresAtUtc);
    }

    public RefreshTokenIssueResult IssueRefreshToken(RefreshTokenIssueRequest request)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var tokenHash = HashToken(rawToken);
        var expiresAtUtc = request.NowUtc.AddDays(SecurityPolicy.RefreshTokenLifetimeDays);
        return new RefreshTokenIssueResult(rawToken, tokenHash, expiresAtUtc);
    }

    public string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}

public sealed class JwtTokenServiceOptions
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
}
