---
phase: 01-identity-security-foundation
plan: 06
subsystem: auth
tags: [problem-details, exception-handling, integration-tests, registration]
requires:
  - phase: 01-05
    provides: JWT/auth pipeline baseline and identity verification context used for regression safety
provides:
  - Centralized exception-to-ProblemDetails mapping for auth validation/business exceptions
  - Registration HTTP contract coverage for weak-password 400 problem+json and strong-password success
  - Focused Phase 01 auth regression classification in integration suite
affects: [phase-01-verification, auth-uat, identity-error-contracts]
tech-stack:
  added: [Microsoft.AspNetCore.Diagnostics, integration test host usage in dedicated IntegrationTests project]
  patterns: [global IExceptionHandler mapping, RFC7807 error contracts, WebApplicationFactory endpoint contract tests]
key-files:
  created:
    - src/API/ErrorHandling/GlobalExceptionHandler.cs
    - tests/IntegrationTests/IntegrationTests.csproj
    - tests/IntegrationTests/Identity/RegisterValidationErrorContractTests.cs
    - tests/IntegrationTests/Identity/TestDoubles.cs
  modified:
    - src/API/Program.cs
key-decisions:
  - "Map expected auth/business exceptions centrally in API pipeline to deterministic ProblemDetails instead of endpoint-local try/catch."
  - "Keep registration error semantics covered by dedicated IntegrationTests contracts, preserving UnitTests isolation guardrail in AGENTS.md."
patterns-established:
  - "Expected registration validation failures are represented as 4xx RFC7807 payloads and never as unhandled exception pages."
  - "Phase-critical auth regression checks are encoded via test traits and run as focused suites."
requirements-completed: [AUTH-01]
duration: 10 min
completed: 2026-04-15
---

# Phase 1 Plan 06: Registration Validation Error Contract Summary

**Registration now returns deterministic RFC7807 problem+json for weak-password validation failures while preserving successful strong-password behavior through integration contract coverage.**

## Performance

- **Duration:** 10 min
- **Started:** 2026-04-15T11:23:03Z
- **Completed:** 2026-04-15T11:33:04Z
- **Tasks:** 3
- **Files modified:** 5

## Accomplishments
- Added centralized API exception handling with ProblemDetails mapping and environment-safe error payload behavior.
- Created dedicated `tests/IntegrationTests` project and implemented registration weak/strong contract tests against real endpoint pipeline.
- Executed focused integration + identity regression suites to confirm behavior fix without auth-core regressions.

## Task Commits

Each task was committed atomically (Task 2 used TDD RED/GREEN/REFACTOR):

1. **Task 1: Add centralized exception mapping for REST-compliant auth validation failures**
   - `55b7a83` (feat)
2. **Task 2: Add registration HTTP contract tests in IntegrationTests scope**
   - `17930df` (test) RED
   - `c692656` (feat) GREEN
   - `70d97d8` (refactor)
3. **Task 3: Run focused phase-01 auth regression suite after exception handling changes**
   - `2c02128` (chore)

## Verification Evidence

- `dotnet test tests/IntegrationTests/IntegrationTests.csproj -v minimal --filter "FullyQualifiedName~RegisterValidationErrorContractTests"` → **Passed (3/3)**
- `dotnet test tests/IntegrationTests/IntegrationTests.csproj -v minimal` → **Passed (3/3)**
- `dotnet test tests/UnitTests/UnitTests.csproj -v minimal --filter "FullyQualifiedName~PasswordPolicyTests|FullyQualifiedName~TokenRotationTests|FullyQualifiedName~LockoutPolicyTests"` → **Passed (8/8)**

## Files Created/Modified
- `src/API/ErrorHandling/GlobalExceptionHandler.cs` - Centralized exception mapping to RFC7807 ProblemDetails with safe 4xx/5xx contracts.
- `src/API/Program.cs` - Registers `AddProblemDetails`, `AddExceptionHandler<GlobalExceptionHandler>`, and `UseExceptionHandler`.
- `tests/IntegrationTests/IntegrationTests.csproj` - Dedicated integration test project aligned with AGENTS.md test boundary guardrail.
- `tests/IntegrationTests/Identity/RegisterValidationErrorContractTests.cs` - Weak-password 400 problem+json contract, strong-password success, and stack-trace non-leak tests.
- `tests/IntegrationTests/Identity/TestDoubles.cs` - In-memory integration test doubles for endpoint-hosted identity contract testing.

## Decisions Made
- Centralized exception handling was preferred over endpoint-scoped translation to keep failure semantics consistent and composable across auth routes.
- Integration tests were separated from `tests/UnitTests` to comply with architecture boundaries and keep UnitTests isolated from full HTTP pipeline behavior.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added IntegrationTests-local identity test doubles to unblock project compilation**
- **Found during:** Task 2 (TDD RED)
- **Issue:** New integration contract tests initially failed to compile because existing in-memory doubles were internal to UnitTests namespace/project scope.
- **Fix:** Added equivalent IntegrationTests-local doubles for repositories, token service, password hasher, clock, and token delivery.
- **Files modified:** `tests/IntegrationTests/Identity/TestDoubles.cs`
- **Verification:** `dotnet test tests/IntegrationTests/IntegrationTests.csproj -v minimal --filter "FullyQualifiedName~RegisterValidationErrorContractTests"` passed.
- **Committed in:** `17930df`

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Deviation was necessary to make the dedicated IntegrationTests project executable without violating UnitTests boundaries.

## Authentication Gates

None.

## Issues Encountered

- `dotnet` emitted pre-existing `MSB4011` duplicate import warnings for Infrastructure generated props/targets; tests and builds still passed and no task-blocking impact occurred.

## Known Stubs

None.

## Next Phase Readiness

- AUTH-01 weak-password UAT blocker is closed with deterministic 400 ProblemDetails semantics and non-leaking error payloads.
- Registration success path remains stable and verified; phase is ready for final validation/closure flow.
## Self-Check: PASSED

- FOUND: src/API/ErrorHandling/GlobalExceptionHandler.cs
- FOUND: tests/IntegrationTests/IntegrationTests.csproj
- FOUND: tests/IntegrationTests/Identity/RegisterValidationErrorContractTests.cs
- FOUND: tests/IntegrationTests/Identity/TestDoubles.cs
- FOUND: .planning/phases/01-identity-security-foundation/01-06-SUMMARY.md
- FOUND commit: 55b7a83
- FOUND commit: 17930df
- FOUND commit: c692656
- FOUND commit: 70d97d8
- FOUND commit: 2c02128
