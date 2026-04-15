---
status: diagnosed
phase: 01-identity-security-foundation
source:
  - 01-01-SUMMARY.md
  - 01-02-SUMMARY.md
  - 01-03-SUMMARY.md
  - 01-04-SUMMARY.md
  - 01-05-SUMMARY.md
started: 2026-04-15T10:21:17Z
updated: 2026-04-15T10:44:53Z
---

## Current Test

[testing complete]

## Tests

### 1. Register with password policy
expected: Submitting registration with a weak password is rejected with a validation error, and submitting with a strong password succeeds with an accepted response.
result: issue
reported: "it does return the error for the password but not on a rest complaint way, it just explode an error, exception error handling in any of the layers"
severity: blocker

### 2. Login lockout after repeated failures
expected: Repeated invalid login attempts for the same account eventually return a lockout response, and valid credentials are denied during lockout window.
result: blocked
blocked_by: prior-phase
reason: "cannot be tested properly without the error handling expressed on the last test, this goes for the other tests"

### 3. Refresh token rotation and replay rejection
expected: A valid refresh succeeds once and returns a new refresh token; reusing the old refresh token is rejected.
result: blocked
blocked_by: prior-phase
reason: "cannot be tested properly without the error handling expressed on the last test, this goes for the other tests"

### 4. Email verification request and one-time confirm
expected: Verification request returns a generic success response, confirm with a valid token succeeds once, and replaying the same token is rejected.
result: blocked
blocked_by: prior-phase
reason: "cannot be tested properly without the error handling expressed on the last test, this goes for the other tests"

### 5. Password reset request and one-time confirm
expected: Password reset request returns a generic success response, confirm with valid token updates password once, and replaying token is rejected.
result: blocked
blocked_by: prior-phase
reason: "cannot be tested properly without the error handling expressed on the last test, this goes for the other tests"

### 6. Admin authorization boundaries
expected: Admin-protected endpoint returns success with a valid admin token, returns forbidden for authenticated non-admin token, and unauthorized for missing or invalid token.
result: blocked
blocked_by: prior-phase
reason: "cannot be tested properly without the error handling expressed on the last test, this goes for the other tests"

## Summary

total: 6
passed: 0
issues: 1
pending: 0
skipped: 0
blocked: 5

## Gaps

- truth: "Submitting registration with a weak password is rejected with a validation error, and submitting with a strong password succeeds with an accepted response."
  status: failed
  reason: "User reported: it does return the error for the password but not on a rest complaint way, it just explode an error, exception error handling in any of the layers"
  severity: blocker
  test: 1
  root_cause: "Weak-password registration throws ArgumentException in the application service, and the API layer has no exception-to-REST mapping, so the expected validation failure bubbles as an unhandled exception instead of a structured 4xx response."
  artifacts:
    - path: "src/Application/Identity/Services/IdentityService.cs"
      issue: "RegisterAsync throws ArgumentException for weak password instead of returning a mapped validation outcome."
    - path: "src/API/Auth/AuthEndpoints.cs"
      issue: "Register endpoint does not translate expected validation exceptions to REST-compliant responses."
    - path: "src/API/Program.cs"
      issue: "No centralized exception middleware maps domain/application exceptions to ProblemDetails."
    - path: "tests/UnitTests/Identity/AuthEndpointContractTests.cs"
      issue: "No contract test verifies weak-password registration error status/body semantics."
  missing:
    - "Map expected registration validation failures to a structured 4xx response (e.g., ProblemDetails)."
    - "Add centralized exception handling or endpoint-level translation for expected business validation exceptions."
    - "Add endpoint/integration tests for weak-password response status and payload contract."
  debug_session: ".planning/debug/registration-weak-password-error.md"
