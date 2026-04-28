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
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var encryptionKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.EncryptionKeyOrFallback));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var encryptingCredentials = new EncryptingCredentials(
            encryptionKey,
            SecurityAlgorithms.Aes256KW,
            SecurityAlgorithms.Aes256CbcHmacSha512);

        var claims = new[]
        {
            new Claim("sub", request.UserId.ToString()),
            new Claim(ClaimTypes.Name, request.Name),
            new Claim("name", request.Name),
            new Claim(ClaimTypes.Email, request.Email),
            new Claim("email", request.Email),
            new Claim(ClaimTypes.Role, request.Role.ToAuthorizationRole()),
            new Claim("role", request.Role.ToAuthorizationRole()),
            new Claim("email_verified", request.EmailVerified ? "true" : "false")
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = request.NowUtc.UtcDateTime,
            Expires = expiresAtUtc.UtcDateTime,
            SigningCredentials = signingCredentials,
            EncryptingCredentials = encryptingCredentials
        };

        var token = new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor);
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
    public string EncryptionKey { get; init; } = string.Empty;

    internal string EncryptionKeyOrFallback =>
        string.IsNullOrWhiteSpace(EncryptionKey) ? SigningKey : EncryptionKey;
}
