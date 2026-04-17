---
phase: 05
slug: order-lifecycle-timeline-visibility
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-17
---

# Phase 05 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit + dotnet test |
| **Config file** | `tests/UnitTests/UnitTests.csproj`, `tests/IntegrationTests/IntegrationTests.csproj` |
| **Quick run command** | `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~Order"` |
| **Full suite command** | `dotnet test backend.slnx -v minimal` |
| **Estimated runtime** | ~90 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~Order"`
- **After every plan wave:** Run `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~Order|FullyQualifiedName~Checkout"`
- **Before `/gsd-verify-work`:** Full suite must be green via `dotnet test backend.slnx -v minimal`
- **Max feedback latency:** 90 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 05-01-01 | 01 | 1 | ORD-01 | T-05-01 | Only legal transitions execute by source authority | unit | `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~OrderLifecycle"` | ✅ | ⬜ pending |
| 05-01-02 | 01 | 1 | ORD-02 | T-05-02 | Status events append only on real transition with UTC timestamp | unit | `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~OrderStatusEvent"` | ✅ | ⬜ pending |
| 05-02-01 | 02 | 2 | ORD-02 | T-05-03 | Persistence keeps immutable status history rows and order lifecycle fields | integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~OrderLifecycleRepository"` | ✅ | ⬜ pending |
| 05-02-02 | 02 | 2 | ORD-03 | T-05-04 | Customer history queries return scoped/paged/timeline data from persisted events | integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~CheckoutOrderHistory"` | ✅ | ⬜ pending |
| 05-03-01 | 03 | 3 | ORD-03 | T-05-05 | Customer endpoints enforce ownership and emit code+label timeline contracts | integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~CheckoutOrderHistoryEndpoints"` | ✅ | ⬜ pending |
| 05-03-02 | 03 | 3 | ORD-04 | T-05-06 | Admin search/action endpoints enforce AdminOnly + reasoned transitions with 409 conflict metadata | integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~AdminOrderManagementEndpoints"` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/UnitTests/Checkout/OrderLifecycleStateMachineTests.cs` — transition legality/idempotency scaffold
- [ ] `tests/IntegrationTests/Checkout/OrderHistoryEndpointsTests.cs` — customer/admin endpoint contract scaffold

---

## Manual-Only Verifications

All phase behaviors have automated verification.

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 90s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
