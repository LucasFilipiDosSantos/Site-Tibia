---
phase: 04
slug: cart-checkout-capture
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-17
---

# Phase 04 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET test SDK) |
| **Config file** | `backend.slnx` |
| **Quick run command** | `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~Cart|FullyQualifiedName~Checkout"` |
| **Full suite command** | `dotnet test backend.slnx -v minimal` |
| **Estimated runtime** | ~90 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~Cart|FullyQualifiedName~Checkout"`
- **After every plan wave:** Run `dotnet test backend.slnx -v minimal`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 120 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 04-01-01 | 01 | 1 | CHK-01 | T-04-01 | Cart add merges duplicate product lines and rejects oversell as 409 | unit | `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~CartServiceTests"` | ✅ | ⬜ pending |
| 04-02-01 | 02 | 2 | CHK-02 | T-04-02 | Checkout writes immutable snapshots and fails atomically on reserve conflict | unit | `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~CheckoutServiceTests"` | ✅ | ⬜ pending |
| 04-03-01 | 03 | 3 | CHK-01, CHK-02, CHK-03 | T-04-03 | Persistence schema enforces cart/order/snapshot/instruction invariants | integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~CheckoutPersistenceContractTests"` | ✅ | ⬜ pending |
| 04-03-02 | 03 | 3 | CHK-01, CHK-02, CHK-03 | T-04-04 | Live database schema is migrated before verification (blocking) | infra | `dotnet ef database update --project src/Infrastructure/Infrastructure.csproj --startup-project src/API/API.csproj --context AppDbContext` | ✅ | ⬜ pending |
| 04-04-01 | 04 | 4 | CHK-01, CHK-02, CHK-03 | T-04-05 | API contracts enforce auth, 409 conflict details, snapshot reads, and cart clear on success | integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~CartEndpointsTests|FullyQualifiedName~CheckoutEndpointsTests"` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/UnitTests/Checkout/CartServiceTests.cs` — scaffold for cart merge/set/remove/oversell behaviors
- [ ] `tests/UnitTests/Checkout/CheckoutServiceTests.cs` — scaffold for snapshot immutability + delivery instruction validation
- [ ] `tests/IntegrationTests/Checkout/CheckoutPersistenceContractTests.cs` — persistence contract harness
- [ ] `tests/IntegrationTests/Checkout/CheckoutEndpointsTests.cs` — API contract harness

---

## Manual-Only Verifications

All phase behaviors have automated verification.

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 120s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
