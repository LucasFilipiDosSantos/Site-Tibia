using Application.Identity.Contracts;
using Application.Identity.Services;

namespace API.Auth;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", async (RegisterRequest request, IIdentityService identityService, CancellationToken ct) =>
        {
            await identityService.RegisterAsync(new RegisterCommand(request.Email, request.Password), ct);
            return Results.Ok(new { message = "Registration successful." });
        }).WithTags("Auth");

        group.MapPost("/login", async (HttpContext context, LoginRequest request, IIdentityService identityService, CancellationToken ct) =>
        {
            context.Request.Headers["X-Auth-Identifier"] = request.Email;
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var result = await identityService.LoginAsync(new LoginCommand(request.Email, request.Password, ip), ct);
            return Results.Ok(new AuthResponse(result.AccessToken, result.RefreshToken, result.AccessTokenExpiresAtUtc, result.RefreshTokenExpiresAtUtc));
        }).WithTags("Auth");

        group.MapPost("/refresh", async (HttpContext context, RefreshRequest request, TokenRotationService rotationService, CancellationToken ct) =>
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var result = await rotationService.RotateAsync(request.RefreshToken, ip, ct);
            return Results.Ok(new AuthResponse(result.AccessToken, result.RefreshToken, result.AccessTokenExpiresAtUtc, result.RefreshTokenExpiresAtUtc));
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

        group.MapGet("/admin/probe", () => Results.Ok(new { ok = true }))
            .WithTags("Health/Probes")
            .RequireAuthorization(AuthPolicies.AdminOnly);

        group.MapGet("/verified/probe", () => Results.Ok(new { ok = true }))
            .WithTags("Health/Probes")
            .RequireAuthorization(AuthPolicies.VerifiedForSensitiveActions);

        return group;
    }
}
