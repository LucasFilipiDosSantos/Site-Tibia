---
status: complete
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
  artifacts: []
  missing: []
- truth: "Password reset request returns a generic success response, token is delivered through the configured provider, confirm updates password once, and replaying the same token is rejected."
  status: failed
  reason: "User reported: got this error: SMTP dispatch failed for recipient teste@email.com with subject Reset your password; System.Net.Mail.SmtpException: Failure sending mail; SocketException (11): Resource temporarily unavailable; GlobalExceptionHandler logged unhandled exception while processing /auth/password-reset/request"
  severity: blocker
  test: 5
  artifacts: []
  missing: []
