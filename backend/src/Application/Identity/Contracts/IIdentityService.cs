namespace Application.Identity.Contracts;

public interface IIdentityService
{
    Task<RegisterResult> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken = default);
    Task<LoginResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);
    Task RequestEmailVerificationAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ConfirmEmailVerificationAsync(string token, CancellationToken cancellationToken = default);
    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ConfirmPasswordResetAsync(string token, string newPassword, CancellationToken cancellationToken = default);
}

public sealed record RegisterCommand(string Name, string Email, string Password);

public sealed record LoginCommand(string Email, string Password, string? IpAddress = null);

public sealed record RegisterResult(Guid UserId, string Name, string Email);

public sealed record LoginResult(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAtUtc, DateTimeOffset RefreshTokenExpiresAtUtc);
