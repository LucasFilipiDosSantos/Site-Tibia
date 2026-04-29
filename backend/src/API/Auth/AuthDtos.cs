namespace API.Auth;

public sealed record RegisterRequest(string Name, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record VerificationRequest(string Email);
public sealed record VerificationConfirmRequest(string Token);
public sealed record PasswordResetRequest(string Email);
public sealed record PasswordResetConfirmRequest(string Token, string NewPassword);

public sealed record AuthUserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool EmailVerified);

public sealed record AuthMeResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool EmailVerified,
    DateTimeOffset CreatedAtUtc);
