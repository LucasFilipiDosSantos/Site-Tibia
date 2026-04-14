# Phase 01 Research — Identity & Security Foundation

**Phase:** 01 — Identity & Security Foundation  
**Date:** 2026-04-14  
**Requirements in scope:** AUTH-01, AUTH-02, AUTH-03, AUTH-04, SEC-01, SEC-02

## Standard Stack

- ASP.NET Core 10 Web API + `Microsoft.AspNetCore.Authentication.JwtBearer`
- JWT signing/validation via `Microsoft.IdentityModel.Tokens` + `System.IdentityModel.Tokens.Jwt`
- EF Core 10 + Npgsql provider for user/session/token persistence
- Password hashing via `Microsoft.AspNetCore.Identity.PasswordHasher<TUser>` (PBKDF2, salted, one-way) to satisfy SEC-02
- Structured logging via Serilog abstractions already selected at project level
- xUnit for unit tests in `tests/UnitTests`

## Architecture Patterns

- Keep Clean Architecture boundaries strict:
  - API: transport (controllers/endpoints, auth middleware, policies)
  - Application: use cases + ports (`IIdentityService`, `ITokenService`, `IUserRepository`, `IRefreshSessionRepository`)
  - Domain: entities/value objects and security rules (password policy, lockout rules, role semantics)
  - Infrastructure: EF persistence, password hasher adapter, JWT issuer adapter
- Use explicit auth flows with deterministic transitions:
  - Register → optional login before email verification (D-05)
  - Verification gate enforced on sensitive actions (checkout/admin) (D-04)
  - Refresh token rotation with immediate prior-token revocation (D-02, D-03)
- Model refresh tokens as session family records with one current active token hash and lineage metadata to support rotation/revocation checks.

## Don’t Hand-Roll

- Do not implement custom password hashing algorithms; use framework-provided PBKDF2 hasher adapter.
- Do not implement ad-hoc role checks inside handlers; use centralized authorization policies and claims mapping.
- Do not store raw refresh/reset/verification tokens; store hashed tokens + expiry + consumed timestamps.
- Do not implement unaudited mutable auth state; every lockout/session-revoke/reset/verification event must emit audit record (D-12).

## Common Pitfalls

1. **Refresh rotation race conditions**: accepting an already-rotated token twice.  
   Mitigation: transactionally revoke previous token and persist new token family state.
2. **Account enumeration leakage** on login/reset endpoints.  
   Mitigation: uniform response messages for unknown email/user cases.
3. **Lockout bypass through IP-only throttling.**  
   Mitigation: enforce dual-key throttling by `(user identifier, IP)` per D-10 and D-11.
4. **Admin policy drift** from manual endpoint checks.  
   Mitigation: policy-based `[Authorize(Policy = "AdminOnly")]` mapped to strict Admin claim (D-08).
5. **HTTPS disabled in non-dev deployments.**  
   Mitigation: enforce HTTPS redirection + HSTS in production path (SEC-01).

## Code Examples

```csharp
// Application port
public interface ITokenService
{
    AccessTokenResult IssueAccessToken(Guid userId, string email, string role, bool emailVerified);
    RefreshTokenResult IssueRefreshToken(Guid userId, Guid sessionFamilyId, DateTimeOffset nowUtc, DateTimeOffset expiresAtUtc);
    RefreshTokenResult RotateRefreshToken(string incomingToken, string ipAddress, DateTimeOffset nowUtc);
}
```

```csharp
// Policy gate example
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("role", "Admin"));

    options.AddPolicy("VerifiedForSensitiveActions", policy =>
        policy.RequireClaim("email_verified", "true"));
});
```

## Validation Architecture

- Unit tests cover domain/application auth logic (password complexity, token expiry checks, refresh rotation invariants, lockout thresholds).
- API behavior checks validate protected endpoint responses:
  - non-admin rejected on admin endpoints (AUTH-03)
  - unverified users blocked from checkout/admin-sensitive actions (D-04, D-05)
- Verification/reset tokens tested for one-time consumption + 30-minute expiry (D-06).
- Security baseline checks include:
  - HTTPS middleware configured in production path (SEC-01)
  - password hashes never equal raw password and verify successfully (SEC-02)

## Research Outcome

Prescriptive implementation for planning:

1. Build contract-first identity module across Domain/Application.
2. Implement infrastructure adapters for hashing/JWT/persistence.
3. Wire API endpoints and auth policies with strict claim-based checks.
4. Add deterministic tests for rotation, lockout, verification/reset expiry and access boundaries.

This phase can proceed without additional external research.
