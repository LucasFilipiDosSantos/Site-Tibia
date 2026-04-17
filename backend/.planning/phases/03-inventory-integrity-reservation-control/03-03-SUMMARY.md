---
phase: 03-inventory-integrity-reservation-control
plan: 03
subsystem: api
tags: [inventory, minimal-api, problem-details, authorization, integration-tests]
requires:
  - phase: 03-inventory-integrity-reservation-control
    provides: inventory service orchestration and persistence transaction guarantees
provides:
  - Public inventory availability, reserve, and release API endpoints
  - Admin-only inventory adjustment endpoint with authenticated actor binding
  - 409 conflict ProblemDetails contract exposing availableQuantity detail
affects: [checkout-api, admin-operations, frontend-inventory-ui]
tech-stack:
  added: []
  patterns: [minimal API route groups, typed DTO transport contracts, deterministic conflict ProblemDetails extensions]
key-files:
  created:
    - src/API/Inventory/InventoryDtos.cs
    - src/API/Inventory/InventoryEndpoints.cs
    - tests/IntegrationTests/Inventory/InventoryEndpointsTests.cs
  modified:
    - src/API/ErrorHandling/GlobalExceptionHandler.cs
    - src/API/Program.cs
key-decisions:
  - "Admin adjustment actor identity is sourced from authenticated JWT sub claim, not client payload, to prevent actor spoofing."
  - "Inventory conflict handling extends existing RFC7807 mapping with only availableQuantity extension to keep details actionable and non-sensitive."
patterns-established:
  - "Inventory integration tests use API inventory DTOs exclusively to avoid payload drift."
  - "Admin inventory routes are grouped under /admin/inventory with AuthPolicies.AdminOnly enforcement."
requirements-completed: [INV-01, INV-02, INV-03, INV-04]
duration: 11min
completed: 2026-04-17
---

# Phase 3 Plan 03: Inventory API contracts and authorization proof Summary

**Inventory API now exposes real-time availability/reservation/release endpoints plus admin-gated stock adjustments with deterministic 409 conflict semantics carrying available quantity detail.**

## Performance

- **Duration:** 11 min
- **Started:** 2026-04-17T12:07:11Z
- **Completed:** 2026-04-17T12:18:38Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments
- Added inventory API DTO contracts and minimal API endpoints for availability, reserve, release, and admin adjustments.
- Extended global exception mapping to produce 409 ProblemDetails for reservation conflicts with `availableQuantity` extension.
- Added comprehensive integration tests for 200/409 behavior, admin 401/403/2xx authorization outcomes, and oversell blocking semantics.

## Task Commits

Each task was committed atomically:

1. **Task 1: Implement inventory endpoint contracts and conflict ProblemDetails mapping** - `4b82d4b` (test), `5adad09` (feat)
2. **Task 2: Enforce admin-only stock adjustment endpoint and final requirement proof suite** - `675c40d` (test)

## Files Created/Modified
- `src/API/Inventory/InventoryDtos.cs` - Typed transport contracts for availability, reserve, release, and admin adjust flows.
- `src/API/Inventory/InventoryEndpoints.cs` - Inventory route mappings with AdminOnly authorization group for adjustments.
- `src/API/ErrorHandling/GlobalExceptionHandler.cs` - Inventory conflict exception mapping to 409 with `availableQuantity` extension.
- `src/API/Program.cs` - Inventory service registration and endpoint mapping wiring.
- `tests/IntegrationTests/Inventory/InventoryEndpointsTests.cs` - End-to-end API contract tests for public/admin inventory behavior.

## Decisions Made
- Bound admin adjustment actor identity to JWT `sub` claim in endpoint layer to preserve audit integrity.
- Reused existing global exception pipeline for inventory conflicts to keep response contract consistent across API domains.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Removed client-supplied admin identity from adjustment command input**
- **Found during:** Task 2 (admin endpoint authorization and audit proof)
- **Issue:** Initial admin adjustment DTO accepted `AdminUserId` from request body, enabling actor spoofing at trust boundary.
- **Fix:** Changed endpoint to derive admin actor from authenticated JWT `sub` claim and ignore caller-provided identity.
- **Files modified:** `src/API/Inventory/InventoryEndpoints.cs`, `src/API/Inventory/InventoryDtos.cs`, `tests/IntegrationTests/Inventory/InventoryEndpointsTests.cs`
- **Verification:** `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~InventoryEndpointsTests"`
- **Committed in:** `5adad09` (part of task commit)

---

**Total deviations:** 1 auto-fixed (Rule 2 missing critical)
**Impact on plan:** Strengthened security posture with no API scope expansion; behavior remains aligned with requirement contracts.

## Issues Encountered
- One integration test run aborted due to transient testhost connection timeout; rerun with blame diagnostics passed and subsequent runs were stable.
- Full solution tests surfaced unrelated pre-existing identity SMTP placeholder configuration failures outside this plan’s changed files; not modified per scope boundary.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Phase 03 now has complete inner-layer, persistence, and API coverage for INV-01..INV-04.
- Ready for cross-phase verification/audit workflow with inventory behaviors fully test-backed.

## Self-Check: PASSED

- FOUND: `.planning/phases/03-inventory-integrity-reservation-control/03-03-SUMMARY.md`
- FOUND: `4b82d4b`
- FOUND: `5adad09`
- FOUND: `675c40d`
