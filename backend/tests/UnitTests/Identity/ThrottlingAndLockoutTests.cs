using API.Auth;

namespace UnitTests.Identity;

public sealed class ThrottlingAndLockoutTests
{
    [Fact]
    public void ThrottleKey_CombinesUserAndIp()
    {
        var path = "/auth/login";
        var user = "user@test.com".ToUpperInvariant();
        var ip = "127.0.0.1";
        var key = $"{path}:{user}:{ip}";

        Assert.Equal("/auth/login:USER@TEST.COM:127.0.0.1", key);
    }

    [Fact]
    public void LockoutDuration_IsFifteenMinutes()
    {
        Assert.Equal(15, Domain.Identity.SecurityPolicy.LockoutDurationMinutes);
    }

    [Fact]
    public void SecurityAuditEvents_ContainRequiredNames()
    {
        Assert.Equal("login_failed", Application.Identity.Services.SecurityAuditService.LoginFailed);
        Assert.Equal("lockout_applied", Application.Identity.Services.SecurityAuditService.LockoutApplied);
        Assert.Equal("refresh_rotated", Application.Identity.Services.SecurityAuditService.RefreshRotated);
        Assert.Equal("password_reset_completed", Application.Identity.Services.SecurityAuditService.PasswordResetCompleted);
    }
}
