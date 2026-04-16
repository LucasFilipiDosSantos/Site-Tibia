# Plan 01-03 Summary

## Completed

- Added auth API contracts and endpoint mappings:
  - `src/API/Auth/AuthDtos.cs`
  - `src/API/Auth/AuthEndpoints.cs`
- Added authorization + HTTPS hardening:
  - `src/API/Auth/AuthPolicies.cs`
  - `src/API/Auth/HttpsSecurityExtensions.cs`
  - wired in `src/API/Program.cs`
- Added auth throttling middleware with combined user+IP keying:
  - `src/API/Auth/AuthRateLimitMiddleware.cs`
- Wired end-to-end app services and infrastructure in API startup:
  - `src/API/Program.cs`
  - `src/API/API.csproj`
- Added API/security behavior tests:
  - `tests/UnitTests/Identity/AuthEndpointContractTests.cs`
  - `tests/UnitTests/Identity/AuthorizationPolicyTests.cs`
  - `tests/UnitTests/Identity/ThrottlingAndLockoutTests.cs`
  - helper `tests/UnitTests/Identity/TestPrincipals.cs`

## Verification

- `dotnet test tests/UnitTests/UnitTests.csproj -v minimal --filter "FullyQualifiedName~AuthEndpointContract|FullyQualifiedName~AuthorizationPolicy|FullyQualifiedName~ThrottlingAndLockout"` passed
- `dotnet test backend.slnx -v minimal` passed

## Notes

- `/auth` now maps the 7 required endpoints:
  - `POST /register`
  - `POST /login`
  - `POST /refresh`
  - `POST /verify-email/request`
  - `POST /verify-email/confirm`
  - `POST /password-reset/request`
  - `POST /password-reset/confirm`
- Policies are explicit and claim-based:
  - `AdminOnly` requires `role=Admin`
  - `VerifiedForSensitiveActions` requires `email_verified=true`
