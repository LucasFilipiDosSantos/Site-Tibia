---
phase: 04-cart-checkout-capture
plan: 04
subsystem: api
tags: [checkout, cart, minimal-api, problemdetails, jwt, integration-tests]
requires:
  - phase: 04-cart-checkout-capture
    provides: checkout/cart application logic and persistence repositories
provides:
  - Authenticated checkout/cart endpoint surface for CHK-01..CHK-03
  - ProblemDetails 409 mapping with lineConflicts payload
  - Integration contract coverage for auth, conflict, snapshot, and cart-clear behavior
affects: [frontend-checkout-flow, customer-order-history, support-troubleshooting]
tech-stack:
  added: []
  patterns: [JWT sub-derived customer identity, typed DTO-only endpoint tests, deterministic 409 line conflict extensions]
key-files:
  created:
    - src/API/Checkout/CheckoutDtos.cs
    - src/API/Checkout/CheckoutEndpoints.cs
    - src/Infrastructure/Checkout/CheckoutServiceDependencies.cs
    - tests/IntegrationTests/Checkout/CartEndpointsTests.cs
    - tests/IntegrationTests/Checkout/CheckoutEndpointsTests.cs
  modified:
    - src/API/Program.cs
    - src/API/ErrorHandling/GlobalExceptionHandler.cs
    - src/Infrastructure/DependencyInjection.cs
key-decisions:
  - "All checkout/cart routes require authenticated verified users and derive customer identity from JWT sub claim."
  - "Cart and checkout conflicts share 409 ProblemDetails title 'Conflict.' with `lineConflicts` extension payload for deterministic client handling."
patterns-established:
  - "Integration tests enforce typed payload usage and explicitly assert conflict detail structure."
requirements-completed: [CHK-01, CHK-02, CHK-03]
duration: 22min
completed: 2026-04-17
---

# Phase 4 Plan 04: Cart and checkout API contract delivery Summary

**The checkout transport layer now exposes authenticated cart and order-capture endpoints with deterministic conflict payloads and integration-verified snapshot and cart-clear behavior.**

## Performance

- **Duration:** 22 min
- **Started:** 2026-04-17T13:53:00Z
- **Completed:** 2026-04-17T14:15:42Z
- **Tasks:** 2
- **Files modified:** 8

## Accomplishments
- Added minimal API checkout/cart routes (`GET/POST/PUT/DELETE` cart, submit checkout, get order).
- Added checkout transport DTOs and endpoint registration in Program.
- Added infrastructure gateway adapters connecting checkout/cart services to inventory and catalog dependencies.
- Extended global exception mapping to emit `lineConflicts` payload for cart and checkout stock conflicts.
- Added integration suites verifying auth gates, merge/set/remove semantics, 409 payload structure, immutable snapshot reads, and successful cart clearing.

## Task Commits

1. **Task 1: Implement cart and checkout API DTO/contracts with integration RED tests** - `1314892` (test)
2. **Task 2: Implement API conflict mapping and final end-to-end contract verification** - `9a3c90f` (feat)

## Files Created/Modified
- `src/API/Checkout/CheckoutDtos.cs` - Cart/checkout transport contracts including snapshot fields.
- `src/API/Checkout/CheckoutEndpoints.cs` - Checkout/cart endpoint mappings and request/response adaptation.
- `src/API/Program.cs` - Checkout endpoint registration and service registrations.
- `src/API/ErrorHandling/GlobalExceptionHandler.cs` - 409 mapping with `lineConflicts` extension for cart/checkout conflicts.
- `src/Infrastructure/Checkout/CheckoutServiceDependencies.cs` - Gateway adapters for inventory reserve, product snapshot, and cart availability.
- `src/Infrastructure/DependencyInjection.cs` - Wiring for checkout/cart gateways and repositories.
- `tests/IntegrationTests/Checkout/CartEndpointsTests.cs` - Cart endpoint integration contracts.
- `tests/IntegrationTests/Checkout/CheckoutEndpointsTests.cs` - Checkout endpoint integration contracts.

## Decisions Made
- Endpoint-level identity resolution always uses JWT `sub` and never accepts customer IDs from payloads.
- Conflict extension key standardized as `lineConflicts` across cart add/set and checkout submit.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Corrected conflict test setup to avoid cart-layer preemption**
- **Found during:** Task 2 integration verification
- **Issue:** Checkout conflict test initially produced 200 because cart availability gate rejected none and checkout reserve had same availability baseline.
- **Fix:** Seeded cart-availability high enough for add path and lowered checkout-reserve availability before submit to trigger intended D-14/D-15 conflict path.
- **Files modified:** `tests/IntegrationTests/Checkout/CheckoutEndpointsTests.cs`
- **Verification:** `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~CartEndpointsTests|FullyQualifiedName~CheckoutEndpointsTests"`
- **Committed in:** `1314892`

---

**Total deviations:** 1 auto-fixed (Rule 1 bug)
**Impact on plan:** Improved test fidelity to required checkout conflict behavior; no scope expansion.

## Known Stubs

None.

## Threat Flags

| Flag | File | Description |
|------|------|-------------|
| threat_flag: new-network-endpoints | `src/API/Checkout/CheckoutEndpoints.cs` | Added authenticated cart and checkout routes handling state mutation and order creation across trust boundary. |

## Issues Encountered

- Full-suite `dotnet test backend.slnx -v minimal` initially failed due to pre-existing SMTP placeholder configuration in unrelated identity tests; reran with `IdentityTokenDelivery__Provider=inmemory` for complete green verification.

## Next Phase Readiness

Phase 04 requirements are now user-visible through API contracts and backed by integration tests.

## Self-Check: PASSED

- FOUND: `1314892`
- FOUND: `9a3c90f`
- FOUND: `.planning/phases/04-cart-checkout-capture/04-04-SUMMARY.md`
