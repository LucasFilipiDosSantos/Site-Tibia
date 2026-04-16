---
phase: 01-identity-security-foundation
plan: 08
subsystem: auth
tags: [smtp, token-delivery, exception-handling, integration-tests]
requires:
  - phase: 01-07
    provides: External SMTP delivery adapter and token round-trip test baseline
provides:
  - Startup SMTP guardrails that reject placeholder hosts/credentials
  - Application-level token delivery failure abstraction mapped to generic request contracts
  - Regression coverage for healthy and faulted token-delivery paths with one-time confirm semantics
affects: [auth-02, phase-07-async-retries, operational-auditability]
tech-stack:
  added: []
  patterns: [options-validator fail-fast startup, safe exception-to-generic-response mapping, transport-fault integration parity testing]
key-files:
  created:
    - src/Infrastructure/Identity/Options/IdentityTokenDeliveryOptionsValidator.cs
    - src/Application/Identity/Exceptions/TokenDeliveryUnavailableException.cs
  modified:
    - src/Infrastructure/DependencyInjection.cs
    - src/Application/Identity/Services/IdentityService.cs
    - src/Application/Identity/Services/SecurityAuditService.cs
    - src/API/ErrorHandling/GlobalExceptionHandler.cs
    - tests/IntegrationTests/Identity/ExternalTokenDeliveryRoundTripTests.cs
    - tests/UnitTests/Identity/VerificationAndPasswordResetRoundTripTests.cs
key-decisions:
  - "Treat placeholder SMTP hosts and credentials as invalid runtime config when Provider=smtp."
  - "Convert transport-layer dispatch failures to TokenDeliveryUnavailableException and map request endpoints to generic 200 responses."
  - "Record dedicated dispatch-failed audit events while keeping token values out of audit payloads."
patterns-established:
  - "Identity request endpoints keep anti-enumeration generic success contracts even on SMTP transport failure."
  - "Integration hosts override IdentityTokenDelivery config explicitly to remain deterministic under startup validators."
requirements-completed: [AUTH-02]
duration: 8 min
completed: 2026-04-16
---

# Phase 01 Plan 08: SMTP Failure Guardrails and Safe Request Contracts Summary

**SMTP-backed verification/password-reset request flows now fail safely: runtime blocks placeholder SMTP config at startup, and transport outages return generic success responses without leaking internals while preserving one-time token semantics.**

## Performance

- **Duration:** 8 min
- **Started:** 2026-04-16T14:02:52Z
- **Completed:** 2026-04-16T14:11:38Z
- **Tasks:** 3
- **Files modified:** 8

## Accomplishments
- Added a reusable `IdentityTokenDeliveryOptionsValidator` and wired it through DI `ValidateOnStart` to reject placeholder SMTP host/credential configurations.
- Introduced `TokenDeliveryUnavailableException`, added dispatch failure audit events, and mapped delivery faults to endpoint-specific generic success contracts in `GlobalExceptionHandler`.
- Expanded integration and unit coverage for healthy and faulted delivery paths, including generic response parity and replay/one-time confirm invariants.

## Task Commits

Each task was committed atomically:

1. **Task 1: Add SMTP runtime guardrails for placeholder/invalid provider config** - `c0f4fc0` (feat)
2. **Task 2: Map token-delivery transport failures to safe generic request contracts** - `f8a7836` (feat)
3. **Task 3: Expand integration coverage for healthy + failure delivery paths** - `23e5225` (test)

## Files Created/Modified
- `src/Infrastructure/Identity/Options/IdentityTokenDeliveryOptionsValidator.cs` - validates Provider=smtp configuration and rejects placeholders.
- `src/Infrastructure/DependencyInjection.cs` - registers the SMTP options validator for startup fail-fast behavior.
- `src/Application/Identity/Exceptions/TokenDeliveryUnavailableException.cs` - application-layer abstraction for delivery unavailability.
- `src/Application/Identity/Services/IdentityService.cs` - wraps delivery faults, records audit events, and throws application exception.
- `src/Application/Identity/Services/SecurityAuditService.cs` - adds explicit dispatch-failed event constants.
- `src/API/ErrorHandling/GlobalExceptionHandler.cs` - maps delivery-unavailable failures to generic 200 responses for request endpoints.
- `tests/IntegrationTests/Identity/ExternalTokenDeliveryRoundTripTests.cs` - adds validator, failure-path generic parity, and audit assertions.
- `tests/UnitTests/Identity/VerificationAndPasswordResetRoundTripTests.cs` - stabilizes host configuration under new startup validation.

## Decisions Made
- Use a dedicated options validator class instead of inline `.Validate(...)` lambdas to keep SMTP guardrail rules reusable and deterministic.
- Keep failure mapping at the global handler boundary so endpoint contracts remain enumeration-safe without duplicating logic in endpoint handlers.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Test hosts failed startup after placeholder SMTP guardrails became active**
- **Found during:** Task 1 and Task 3 verification
- **Issue:** Existing integration/unit WebApplicationFactory hosts inherited `appsettings.Development.json` placeholder SMTP host (`smtp.dev.local`), triggering startup `OptionsValidationException` and preventing test execution.
- **Fix:** Added explicit in-memory IdentityTokenDelivery configuration overrides in test hosts to use concrete non-placeholder SMTP settings (or `inmemory` where appropriate).
- **Files modified:** `tests/IntegrationTests/Identity/ExternalTokenDeliveryRoundTripTests.cs`, `tests/UnitTests/Identity/VerificationAndPasswordResetRoundTripTests.cs`
- **Verification:** Task and plan test suites passed after overrides.
- **Committed in:** `c0f4fc0`, `23e5225`

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Fix was required to preserve deterministic test execution under the new fail-fast security guardrails; no scope creep.

## Issues Encountered
- Plan verification filter for `AdminJwtAuthorizationTests` matched zero tests in this repository state. Other required verification suites executed successfully.

## Authentication Gates
None.

## User Setup Required
For runtime/UAT SMTP delivery, environment overrides are required (non-placeholder host and credentials) before running with `IdentityTokenDelivery:Provider=smtp`.

## Next Phase Readiness
- UAT blockers for verify-email and password-reset request transport faults are closed with safe generic contracts.
- SMTP provider mode now enforces fail-fast runtime guardrails against placeholder configuration.
- Healthy request→token capture→single confirm→replay rejection behavior remains covered.

## Self-Check: PASSED

- FOUND: `.planning/phases/01-identity-security-foundation/01-08-SUMMARY.md`
- FOUND commit: `c0f4fc0`
- FOUND commit: `f8a7836`
- FOUND commit: `23e5225`
