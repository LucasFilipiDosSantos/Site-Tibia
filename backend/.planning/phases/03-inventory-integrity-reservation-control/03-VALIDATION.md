---
phase: 03
slug: inventory-integrity-reservation-control
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-17
---

# Phase 03 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET 10) |
| **Config file** | `tests/UnitTests/UnitTests.csproj`, `tests/IntegrationTests/IntegrationTests.csproj` |
| **Quick run command** | `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~Inventory"` |
| **Full suite command** | `dotnet test backend.slnx` |
| **Estimated runtime** | ~120 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~Inventory"`
- **After every plan wave:** Run `dotnet test backend.slnx`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 120 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 03-01-01 | 01 | 1 | INV-02, INV-03 | T-03-01 | Reservation idempotency and conflict outcomes are deterministic under retry/race | unit | `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~InventoryReservation"` | ✅ | ⬜ pending |
| 03-01-02 | 01 | 1 | INV-01 | T-03-02 | Availability computation never leaks negative values and respects release semantics | unit | `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~InventoryAvailability"` | ✅ | ⬜ pending |
| 03-02-01 | 02 | 2 | INV-01, INV-02, INV-04 | T-03-03 | Persisted stock/reservation/audit records maintain transactional integrity | integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~InventoryPersistence"` | ❌ W0 | ⬜ pending |
| 03-02-02 | 02 | 2 | INV-01, INV-02, INV-04 | T-03-04 | Schema constraints enforce optimistic concurrency and non-negative stock boundaries | integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~InventorySchema"` | ❌ W0 | ⬜ pending |
| 03-03-01 | 03 | 3 | INV-01, INV-02, INV-03 | T-03-05 | API returns 409 ProblemDetails with actionable available quantity details on insufficiency/conflict | integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~InventoryEndpoints"` | ❌ W0 | ⬜ pending |
| 03-03-02 | 03 | 3 | INV-04 | T-03-06 | Admin adjustment endpoints require AdminOnly and produce complete audit records | integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~InventoryAdmin"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/UnitTests/Inventory/InventoryReservationServiceTests.cs` — reservation/idempotency/conflict test scaffolds
- [ ] `tests/UnitTests/Inventory/InventoryAdjustmentInvariantTests.cs` — delta/negative/reason invariants
- [ ] `tests/IntegrationTests/Inventory/InventoryEndpointsTests.cs` — API contract scaffolds

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
