using Application.Identity.Contracts;
using Application.Identity.Services;
using Domain.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Auth;

public static class AuthEndpoints
{
    private const string AccessCookieName = "auth";
    private const string RefreshCookieName = "refresh";

    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", async (RegisterRequest request, IIdentityService identityService, CancellationToken ct) =>
        {
            await identityService.RegisterAsync(new RegisterCommand(request.Name, request.Email, request.Password), ct);
            return Results.Ok(new { message = "Registration successful." });
        }).WithTags("Auth");

        group.MapPost("/login", async (HttpContext context, LoginRequest request, IIdentityService identityService, CancellationToken ct) =>
        {
            context.Request.Headers["X-Auth-Identifier"] = request.Email;
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var result = await identityService.LoginAsync(new LoginCommand(request.Email, request.Password, ip), ct);
            AppendAuthCookies(context, result);
            return Results.Ok(ToAuthUserResponse(result.User));
        }).WithTags("Auth");

        group.MapPost("/refresh", async (HttpContext context, TokenRotationService rotationService, CancellationToken ct) =>
        {
            if (!context.Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken)
                || string.IsNullOrWhiteSpace(refreshToken))
            {
                ClearAuthCookies(context);
                return Results.Unauthorized();
            }

            var ip = context.Connection.RemoteIpAddress?.ToString();
            var result = await rotationService.RotateAsync(refreshToken, ip, ct);
            AppendAuthCookies(context, result);
            return Results.Ok(ToAuthUserResponse(result.User));
        }).WithTags("Auth");

        group.MapPost("/logout", (HttpContext context) =>
        {
            ClearAuthCookies(context);
            return Results.NoContent();
        }).WithTags("Auth");

        group.MapPost("/verify-email/request", async (VerificationRequest request, IIdentityService identityService, CancellationToken ct) =>
        {
            await identityService.RequestEmailVerificationAsync(request.Email, ct);
            return Results.Ok(new { message = "If the account exists, a verification link was sent." });
        }).WithTags("Auth");

        group.MapPost("/verify-email/confirm", async (VerificationConfirmRequest request, IIdentityService identityService, CancellationToken ct) =>
        {
            var ok = await identityService.ConfirmEmailVerificationAsync(request.Token, ct);
            return ok ? Results.Ok(new { message = "Email verified." }) : Results.BadRequest(new { message = "Invalid or expired token." });
        }).WithTags("Auth");

        group.MapPost("/password-reset/request", async (HttpContext context, PasswordResetRequest request, IIdentityService identityService, CancellationToken ct) =>
        {
            context.Request.Headers["X-Auth-Identifier"] = request.Email;
            await identityService.RequestPasswordResetAsync(request.Email, ct);
            return Results.Ok(new { message = "If the account exists, a reset link was sent." });
        }).WithTags("Auth");

        group.MapPost("/password-reset/confirm", async (PasswordResetConfirmRequest request, IIdentityService identityService, CancellationToken ct) =>
        {
            var ok = await identityService.ConfirmPasswordResetAsync(request.Token, request.NewPassword, ct);
            return ok ? Results.Ok(new { message = "Password reset completed." }) : Results.BadRequest(new { message = "Invalid, consumed, or expired token." });
        }).WithTags("Auth");

        group.MapGet("/me", async (ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken ct) =>
        {
            var subject = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(subject, out var userId) || userId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            var user = await dbContext.Users
                .AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => new AuthMeResponse(
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Role.ToAuthorizationRole(),
                    user.EmailVerified,
                    user.CreatedAtUtc))
                .SingleOrDefaultAsync(ct);

            return user is null ? Results.Unauthorized() : Results.Ok(user);
        })
        .WithTags("Auth")
        .RequireAuthorization();

        group.MapPost("/bootstrap-admin", async (
            BootstrapAdminRequest request,
            IConfiguration configuration,
            AppDbContext dbContext,
            CancellationToken ct) =>
        {
            var configuredSecret = configuration["AdminBootstrap:Secret"];
            if (string.IsNullOrWhiteSpace(configuredSecret)
                || !string.Equals(request.Secret, configuredSecret, StringComparison.Ordinal))
            {
                return Results.NotFound();
            }

            var normalizedEmail = UserAccount.NormalizeEmail(request.Email);
            var user = await dbContext.Users.SingleOrDefaultAsync(user => user.Email == normalizedEmail, ct);
            if (user is null)
            {
                return Results.NotFound(new { message = "User not found." });
            }

            user.SetRole(UserRole.Admin);
            user.SetEmailVerified(true);
            await dbContext.SaveChangesAsync(ct);

            return Results.Ok(new
            {
                user.Id,
                user.Email,
                role = user.Role.ToAuthorizationRole(),
                user.EmailVerified
            });
        })
        .WithTags("Auth")
        .AllowAnonymous();

        group.MapGet("/admin/probe", () => Results.Ok(new { ok = true }))
            .WithTags("Health/Probes")
            .RequireAuthorization(AuthPolicies.AdminOnly);

        group.MapGet("/verified/probe", () => Results.Ok(new { ok = true }))
            .WithTags("Health/Probes")
            .RequireAuthorization(AuthPolicies.VerifiedForSensitiveActions);

        return group;
    }

    private static AuthUserResponse ToAuthUserResponse(AuthenticatedUserResult user)
    {
        return new AuthUserResponse(
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.EmailVerified);
    }

    private static void AppendAuthCookies(HttpContext context, LoginResult result)
    {
        context.Response.Cookies.Append(
            AccessCookieName,
            result.AccessToken,
            BuildCookieOptions(context, result.AccessTokenExpiresAtUtc));

        context.Response.Cookies.Append(
            RefreshCookieName,
            result.RefreshToken,
            BuildCookieOptions(context, result.RefreshTokenExpiresAtUtc));
    }

    private static void ClearAuthCookies(HttpContext context)
    {
        var options = BuildCookieOptions(context, DateTimeOffset.UnixEpoch);
        context.Response.Cookies.Delete(AccessCookieName, options);
        context.Response.Cookies.Delete(RefreshCookieName, options);
    }

    private static CookieOptions BuildCookieOptions(HttpContext context, DateTimeOffset expiresAtUtc)
    {
        var sameSite = IsCrossSiteRequest(context)
            ? SameSiteMode.None
            : SameSiteMode.Lax;

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps || sameSite == SameSiteMode.None,
            SameSite = sameSite,
            Expires = expiresAtUtc,
            Path = "/"
        };
    }

    private static bool IsCrossSiteRequest(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Origin", out var originValues)
            || !Uri.TryCreate(originValues.FirstOrDefault(), UriKind.Absolute, out var origin)
            || !context.Request.Host.HasValue)
        {
            return false;
        }

        return !string.Equals(origin.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record BootstrapAdminRequest(string Email, string Secret);
