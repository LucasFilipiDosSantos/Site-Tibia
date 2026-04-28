using Application.Identity.Contracts;
using Application.Identity.Services;
using Domain.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Auth;

public static class AuthEndpoints
{
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
            return Results.Ok(ToAuthResponse(result));
        }).WithTags("Auth");

        group.MapPost("/refresh", async (HttpContext context, RefreshRequest request, TokenRotationService rotationService, CancellationToken ct) =>
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var result = await rotationService.RotateAsync(request.RefreshToken, ip, ct);
            return Results.Ok(ToAuthResponse(result));
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
            UserRole? databaseRole = null;

            if (Guid.TryParse(subject, out var userId))
            {
                databaseRole = await dbContext.Users
                    .AsNoTracking()
                    .Where(user => user.Id == userId)
                    .Select(user => (UserRole?)user.Role)
                    .SingleOrDefaultAsync(ct);
            }

            var tokenRoles = principal.Claims
                .Where(claim => claim.Type == "role" || claim.Type == ClaimTypes.Role)
                .Select(claim => new { claim.Type, claim.Value })
                .ToArray();

            return Results.Ok(new
            {
                subject,
                email = principal.FindFirstValue("email") ?? principal.FindFirstValue(ClaimTypes.Email),
                tokenRoles,
                databaseRole = databaseRole?.ToAuthorizationRole(),
                isAdmin = databaseRole == UserRole.Admin || AuthPolicies.HasAdminRoleClaim(principal)
            });
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

    private static AuthResponse ToAuthResponse(LoginResult result)
    {
        return new AuthResponse(
            result.AccessToken,
            result.RefreshToken,
            result.AccessTokenExpiresAtUtc,
            result.RefreshTokenExpiresAtUtc,
            new AuthUserResponse(
                result.User.Id,
                result.User.Name,
                result.User.Email,
                result.User.Role,
                result.User.EmailVerified));
    }
}

public sealed record BootstrapAdminRequest(string Email, string Secret);
