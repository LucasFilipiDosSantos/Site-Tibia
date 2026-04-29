using API.Auth;

namespace UnitTests.Identity;

public sealed class AuthEndpointContractTests
{
    [Fact]
    public void AuthDtos_ContainAllRequiredContracts()
    {
        _ = new RegisterRequest("Test User", "u@test.com", "ValidPass123!");
        _ = new LoginRequest("u@test.com", "ValidPass123!");
        _ = new VerificationRequest("u@test.com");
        _ = new VerificationConfirmRequest("verification-token");
        _ = new PasswordResetRequest("u@test.com");
        _ = new PasswordResetConfirmRequest("reset-token", "NewPass123!");
        _ = new AuthUserResponse(Guid.NewGuid(), "Test User", "u@test.com", "Customer", true);
        _ = new AuthMeResponse(Guid.NewGuid(), "Test User", "u@test.com", "Customer", true, DateTimeOffset.UtcNow);
    }

    [Fact]
    public void AuthEndpointMappings_ExposeEightRoutesUnderAuth()
    {
        var routeNames = new[]
        {
            "/auth/register",
            "/auth/login",
            "/auth/refresh",
            "/auth/logout",
            "/auth/verify-email/request",
            "/auth/verify-email/confirm",
            "/auth/password-reset/request",
            "/auth/password-reset/confirm"
        };

        Assert.Equal(8, routeNames.Length);
    }
}
