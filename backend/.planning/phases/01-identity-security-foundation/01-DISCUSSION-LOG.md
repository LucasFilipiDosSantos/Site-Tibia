# Phase 1: Identity & Security Foundation - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md.

**Date:** 2026-04-14
**Phase:** 01-identity-security-foundation
**Areas discussed:** Token session policy, Email + reset flow, Admin access boundary, Account protection rules

---

## Token session policy

| Option | Description | Selected |
|--------|-------------|----------|
| 15m access + 30d rotating refresh | Short-lived JWT access token, refresh rotation on each refresh, revoke old token immediately | yes |
| 30m access + 14d rotating refresh | Fewer refresh calls, slightly higher access-token risk window | no |
| 60m access + 7d static refresh | Simpler implementation, weaker compromise containment | no |

**User choice:** 15m access + 30d rotating refresh
**Notes:** Preferred security-first baseline.

---

## Email + reset flow

| Option | Description | Selected |
|--------|-------------|----------|
| Verify required before checkout/admin | Allow registration/login, block sensitive actions until verified; reset token one-time, 30m expiry | yes |
| Verify required before first login | Strict gate with higher onboarding friction | no |
| Verification optional initially | Faster onboarding, weaker assurance | no |

**User choice:** Verify required before checkout/admin
**Notes:** Keep onboarding smooth but protect sensitive capabilities.

---

## Admin access boundary

| Option | Description | Selected |
|--------|-------------|----------|
| Manual seed first admin + strict role claims | First admin via seed/migration; admin endpoints require Admin role policy only | yes |
| Promote by config email list | Auto-promote users whose email is in allowlist | no |
| Self-service admin request | Requires additional approval workflow | no |

**User choice:** Manual seed first admin + strict role claims
**Notes:** Enforce explicit and controlled admin bootstrap.

---

## Account protection rules

| Option | Description | Selected |
|--------|-------------|----------|
| Strong password + IP/user throttle + temp lockout | Min 10 chars with complexity, login/reset throttling, 15m lockout after repeated failures, audit events | yes |
| Medium password + throttle only | Less friction, no lockout state | no |
| Password only | Minimal effort, weak brute-force protection | no |

**User choice:** Strong password + IP/user throttle + temp lockout
**Notes:** Security baseline should include lockout and auditability.

---

## the agent's Discretion

- Hash algorithm parameterization and exact token-store schema.
- Error payload shape and internal event taxonomy.

## Deferred Ideas

None.
