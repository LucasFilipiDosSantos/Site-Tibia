namespace Domain.Identity;

public static class SecurityPolicy
{
    public const int AccessTokenLifetimeMinutes = 15;
    public const int RefreshTokenLifetimeDays = 30;
    public const int LockoutDurationMinutes = 15;
    public const int PasswordMinLength = 10;
    public const int PasswordResetTokenLifetimeMinutes = 30;
    public const int FailedLoginThreshold = 5;

    public static bool IsPasswordCompliant(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < PasswordMinLength)
        {
            return false;
        }

        var hasUpper = false;
        var hasLower = false;
        var hasDigit = false;
        var hasSpecial = false;

        foreach (var c in password)
        {
            if (char.IsUpper(c)) hasUpper = true;
            else if (char.IsLower(c)) hasLower = true;
            else if (char.IsDigit(c)) hasDigit = true;
            else hasSpecial = true;
        }

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }
}
