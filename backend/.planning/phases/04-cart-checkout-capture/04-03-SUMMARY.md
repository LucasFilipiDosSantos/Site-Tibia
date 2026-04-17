---
phase: 04-cart-checkout-capture
plan: 03
subsystem: database
tags: [ef-core, postgres, migrations, checkout, cart, integration-tests]
requires:
  - phase: 04-cart-checkout-capture
    provides: checkout/cart domain and application contracts
provides:
  - PostgreSQL schema for carts, cart lines, orders, snapshots, and delivery instructions
  - EF repositories for cart and checkout persistence
  - Integration proof for immutable snapshots and cart clear persistence behavior
affects: [checkout-api, operational-migrations, order-history-reads]
tech-stack:
  added: []
  patterns: [unique customer cart index, unique cart line composite index, snapshot money schema numeric(18,2)+currency(3)]
key-files:
  created:
    - src/Infrastructure/Checkout/Repositories/CartRepository.cs
    - src/Infrastructure/Checkout/Repositories/CheckoutRepository.cs
    - src/Infrastructure/Persistence/Configurations/CartConfiguration.cs
    - src/Infrastructure/Persistence/Configurations/CartLineConfiguration.cs
    - src/Infrastructure/Persistence/Configurations/OrderConfiguration.cs
    - src/Infrastructure/Persistence/Configurations/OrderItemSnapshotConfiguration.cs
    - src/Infrastructure/Persistence/Configurations/DeliveryInstructionConfiguration.cs
    - src/Infrastructure/Persistence/Migrations/20260417140532_AddCartCheckoutCapture.cs
    - tests/IntegrationTests/Checkout/CheckoutPersistenceContractTests.cs
  modified:
    - src/Infrastructure/Persistence/AppDbContext.cs
    - src/Infrastructure/DependencyInjection.cs
    - src/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs
key-decisions:
  - "Persisted one-cart-per-customer and one-line-per-(cart,product) constraints to enforce D-01 at DB boundary."
  - "Applied blocking migration push before final verification to eliminate schema drift risk."
patterns-established:
  - "Checkout persistence integration tests use real EF mappings with sqlite relational constraints for contract proof."
requirements-completed: [CHK-01, CHK-02, CHK-03]
duration: 27min
completed: 2026-04-17
---

# Phase 4 Plan 03: Checkout persistence schema and repository durability Summary

**Checkout/cart data is now durably persisted with EF/PostgreSQL mappings, enforced relational constraints, and migration-backed schema updates validated by integration contract tests.**

## Performance

- **Duration:** 27 min
- **Started:** 2026-04-17T13:48:00Z
- **Completed:** 2026-04-17T14:15:00Z
- **Tasks:** 2
- **Files modified:** 15

## Accomplishments
- Added EF mappings and repositories for cart, order, snapshot, and delivery instruction persistence.
- Generated and applied `AddCartCheckoutCapture` migration to live local Postgres.
- Added and passed integration contracts proving merged-line persistence, snapshot immutability under catalog mutation, delivery branch persistence, and cart clear behavior.

## Task Commits

1. **Task 1: Add checkout/cart persistence mappings and integration RED tests** - `67bee1d` (test)
2. **Task 2: [BLOCKING] Generate/apply migration and push schema before verification** - `ad0e0cc` (feat)

## Files Created/Modified
- `src/Infrastructure/Persistence/AppDbContext.cs` - Added checkout/cart DbSet registrations.
- `src/Infrastructure/Persistence/Configurations/CartConfiguration.cs` - Cart table mapping and unique customer index.
- `src/Infrastructure/Persistence/Configurations/CartLineConfiguration.cs` - Cart line mapping and unique composite index.
- `src/Infrastructure/Persistence/Configurations/OrderConfiguration.cs` - Order root mapping with intent-key uniqueness.
- `src/Infrastructure/Persistence/Configurations/OrderItemSnapshotConfiguration.cs` - Snapshot schema with numeric(18,2) and currency length 3.
- `src/Infrastructure/Persistence/Configurations/DeliveryInstructionConfiguration.cs` - Delivery instruction persistence mapping.
- `src/Infrastructure/Checkout/Repositories/CartRepository.cs` - Cart persistence load/save/clear operations.
- `src/Infrastructure/Checkout/Repositories/CheckoutRepository.cs` - Order persistence and snapshot-backed reads.
- `src/Infrastructure/DependencyInjection.cs` - Checkout/cart repository registrations.
- `src/Infrastructure/Persistence/Migrations/20260417140532_AddCartCheckoutCapture*.cs` - New schema migration.
- `tests/IntegrationTests/Checkout/CheckoutPersistenceContractTests.cs` - Persistence behavior integration tests.

## Decisions Made
- Cart persistence updates existing cart lines through aggregate operations instead of ad-hoc row mutation to preserve domain invariants.
- Delivery instruction discriminator stored as string enum to keep historical readability.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Started local Postgres container before migration push**
- **Found during:** Task 2 (schema update)
- **Issue:** `dotnet ef database update` failed with connection refused on localhost:5432.
- **Fix:** Started `postgres` service via `docker compose up -d postgres` and reran schema push successfully.
- **Files modified:** none (environment step)
- **Verification:** `dotnet ef database update --project src/Infrastructure/Infrastructure.csproj --startup-project src/API/API.csproj --context AppDbContext`
- **Committed in:** `ad0e0cc` (migration + schema state)

---

**Total deviations:** 1 auto-fixed (Rule 3 blocking)
**Impact on plan:** Required operational precondition for mandatory migration push.

## Known Stubs

None.

## Threat Flags

| Flag | File | Description |
|------|------|-------------|
| threat_flag: schema-trust-boundary | `src/Infrastructure/Persistence/Migrations/20260417140532_AddCartCheckoutCapture.cs` | Added new persistent order/cart schema at client-input trust boundary requiring continued conflict and validation enforcement in API layer. |

## Issues Encountered

- Initial EF mapping for private field navigation (`_lines`) failed during integration run; corrected to mapped public navigation with field access mode.

## Next Phase Readiness

Persistence foundation for cart/checkout is migrated and ready for transport/API contract exposure.

## Self-Check: PASSED

- FOUND: `67bee1d`
- FOUND: `ad0e0cc`
- FOUND: `.planning/phases/04-cart-checkout-capture/04-03-SUMMARY.md`
