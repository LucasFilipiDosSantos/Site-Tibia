# Plan 01-01 Summary

## Completed

- Added Domain identity models and policies in `src/Domain/Identity/`:
  - `SecurityPolicy` constants for access lifetime (15m), refresh lifetime (30d), password min length (10), lockout duration (15m)
  - `UserAccount` with failed-login tracking, lockout behavior, email normalization
  - `RefreshSession` with revoke/rotation lineage metadata
  - `SecurityToken` with expiry + one-time consumption support
- Added Application identity contracts in `src/Application/Identity/Contracts/`:
  - `IIdentityService`, `ITokenService`, `IUserRepository`, `IRefreshSessionRepository`, `ISecurityTokenRepository`, `IPasswordHasherService`, `ISystemClock`
- Implemented Application services in `src/Application/Identity/Services/`:
  - `IdentityService` (registration, login, verification/reset tokenized flows, lockout policy)
  - `TokenRotationService` (refresh rotation + replay rejection)
  - `SecurityAuditService` (required event names and in-memory capture)
- Added initial identity unit tests:
  - `tests/UnitTests/Identity/PasswordPolicyTests.cs`
  - `tests/UnitTests/Identity/LockoutPolicyTests.cs`
  - `tests/UnitTests/Identity/TokenRotationTests.cs`
  - test doubles under `tests/UnitTests/Identity/TestDoubles.cs`

## Verification

- `dotnet build backend.slnx` passed
- `dotnet test tests/UnitTests/UnitTests.csproj -v minimal --filter "FullyQualifiedName~Identity"` passed

## Notes

- Work includes contract/service groundwork that later plans consume.
- Security constants and invariants are now codified in executable tests.
