---
phase: 04-cart-checkout-capture
plan: 06
subsystem: checkout
tags: [checkout, inventory, idempotency, atomicity, integration-tests]
requires:
  - phase: 04-05
    provides: compensation-aware checkout rollback path
provides:
  - product-scoped inventory idempotency keyed by orderIntentKey + productId
  - quantity-safety guard for idempotent replay under shared checkout intent key
  - real-path integration proof for multi-line reserve-all success and fail-all rollback
affects: [phase-05-order-lifecycle, checkout, inventory]
tech-stack:
  added: []
  patterns:
    - product-scoped idempotent replay with strict quantity parity
    - checkout multi-line real-path atomicity verification against EF repository
key-files:
  created: [.planning/phases/04-cart-checkout-capture/04-06-SUMMARY.md]
  modified:
    - src/Application/Inventory/Contracts/IInventoryRepository.cs
    - src/Application/Inventory/Services/InventoryService.cs
    - src/Infrastructure/Inventory/Repositories/InventoryRepository.cs
    - tests/UnitTests/Inventory/InventoryReservationServiceTests.cs
    - tests/IntegrationTests/Checkout/CheckoutPersistenceContractTests.cs
    - tests/UnitTests/Inventory/InventoryAdjustmentInvariantTests.cs
    - tests/IntegrationTests/Inventory/InventoryEndpointsTests.cs
key-decisions:
  - "Inventory idempotency replay is valid only for exact orderIntentKey + productId + quantity matches."
  - "Shared checkout orderIntentKey is retained, but reserve semantics are line/product scoped to prevent hollow success."
  - "Checkout atomicity closure is proven through CheckoutService + InventoryService + InventoryRepository integration path."
patterns-established:
  - "Replay guard: same intent + same product + same quantity returns existing reservation; otherwise reserve/conflict path executes."
  - "Conflict branch preserves deterministic 409 lineConflicts payload while compensation releases prior line reservations."
requirements-completed: [CHK-02]
duration: 31min
completed: 2026-04-17
---

# Phase 04 Plan 06: Inventory Idempotency Atomicity Gap Closure Summary

**Checkout inventory reservation now enforces product-scoped idempotent replay with quantity safety and includes real persistence-path proof that multi-line checkout either reserves every line and persists one order or fails with full rollback.**

## Performance

- **Duration:** 31 min
- **Started:** 2026-04-17T14:52:38Z
- **Completed:** 2026-04-17T15:23:38Z
- **Tasks:** 2
- **Files modified:** 7

## Accomplishments
- Replaced intent-only replay lookup with product-scoped lookup (`orderIntentKey + productId`) and blocked replay when quantity mismatches.
- Preserved shared checkout intent traceability while preventing hollow success for unrelated cart lines under the same intent key.
- Added integration proof through real Checkout + Inventory service/repository path for multi-line success and conflict rollback semantics.

## Task Commits

Each task was committed atomically:

1. **Task 1: Fix inventory idempotency to be line/product scoped for shared checkout intents** - `004cef1` (fix)
2. **Task 2: Add real checkout+inventory integration proof for multi-line reserve-all atomicity** - `1c3c188` (test)

## Files Created/Modified
- `src/Application/Inventory/Contracts/IInventoryRepository.cs` - Added product-scoped active reservation lookup contract.
- `src/Application/Inventory/Services/InventoryService.cs` - Switched replay lookup to intent+product and added quantity mismatch guard.
- `src/Infrastructure/Inventory/Repositories/InventoryRepository.cs` - Implemented intent+product reservation query and sqlite-safe ordering behavior used by integration tests.
- `tests/UnitTests/Inventory/InventoryReservationServiceTests.cs` - Added replay/different-product/quantity-mismatch coverage for idempotency correctness.
- `tests/IntegrationTests/Checkout/CheckoutPersistenceContractTests.cs` - Added real-path multi-line success and fail-all rollback scenarios.
- `tests/UnitTests/Inventory/InventoryAdjustmentInvariantTests.cs` - Updated in-memory repository to satisfy interface evolution.
- `tests/IntegrationTests/Inventory/InventoryEndpointsTests.cs` - Updated in-memory repository to satisfy interface evolution.

## Decisions Made
- Idempotent replay under shared checkout intent is accepted only when product and quantity exactly match existing active reservation.
- Quantity mismatch for same intent/product is treated as deterministic operation failure to protect reserve-all correctness.
- Integration proof for D-14 must use real inventory persistence path, not only in-memory checkout doubles.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] New repository contract broke test doubles implementing `IInventoryRepository`**
- **Found during:** Task 1 (GREEN compile)
- **Issue:** Adding `GetReservationByIntentAndProductAsync` caused compile failures in existing unit/integration in-memory repositories.
- **Fix:** Implemented the new method in affected in-memory repositories while preserving existing test behaviors.
- **Files modified:** `tests/UnitTests/Inventory/InventoryAdjustmentInvariantTests.cs`, `tests/IntegrationTests/Inventory/InventoryEndpointsTests.cs`
- **Verification:** `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~InventoryReservationServiceTests"`
- **Committed in:** `004cef1`

**2. [Rule 3 - Blocking] SQLite cannot translate `DateTimeOffset` ORDER BY in reservation query**
- **Found during:** Task 2 (RED run for new integration tests)
- **Issue:** Real-path checkout integration tests failed with `NotSupportedException` due to provider translation limits in `OrderByDescending(x => x.ReservedAtUtc)`.
- **Fix:** Materialized filtered reservation rows, then applied ordering in memory for both intent+product lookup and intent-only release lookup.
- **Files modified:** `src/Infrastructure/Inventory/Repositories/InventoryRepository.cs`
- **Verification:** `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~CheckoutPersistenceContractTests"`
- **Committed in:** `1c3c188`

---

**Total deviations:** 2 auto-fixed (2 blocking)
**Impact on plan:** Both fixes were required to complete planned behavior and verification without scope creep.

## Issues Encountered
None beyond auto-fixed blocking issues.

## Auth Gates
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- D-14 reserve-all/fail-all gap is closed with product-scoped idempotency semantics and real-path multi-line integration evidence.
- Checkout conflict payload semantics (lineConflicts with availableQuantity only) remain unchanged.

## Self-Check: PASSED
- FOUND: `.planning/phases/04-cart-checkout-capture/04-06-SUMMARY.md`
- FOUND: `004cef1`
- FOUND: `1c3c188`
