# Plan 01-02 Summary

## Completed

- Added EF identity persistence root and mappings:
  - `src/Infrastructure/Persistence/AppDbContext.cs`
  - `src/Infrastructure/Persistence/Configurations/UserAccountConfiguration.cs`
  - `src/Infrastructure/Persistence/Configurations/RefreshSessionConfiguration.cs`
  - `src/Infrastructure/Persistence/Configurations/SecurityTokenConfiguration.cs`
- Added infrastructure repositories:
  - `src/Infrastructure/Identity/Repositories/UserRepository.cs`
  - `src/Infrastructure/Identity/Repositories/RefreshSessionRepository.cs`
  - `src/Infrastructure/Identity/Repositories/SecurityTokenRepository.cs`
- Added security adapters:
  - `src/Infrastructure/Identity/Services/JwtTokenService.cs`
  - `src/Infrastructure/Identity/Services/PasswordHasherService.cs`
  - `src/Infrastructure/Identity/Services/SystemClock.cs`
- Added dependency wiring:
  - `src/Infrastructure/DependencyInjection.cs`
- Updated project references/package dependencies:
  - `src/Application/Application.csproj`
  - `src/Infrastructure/Infrastructure.csproj`
  - `tests/UnitTests/UnitTests.csproj`
- Added JWT and expiry tests:
  - `tests/UnitTests/Identity/JwtTokenServiceTests.cs`
  - `tests/UnitTests/Identity/SecurityTokenExpiryTests.cs`

## Verification

- `dotnet build backend.slnx` passed
- `dotnet test tests/UnitTests/UnitTests.csproj -v minimal --filter "FullyQualifiedName~JwtTokenService|FullyQualifiedName~SecurityTokenExpiry"` passed

## Notes

- JWT claims now include `sub`, `email`, `role`, and `email_verified`.
- Refresh/session and security token persistence has unique token-hash indexing and one-time-consume support.
