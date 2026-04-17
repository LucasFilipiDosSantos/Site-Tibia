---
phase: 05-order-lifecycle-timeline-visibility
plan: 01
subsystem: api
tags: [ef-core, repository, domain-driven-design, state-machine]

# Dependency graph
requires: []
provides:
  - Order lifecycle state machine (Pending/Paid/Cancelled)
  - Transition source types (System/Admin/Customer)
  - Immutable timeline events (from/to/source/timestamp)
  - Unit tests for legal transition matrix and idempotency

affects: [05-02-persistence, 05-03-api, payment-processing, order-fulfillment]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - State machine pattern with source-specific transition validation
    - Append-only timeline with idempotent no-op on duplicate status
    - Explicit command contracts (no generic SetStatus)

key-files:
  created:
    - src/Domain/Checkout/OrderStatus.cs
    - src/Domain/Checkout/OrderStatusTransitionEvent.cs
    - src/Application/Checkout/Contracts/OrderLifecycleContracts.cs
    - tests/UnitTests/Checkout/OrderLifecycleStateMachineTests.cs
    - tests/UnitTests/Checkout/OrderStatusTimelineTests.cs
  modified:
    - src/Domain/Checkout/Order.cs
    - src/Application/Checkout/Services/OrderLifecycleService.cs

key-decisions:
  - "Pending→Paid allowed only for System source (D-03)"
  - "Customer/Admin can cancel Pending orders only (D-02)"
  - "Duplicate target-status requests are idempotent no-ops (D-04, D-05)"

requirements-completed: [ORD-01, ORD-02]

# Metrics
duration: 0min
completed: 2026-04-17
---

# Phase 05 Plan 01: Order Lifecycle State Machine Summary

**Order lifecycle core with state-machine validation and immutable timeline events**

## Performance

- **Duration:** 0 min (pre-completed)
- **Completed:** 2026-04-17
- **Tasks:** 2 (pre-completed)
- **Files modified:** 7

## Accomplishments

- Order domain model with transition validation by source authority
- Status history events append only on real changes (D-06, D-07)
- Explicit command contracts: ApplySystemTransition, ApplyAdminCancel, ApplyCustomerCancel
- Unit tests verify legal transition matrix and idempotent no-op behavior
- ForbiddenStatusTransitionException carries currentStatus and allowedTransitions metadata

## Task Commits

Plan already committed in prior session.

**Plan metadata:** 4474394 (docs: complete plan)

## Files Created/Modified

- `src/Domain/Checkout/OrderStatus.cs` - Status enum: Pending, Paid, Cancelled
- `src/Domain/Checkout/OrderStatusTransitionEvent.cs` - Timeline event with from/to/source/timestamp
- `src/Application/Checkout/Contracts/OrderLifecycleContracts.cs` - Explicit commands + conflict exception
- `src/Domain/Checkout/Order.cs` - ApplyTransition method with source-specific validation
- `src/Application/Checkout/Services/OrderLifecycleService.cs` - Application orchestration
- `tests/UnitTests/Checkout/OrderLifecycleStateMachineTests.cs` - Legal matrix tests (165 lines)
- `tests/UnitTests/Checkout/OrderStatusTimelineTests.cs` - Timeline append-only tests

## Decisions Made

- Used explicit command contracts instead of generic SetStatus (per D-14)
- Transition events only created when status actually changes (per D-04, D-05)
- Source type captured for audit trail (per D-06)

## Deviations from Plan

None - plan executed exactly as written.

---

*Phase: 05-order-lifecycle-timeline-visibility*
*Completed: 2026-04-17*