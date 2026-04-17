---
phase: 04-cart-checkout-capture
plan: 01
subsystem: application
tags: [checkout, cart, unit-tests, conflict-contract, tdd]
requires:
  - phase: 03-inventory-integrity-reservation-control
    provides: reservation conflict model and availability semantics
provides:
  - Deterministic cart service add/set/remove/get operations
  - Stock conflict contract with product/requested/available fields
  - Unit proof for D-01 through D-04 rules
affects: [checkout-service, checkout-api, cart-persistence]
tech-stack:
  added: []
  patterns: [single-line merge by product id, absolute set semantics, explicit remove command]
key-files:
  created: []
  modified:
    - src/Application/Checkout/Services/CartService.cs
    - src/Application/Checkout/Contracts/CartContracts.cs
    - tests/UnitTests/Checkout/CartServiceTests.cs
key-decisions:
  - "Cart add uses merge semantics while set uses absolute quantity to avoid ambiguous deltas."
  - "Stock overflows throw explicit cart conflict exceptions for deterministic API 409 mapping."
patterns-established:
  - "Cart deletion is endpoint/service explicit remove, never quantity=0 shorthand."
requirements-completed: [CHK-01]
duration: 10min
completed: 2026-04-17
---

# Phase 4 Plan 01: Cart service contracts and mutation invariants Summary

**Cart mutation core now enforces merge/add, absolute set, explicit remove, and deterministic stock conflict behavior with unit-level contract proof.**

## Performance

- **Duration:** 10 min
- **Started:** 2026-04-17T13:18:00Z
- **Completed:** 2026-04-17T13:28:00Z
- **Tasks:** 2
- **Files modified:** 7

## Accomplishments
- Added cart command/query contracts and stock conflict exception payload.
- Implemented `CartService` orchestration using availability gateway checks before mutation.
- Locked D-01..D-04 semantics with targeted unit tests.

## Task Commits

1. **Task 1: Create cart contracts and failing unit tests for locked cart rules** - `0de4c85` (test)
2. **Task 2: Implement CartService to satisfy merge/set/remove/conflict tests** - `3471d8f` (feat)

## Files Created/Modified
- `src/Application/Checkout/Contracts/CartContracts.cs` - Cart request/response and conflict contracts.
- `src/Application/Checkout/Contracts/ICartRepository.cs` - Cart persistence port.
- `src/Application/Checkout/Contracts/ICartProductAvailabilityGateway.cs` - Availability gateway port.
- `src/Application/Checkout/Services/CartService.cs` - Cart mutation/query orchestration.
- `src/Domain/Checkout/Cart.cs` - Cart aggregate behaviors.
- `src/Domain/Checkout/CartLine.cs` - Cart line invariant handling.
- `tests/UnitTests/Checkout/CartServiceTests.cs` - D-01..D-04 verification suite.

## Decisions Made
- Add and set paths both validate quantity > 0 to close mutation ambiguity at trust boundary.
- Conflict contract includes `ProductId`, `RequestedQuantity`, and `AvailableQuantity` for RFC7807 extension mapping downstream.

## Deviations from Plan

None - plan executed exactly as written.

## Known Stubs

None.

## Threat Flags

None.

## Issues Encountered

None.

## Next Phase Readiness

Cart application contract is stable for checkout orchestration and persistence wiring.

## Self-Check: PASSED

- FOUND: `0de4c85`
- FOUND: `3471d8f`
- FOUND: `.planning/phases/04-cart-checkout-capture/04-01-SUMMARY.md`
