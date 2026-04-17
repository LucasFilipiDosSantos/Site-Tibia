---
phase: 05-order-lifecycle-timeline-visibility
plan: 03
subsystem: api
tags: [aspnet-core, minimal-api, authorization, rfc7807]

# Dependency graph
requires:
  - phase: 05-01
    provides: Order lifecycle state machine and transition contracts
  - phase: 05-02
    provides: Persistence layer and repository methods
provides:
  - Customer order history GET /checkout/orders with pagination
  - Customer order detail with timeline GET /checkout/orders/{id}
  - Admin search GET /admin/orders with filters
  - Admin explicit cancel POST /admin/orders/{id}/actions/cancel
  - 409 Conflict mapping with currentStatus and allowedTransitions

affects: [payment-processing, order-fulfillment, admin-dashboard]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Customer endpoints under VerifiedForSensitiveActions auth
    - Admin endpoints under AdminOnly policy
    - RFC 7807 ProblemDetails for conflict responses
    - Actor identity from JWT sub claim (D-16)

key-files:
  created:
    - src/API/Checkout/AdminOrderEndpoints.cs
  modified:
    - src/API/Checkout/CheckoutEndpoints.cs
    - src/API/ErrorHandling/GlobalExceptionHandler.cs

key-decisions:
  - "Customer history returns statusCode and statusLabel (D-11)"
  - "Admin explicit cancel with required reason (D-14, D-16)"
  - "409 returns currentStatus and allowedTransitions (D-15)"

requirements-completed: [ORD-03, ORD-04, ORD-01]

# Metrics
duration: 0min
completed: 2026-04-17
---

# Phase 05 Plan 03: API Endpoints Summary

**Customer order history, admin management, and conflict response mapping**

## Performance

- **Duration:** 0 min (pre-completed)
- **Completed:** 2026-04-17
- **Tasks:** 2 (pre-completed)
- **Files modified:** 4

## Accomplishments

- Customer GET /checkout/orders with pagination and newest-first default
- Customer GET /checkout/orders/{id} includes statusCode and statusLabel
- Admin GET /admin/orders supports status, customerId, createdFrom/To filters
- Admin POST /admin/orders/{id}/actions/cancel with required reason
- ForbiddenStatusTransitionException maps to 409 with ProblemDetails
- Auth policies: VerifiedForSensitiveActions for customers, AdminOnly for admin

## Task Commits

Plan already committed in prior session.

**Plan metadata:** 4474394 (docs: complete plan)

## Files Created/Modified

- `src/API/Checkout/AdminOrderEndpoints.cs` - Admin search and explicit cancel (90 lines)
- `src/API/Checkout/CheckoutEndpoints.cs` - Customer order history endpoints
- `src/API/ErrorHandling/GlobalExceptionHandler.cs` - 409 conflict mapping

## Decisions Made

- No generic set-status endpoint - only explicit action routes (per D-14)
- Actor identity sourced from JWT sub claim, not client payload (per D-16)
- Reason required for admin transitions but optional for customer
- Admin search supports all filter combinations per D-13

## Deviations from Plan

None - plan executed exactly as written.

---

*Phase: 05-order-lifecycle-timeline-visibility*
*Completed: 2026-04-17*