---
phase: 01
slug: identity-security-foundation
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-14
---

# Phase 01 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit |
| **Config file** | `tests/UnitTests/UnitTests.csproj` |
| **Quick run command** | `dotnet test tests/UnitTests/UnitTests.csproj -v minimal --no-build` |
| **Full suite command** | `dotnet test backend.slnx -v minimal` |
| **Estimated runtime** | ~30-90 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/UnitTests/UnitTests.csproj -v minimal --no-build`
- **After every plan wave:** Run `dotnet test backend.slnx -v minimal`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 90 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 01-01-01 | 01 | 1 | AUTH-01, SEC-02 | T-01-01 | Password complexity + hashing policy enforced in domain/application contract paths | unit | `dotnet test tests/UnitTests/UnitTests.csproj -v minimal --filter "FullyQualifiedName~Password"` | ✅ | ⬜ pending |
| 01-01-02 | 01 | 1 | AUTH-01, AUTH-04 | T-01-02 | Access token issuance and refresh rotation logic behaves with immediate previous-token revoke | unit | `dotnet test tests/UnitTests/UnitTests.csproj -v minimal --filter "FullyQualifiedName~Refresh"` | ✅ | ⬜ pending |
| 01-02-01 | 02 | 2 | AUTH-02 | T-01-03 | Email verification + password reset tokens are one-time and expiry-checked (30 min reset) | unit | `dotnet test tests/UnitTests/UnitTests.csproj -v minimal --filter "FullyQualifiedName~Verification|FullyQualifiedName~Reset"` | ✅ | ⬜ pending |
| 01-02-02 | 02 | 2 | AUTH-01, AUTH-03, AUTH-04 | T-01-04 | Auth endpoints + policy gates reject unauthorized users deterministically | integration-lite | `dotnet test backend.slnx -v minimal --filter "FullyQualifiedName~Auth"` | ✅ | ⬜ pending |
| 01-03-01 | 03 | 3 | AUTH-03, SEC-01 | T-01-05 | Admin-only policy and verified-sensitive policy are enforced; HTTPS middleware active in production path | integration-lite | `dotnet test backend.slnx -v minimal --filter "FullyQualifiedName~Authorization|FullyQualifiedName~Https"` | ✅ | ⬜ pending |
| 01-03-02 | 03 | 3 | AUTH-02, AUTH-04, SEC-02 | T-01-06 | Throttle/lockout/audit and session controls are persisted and observable | unit/integration-lite | `dotnet test backend.slnx -v minimal --filter "FullyQualifiedName~Lockout|FullyQualifiedName~Audit|FullyQualifiedName~Session"` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/UnitTests/Identity/PasswordPolicyTests.cs` — password rule test scaffold
- [ ] `tests/UnitTests/Identity/RefreshTokenRotationTests.cs` — rotation/revocation tests
- [ ] `tests/UnitTests/Identity/VerificationAndResetTests.cs` — one-time/expiry token tests
- [ ] `tests/UnitTests/Identity/AuthorizationPolicyTests.cs` — Admin/verified policy behavior tests

---

## Manual-Only Verifications

All planned phase behaviors are automatable from CLI test commands. No manual-only verification required.

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 90s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
