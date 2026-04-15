---
phase: 01-identity-security-foundation
plan: 07
subsystem: auth
tags: [smtp, identity, token-delivery, integration-tests]
requires:
  - phase: 01-04
    provides: In-process token delivery contract and verify/reset round-trip baseline
  - phase: 01-06
    provides: deterministic API validation/error contracts used by integration host
provides:
  - Provider-backed SMTP token delivery for verification/password-reset flows
  - Runtime DI provider selection with fail-fast SMTP config validation
  - Integration proof that externally delivered tokens are confirmed exactly once
affects: [phase-01-verification, auth-02, reliability-phase]
tech-stack:
  added: [Microsoft.Extensions.Options.ConfigurationExtensions, System.Net.Mail]
  patterns: [provider-based delivery adapter, options-validated fail-fast startup, external-capture integration testing]
key-files:
  created:
    - src/Infrastructure/Identity/Options/IdentityTokenDeliveryOptions.cs
    - src/Infrastructure/Identity/Services/SmtpIdentityTokenDelivery.cs
    - src/Infrastructure/Identity/Services/SmtpClientTokenTransport.cs
    - tests/IntegrationTests/Identity/ExternalTokenDeliveryRoundTripTests.cs
  modified:
    - src/Infrastructure/DependencyInjection.cs
    - src/API/appsettings.json
    - src/API/appsettings.Development.json
key-decisions:
  - "Token delivery provider selection is runtime-configured (`smtp` or `inmemory`) with `smtp` as non-test default."
  - "SMTP adapter logs only audit-safe metadata (user id and expiry) while raw tokens exist only in outbound message body."
patterns-established:
  - "Identity delivery adapters validate options at construction and fail startup early via ValidateOnStart."
  - "Integration tests prove token round-trip via provider capture instead of reading tokens from HTTP responses."
requirements-completed: [AUTH-02]
duration: 10 min
completed: 2026-04-15
---

# Phase 01 Plan 07: External SMTP Token Delivery Summary

**SMTP-backed verification and password-reset token dispatch now runs through an out-of-process provider path with one-time confirm semantics proven end-to-end.**

## Performance

- **Duration:** 10 min
- **Started:** 2026-04-15T12:22:23Z
- **Completed:** 2026-04-15T12:33:20Z
- **Tasks:** 3
- **Files modified:** 8

## Accomplishments
- Introduced strongly typed `IdentityTokenDelivery` options and implemented `SmtpIdentityTokenDelivery` for verification/reset token dispatch.
- Rewired infrastructure DI to bind provider options, choose runtime implementation, and fail startup deterministically on invalid SMTP config.
- Added integration tests that drive request→provider capture→confirm flows and prove replay rejection for verify/reset tokens.

## Task Commits

Each task was committed atomically:

1. **Task 1: Introduce provider-backed token delivery adapter and options contract**
   - `7dea188` (test)
   - `7dea0df` (feat)
2. **Task 2: Wire non-test runtime to provider delivery with fail-fast configuration**
   - `a808f6a` (feat)
3. **Task 3: Prove external-delivery round-trip and one-time confirm semantics**
   - `63f69df` (test)

Additional corrective commit within plan scope:
- `5793bb2` (fix) — implemented concrete SMTP client transport dispatch required for production-capable provider behavior.

## Files Created/Modified
- `src/Infrastructure/Identity/Options/IdentityTokenDeliveryOptions.cs` - typed provider and SMTP configuration contract.
- `src/Infrastructure/Identity/Services/SmtpIdentityTokenDelivery.cs` - delivery adapter implementing `IIdentityTokenDelivery`.
- `src/Infrastructure/Identity/Services/SmtpClientTokenTransport.cs` - concrete SMTP sender for out-of-process dispatch.
- `src/Infrastructure/DependencyInjection.cs` - provider selection + options binding + startup validation.
- `src/API/appsettings.json` - production/default IdentityTokenDelivery config template.
- `src/API/appsettings.Development.json` - development IdentityTokenDelivery config template.
- `tests/IntegrationTests/Identity/ExternalTokenDeliveryRoundTripTests.cs` - adapter and API round-trip integration coverage.

## Decisions Made
- Use provider-dispatch abstraction (`ISmtpTokenTransport`) so integration tests can capture outbound messages without exposing tokens through API responses.
- Keep in-memory delivery registered only as explicit provider option for test hosts, not default runtime path.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added options binding package for infrastructure compile**
- **Found during:** Task 2
- **Issue:** `OptionsBuilder.Bind(...)` extension was unavailable, blocking DI configuration binding.
- **Fix:** Added `Microsoft.Extensions.Options.ConfigurationExtensions` to Infrastructure project dependencies.
- **Files modified:** `src/Infrastructure/Infrastructure.csproj`
- **Verification:** `dotnet build backend.slnx` completed successfully.
- **Committed in:** `a808f6a`

**2. [Rule 2 - Missing Critical] Replaced placeholder SMTP transport with real out-of-process dispatch**
- **Found during:** Task 3 verification review
- **Issue:** Placeholder transport returned completed task and did not actually send outbound messages, violating production-capable delivery requirement.
- **Fix:** Implemented `SmtpClientTokenTransport` with `System.Net.Mail` send logic and error logging.
- **Files modified:** `src/Infrastructure/Identity/Services/SmtpClientTokenTransport.cs`
- **Verification:** Full plan verification suite passed after transport implementation.
- **Committed in:** `5793bb2`

---

**Total deviations:** 2 auto-fixed (1 blocking, 1 missing critical)
**Impact on plan:** Both fixes were necessary to satisfy AUTH-02 correctness and startup reliability without scope creep.

## Issues Encountered
None

## Authentication Gates
None

## User Setup Required
None - no additional manual setup beyond existing appsettings/env secret overrides.

## Next Phase Readiness
- AUTH-02 is now implementable with out-of-process token delivery and external-capture regression proof.
- Ready for phase re-verification to close the remaining verification gap.

## Self-Check: PASSED
