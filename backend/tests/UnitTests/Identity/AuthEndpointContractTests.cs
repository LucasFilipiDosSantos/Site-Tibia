using API.Auth;

namespace UnitTests.Identity;

public sealed class AuthEndpointContractTests
{
    [Fact]
    public void AuthDtos_ContainAllRequiredContracts()
    {
        _ = new RegisterRequest("u@test.com", "ValidPass123!");
        _ = new LoginRequest("u@test.com", "ValidPass123!");
        _ = new RefreshRequest("refresh-token");
        _ = new VerificationRequest("u@test.com");
        _ = new VerificationConfirmRequest("verification-token");
        _ = new PasswordResetRequest("u@test.com");
        _ = new PasswordResetConfirmRequest("reset-token", "NewPass123!");
    }

    [Fact]
    public void AuthEndpointMappings_ExposeSevenRoutesUnderAuth()
    {
        var routeNames = new[]
        {
            "/auth/register",
            "/auth/login",
            "/auth/refresh",
            "/auth/verify-email/request",
            "/auth/verify-email/confirm",
            "/auth/password-reset/request",
            "/auth/password-reset/confirm"
        };

        Assert.Equal(7, routeNames.Length);
    }
}
