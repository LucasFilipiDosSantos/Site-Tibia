---
phase: 09
plan: 01
subsystem: custom-orders
tags: [domain-model, lifecycle, custom-script, custom-macro]
dependency_graph:
  requires: []
  provides:
    - CustomRequest aggregate
    - ICustomOrderService
    - CustomRequests table
  affects:
    - Order lifecycle (optional orderId link)
tech_stack:
  added:
    - CustomRequestStatus enum
    - CustomRequest entity
    - ICustomOrderService + implementation
    - ICustomOrderRepository + implementation
    - CustomRequestConfiguration + migration
    - CustomOrderEndpoints
key_files:
  created:
    - src/Domain/Checkout/CustomRequestStatus.cs
    - src/Domain/Checkout/CustomRequest.cs
    - src/Application/Checkout/Contracts/CustomOrderContracts.cs
    - src/Application/Checkout/Services/CustomOrderService.cs
    - src/Infrastructure/Checkout/Repositories/CustomOrderRepository.cs
    - src/Infrastructure/Persistence/Configurations/CustomRequestConfiguration.cs
    - src/Infrastructure/Persistence/Migrations/20260418220000_AddCustomRequestTables.cs
    - src/API/CustomOrders/CustomOrderEndpoints.cs
  modified:
    - src/Infrastructure/Persistence/AppDbContext.cs
    - src/Infrastructure/DependencyInjection.cs
    - src/API/Program.cs
decisions:
  - CustomRequest linked to optional OrderId for existing order references
  - Status transitions enforced server-side (no client-controlled status)
  - Customer isolation via customerId check in GetByIdAsync
metrics:
  duration: ~3 min
  completed: 2026-04-18
  tasks: 3
  files: 11
---

# Phase 09 Plan 01: Custom Request Domain Model Summary

## Objective

Implemented custom request domain model and lifecycle for custom script/macro requests with customer submit and admin status management.

## What Was Built

### Domain Layer
- **CustomRequestStatus enum**: Pending(0), InProgress(1), Delivered(2) - linear progression only
- **CustomRequest entity**: Id, OrderId(nullable), CustomerId, Description, Status, CreatedAtUtc, UpdatedAtUtc
- Factory method enforces validation (customerId required, description required)
- Status transitions: StartProgress() (Pending→InProgress), MarkDelivered() (InProgress→Delivered)
- Transition guards prevent backwards status changes

### Application Layer
- **ICustomOrderService interface**: CreateRequestAsync, GetByIdAsync, GetCustomerRequestsAsync, StartProgressAsync, MarkDeliveredAsync
- **ICustomOrderRepository interface**: GetByIdAsync, GetByCustomerIdAsync, AddAsync, SaveChangesAsync
- **CustomOrderService implementation**: Full CRUD + status transition orchestration

### Infrastructure Layer
- **CustomRequestConfiguration**: EF Core config for custom_requests table
- **20260418220000_AddCustomRequestTables migration**: PostgreSQL schema
- **CustomOrderRepository**: DbContext-backed repository implementation
- DI wiring: ICustomOrderRepository + ICustomOrderService scoped registration

### API Layer
- **CustomOrderEndpoints**:
  - `POST /custom-orders` — Customer submit custom request
  - `GET /custom-orders` — Customer list own requests
  - `GET /custom-orders/{requestId}` — Customer view single request
  - `POST /admin/custom-orders/{requestId}/start` — Admin start progress
  - `POST /admin/custom-orders/{requestId}/deliver` — Admin mark delivered

## Verification

- `dotnet build src/Domain/` ✓ (0 errors)
- `dotnet build src/Application/` ✓ (0 errors)
- `dotnet build src/Infrastructure/` ✓ (0 errors)
- `dotnet build src/API/` ✓ (0 errors)

## Requirements Met

| Requirement | Status | Evidence |
|-------------|--------|----------|
| CUS-01: Customer submit custom script/macro requests with description | ✓ | POST /custom-orders endpoint + CustomRequest.Create factory |
| CUS-02: Admin update custom order status Pending→InProgress→Delivered | ✓ | Admin endpoints + status transition guards |

## Threat Mitigation (from Threat Model)

| Threat | Mitigation | Status |
|--------|-----------|--------|
| T-09-01: Client-controlled status tampering | ✓ | Status transitions enforced server-side only |
| T-09-02: Information disclosure | ✓ | CustomerId check in GetByIdAsync |
| T-09-03: Unauthorized admin status update | ✓ | AdminOnly policy on admin endpoints |

## Deviations from Plan

None - plan executed as specified.

## Auth Gates

None encountered.

## Self-Check

- [x] CustomRequestStatus.cs created with Pending=0, InProgress=1, Delivered=2
- [x] CustomRequest.cs created with factory, status transitions (53 lines)
- [x] ICustomOrderService interface with 5 methods implemented
- [x] CustomOrderEndpoints.cs created with customer + admin routes
- [x] Migration created for custom_requests table
- [x] All projects build without errors
- [x] Commits: 158bf18 (domain), f9d2ffb (API)

## Plan Completion

Plan 09-01 complete: Custom request domain model + lifecycle + API endpoints implemented, all verification passed.

**Next Plan:** 09-02 (Marketplace downloads with entitlement-based signed URL access)