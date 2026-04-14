# Phase 1: Identity & Security Foundation - Context

**Gathered:** 2026-04-14
**Status:** Ready for planning

<domain>
## Phase Boundary

Deliver secure account access and protected role boundaries for backend APIs: registration, login, JWT access+refresh session continuity, email verification, password reset, RBAC enforcement for admin endpoints, and baseline account protection controls.

</domain>

<decisions>
## Implementation Decisions

### Token Session Policy
- **D-01:** Use JWT access token lifetime of 15 minutes.
- **D-02:** Use refresh token lifetime of 30 days with rotation on every refresh.
- **D-03:** Revoke previous refresh token immediately after successful rotation.

### Email Verification and Password Reset
- **D-04:** Email verification is required before checkout and any admin capability.
- **D-05:** Registration/login may proceed before verification, but sensitive/protected actions are blocked until verified.
- **D-06:** Password reset token is one-time use and expires in 30 minutes.

### Admin Access Boundary
- **D-07:** First admin account is provisioned manually via controlled seed/migration path.
- **D-08:** Admin endpoints require strict Admin role policy based on role claims.

### Account Protection Rules
- **D-09:** Enforce strong password policy (minimum 10 chars with complexity requirements).
- **D-10:** Apply login/reset throttling by user and IP.
- **D-11:** Apply temporary lockout for 15 minutes after repeated failed authentication attempts.
- **D-12:** Record security-relevant audit events for auth/session/lockout flows.

### the agent's Discretion
- Password hashing algorithm parameters and cost tuning (within strong one-way hashing requirement).
- Exact storage model for refresh token families and revocation metadata.
- Exact HTTP error response shape for auth/security failures (while preserving security posture).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase definition and acceptance
- `.planning/ROADMAP.md` - Phase 1 goal, dependencies, requirements mapping, and success criteria.
- `.planning/REQUIREMENTS.md` - AUTH-01..04 and SEC-01..02 requirement contracts.

### Project constraints and guardrails
- `.planning/PROJECT.md` - Non-negotiable stack, architecture, security, and reliability constraints.
- `AGENTS.md` - Enforceable architecture boundary guardrails for layer ownership and dependency direction.

### Repository structure baseline
- `backend.slnx` - Canonical solution/project layout (`src/API`, `src/Application`, `src/Domain`, `src/Infrastructure`, `tests/UnitTests`).
- `.planning/research/ARCHITECTURE.md` - Layer separation baseline aligned to .NET Clean Architecture in this repo.
- `.planning/research/STACK.md` - Package placement and dependency boundaries per project.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `src/API/Program.cs` - Minimal ASP.NET Core host bootstrap ready for auth middleware/policy wiring.

### Established Patterns
- `backend.slnx` and project files already enforce four-layer split (`API`, `Application`, `Domain`, `Infrastructure`) plus `tests/UnitTests`.
- `src/API/API.csproj` currently contains only OpenAPI package; auth/security packages still need explicit addition in planned tasks.

### Integration Points
- API auth endpoints, middleware, and policy registration in `src/API`.
- Token/session use cases and contracts in `src/Application`.
- User/session/security domain rules in `src/Domain`.
- Persistence, token storage, hashing, and security adapters in `src/Infrastructure`.

</code_context>

<specifics>
## Specific Ideas

- Baseline should prefer short-lived access tokens with rotating refresh tokens.
- Verification gate should protect checkout/admin actions, not block initial onboarding login.
- Role boundary must be strict and explicit from first admin bootstrap onward.

</specifics>

<deferred>
## Deferred Ideas

None - discussion stayed within phase scope.

</deferred>

---

*Phase: 01-identity-security-foundation*
*Context gathered: 2026-04-14*
