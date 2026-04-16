---
status: diagnosed
phase: 01-identity-security-foundation
source:
  - 01-01-SUMMARY.md
  - 01-02-SUMMARY.md
  - 01-03-SUMMARY.md
  - 01-04-SUMMARY.md
  - 01-05-SUMMARY.md
  - 01-06-SUMMARY.md
  - 01-07-SUMMARY.md
started: 2026-04-16T09:02:43Z
updated: 2026-04-16T12:44:50Z
---

## Current Test

[testing complete]

## Tests

### 1. Register with password policy and REST validation
expected: Submitting registration with a weak password returns a structured 400 ProblemDetails response (no stack trace), and submitting with a strong password returns a success response.
result: pass

### 2. Login lockout after repeated failures
expected: Repeated invalid login attempts for the same account eventually return a lockout response, and valid credentials are denied during lockout window.
result: pass

### 3. Refresh token rotation and replay rejection
expected: A valid refresh succeeds once and returns a new refresh token; reusing the old refresh token is rejected.
result: pass

### 4. Email verification token delivery and one-time confirm
expected: Verification request returns a generic success response, token is delivered through the configured provider, confirm with that token succeeds once, and replaying the same token is rejected.
result: issue
reported: "it pass but it returned this error info: SMTP dispatch failed for recipient teste@email.com with subject Verify your email; System.Net.Mail.SmtpException: Failure sending mail; SocketException (11): Resource temporarily unavailable; GlobalExceptionHandler logged unhandled exception while processing /auth/verify-email/request"
severity: blocker

### 5. Password reset token delivery and one-time confirm
expected: Password reset request returns a generic success response, token is delivered through the configured provider, confirm updates password once, and replaying the same token is rejected.
result: issue
reported: "got this error: SMTP dispatch failed for recipient teste@email.com with subject Reset your password; System.Net.Mail.SmtpException: Failure sending mail; SocketException (11): Resource temporarily unavailable; GlobalExceptionHandler logged unhandled exception while processing /auth/password-reset/request"
severity: blocker

### 6. Admin authorization boundaries
expected: Admin-protected endpoint returns success with a valid admin token, returns forbidden for authenticated non-admin token, and unauthorized for missing or invalid token.
result: pass

### 7. Verification/reset request enumeration safety
expected: Verification and password reset request endpoints return generic success for unknown emails and do not leak whether an account exists.
result: pass

## Summary

total: 7
passed: 5
issues: 2
pending: 0
skipped: 0
blocked: 0

## Gaps

- truth: "Verification request returns a generic success response, token is delivered through the configured provider, confirm with that token succeeds once, and replaying the same token is rejected."
  status: failed
  reason: "User reported: it pass but it returned this error info: SMTP dispatch failed for recipient teste@email.com with subject Verify your email; System.Net.Mail.SmtpException: Failure sending mail; SocketException (11): Resource temporarily unavailable; GlobalExceptionHandler logged unhandled exception while processing /auth/verify-email/request"
  severity: blocker
  test: 4
  root_cause: "Runtime delivery is configured to use SMTP (`Provider: smtp`) but configured hosts are placeholders/non-reachable in the UAT environment (`smtp.dev.local` / `smtp.example.com`), causing `SmtpClient.SendMailAsync` to fail and bubble as unhandled exception during verify-email request."
  artifacts:
    - path: "src/API/appsettings.Development.json"
      issue: "SMTP host points to `smtp.dev.local`, which is not resolvable/reachable in this runtime."
    - path: "src/API/appsettings.json"
      issue: "Base SMTP settings use placeholder host/credentials (`smtp.example.com`, `change-me`)."
    - path: "src/Infrastructure/Identity/Services/SmtpClientTokenTransport.cs"
      issue: "Transport logs and rethrows SMTP failures, so request pipeline receives exception."
    - path: "src/Application/Identity/Services/IdentityService.cs"
      issue: "RequestEmailVerificationAsync awaits token delivery directly and bubbles transport exceptions."
    - path: "src/API/ErrorHandling/GlobalExceptionHandler.cs"
      issue: "SMTP transport failures are not mapped to an expected generic response contract."
  missing:
    - "Provide valid, reachable SMTP endpoint for UAT/runtime or switch provider to `inmemory` for non-SMTP environments."
    - "Add startup/runtime guardrails that fail fast or clearly signal invalid SMTP host configuration for selected provider."
    - "Map token-delivery transport failures to safe, generic request responses for verify/reset request endpoints."
  debug_session: ".planning/debug/uat-test4-verify-smtp-fail.md"
- truth: "Password reset request returns a generic success response, token is delivered through the configured provider, confirm updates password once, and replaying the same token is rejected."
  status: failed
  reason: "User reported: got this error: SMTP dispatch failed for recipient teste@email.com with subject Reset your password; System.Net.Mail.SmtpException: Failure sending mail; SocketException (11): Resource temporarily unavailable; GlobalExceptionHandler logged unhandled exception while processing /auth/password-reset/request"
  severity: blocker
  test: 5
  root_cause: "Password reset request follows the same SMTP delivery path as verify-email; with SMTP provider selected and unreachable placeholder host configuration, send fails at socket level and exception propagates as unhandled in request pipeline."
  artifacts:
    - path: "src/API/Auth/AuthEndpoints.cs"
      issue: "Password-reset request endpoint invokes identity service flow that depends on SMTP delivery success path."
    - path: "src/Application/Identity/Services/IdentityService.cs"
      issue: "RequestPasswordResetAsync dispatches token through SMTP delivery and propagates transport errors."
    - path: "src/Infrastructure/Identity/Services/SmtpIdentityTokenDelivery.cs"
      issue: "Builds outbound reset message and delegates to SMTP transport for live dispatch."
    - path: "src/Infrastructure/Identity/Services/SmtpClientTokenTransport.cs"
      issue: "`SendMailAsync` socket failure is logged and rethrown."
    - path: "src/Infrastructure/DependencyInjection.cs"
      issue: "Provider resolution selects SMTP for runtime by configuration."
  missing:
    - "Use environment-appropriate provider configuration (`smtp` only when endpoint is reachable; otherwise `inmemory` in local/UAT)."
    - "Add end-to-end runtime validation for SMTP connectivity expectations before enabling SMTP-backed flow in UAT."
    - "Harden request endpoints so SMTP transport failures do not surface as unhandled exceptions."
  debug_session: ".planning/debug/pwd-reset-smtp-failure.md"
