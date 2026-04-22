namespace API.Auth;

public sealed record RegisterRequest(string Name, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record VerificationRequest(string Email);
public sealed record VerificationConfirmRequest(string Token);
public sealed record PasswordResetRequest(string Email);
public sealed record PasswordResetConfirmRequest(string Token, string NewPassword);

public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAtUtc, DateTimeOffset RefreshTokenExpiresAtUtc);
