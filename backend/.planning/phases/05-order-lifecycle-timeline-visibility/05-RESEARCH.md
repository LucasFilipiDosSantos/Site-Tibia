# Phase 05 Research — Order Lifecycle & Timeline Visibility

**Phase:** 05 — Order Lifecycle & Timeline Visibility  
**Date:** 2026-04-17  
**Requirements in scope:** ORD-01, ORD-02, ORD-03, ORD-04

## Standard Stack

- ASP.NET Core 10 Minimal APIs with route-group modules (customer and admin surfaces split by policy)
- Application-layer orchestration services and contracts for transition workflows
- Domain-centered lifecycle state machine inside `Domain.Checkout.Order` (no API-only transition validation)
- EF Core 10 + Npgsql persistence with append-only status event entity/table
- Existing RFC7807 global exception mapping for deterministic 409 state-conflict responses
- xUnit unit tests + integration tests (API + persistence path)

## Architecture Patterns

- Keep lifecycle invariants in Domain/Application (`D-01`, `D-02`, `D-03`, `D-04`), not in endpoint handlers.
- Represent status timeline as append-only event stream (`D-05`, `D-08`) with backend UTC commit timestamp (`D-07`).
- Preserve existing offset paging conventions (`page`, `pageSize`) for customer history list (`D-12`).
- Keep customer transport contracts stable and explicit: raw status code + display label (`D-11`).
- Use operation-specific admin transition commands (`D-14`) and explicit conflict payloads for illegal transitions (`D-15`).

## Locked Decision Translation (Context Fidelity)

- **D-01..D-04:** Legal transitions are enforced through a domain/application state machine; `Cancelled` only from `Pending`; `Paid` transitions remain system-owned; duplicate transitions to current status are idempotent no-op without event duplication.
- **D-05..D-08:** Status history event appended only on real change; event includes `fromStatus`, `toStatus`, `occurredAtUtc`, `sourceType`; timestamps are backend UTC generated at commit; events are immutable append-only.
- **D-09..D-12:** Customer history API provides paged list plus detail timeline; default sort `CreatedAtUtc DESC`; response includes stable code + display label; pagination uses existing offset contract.
- **D-13..D-16:** Admin search supports status/customer/date range; status management uses explicit transition actions, not generic set-status; illegal transitions return `409 ProblemDetails` with `currentStatus` + `allowedTransitions`; admin manual transitions require actor + reason in metadata.

## Don’t Hand-Roll

- Do not implement transition guards only at API level (`D-01` violation).
- Do not mutate existing timeline rows in correction flows (`D-08` violation).
- Do not append duplicate timeline events for duplicate target-state requests (`D-04`, `D-05` violation).
- Do not expose illegal transition as generic 400 — must be 409 with actionable state details (`D-15`).
- Do not provide generic admin set-status endpoint (`D-14` violation).

## Common Pitfalls

1. **Event duplication under retries** on idempotent requests.  
   Mitigation: no-op when target status equals current and skip append.
2. **Authority boundary drift** where admin/customer can mark orders as paid.  
   Mitigation: transition service enforces source-specific allowed transitions.
3. **Timeline mutability leakage** through repository update paths.  
   Mitigation: model events as append-only with no update/delete APIs.
4. **History API inconsistency** by sorting on transition timestamp instead of order creation.  
   Mitigation: default list sort on order `CreatedAtUtc` per `D-10`.
5. **Weak admin conflict diagnostics** without allowed transition hints.  
   Mitigation: structured conflict exception mapped to ProblemDetails extensions.

## Validation Architecture

- Unit tests:
  - Domain transition matrix legality and authority/source constraints (`D-01`, `D-02`, `D-03`)
  - Idempotent duplicate transition no-op and no extra event append (`D-04`, `D-05`)
  - Append-only event semantics with required metadata (`D-06`, `D-08`)
- Integration tests:
  - Customer history list/detail timeline contract with offset pagination and default ordering (`D-09`, `D-10`, `D-11`, `D-12`)
  - Admin search filters by status/customer/date range (`D-13`)
  - Admin explicit transition actions with required reason + actor capture (`D-14`, `D-16`)
  - Illegal transition returns `409 ProblemDetails` with `currentStatus` and `allowedTransitions` (`D-15`)
- Persistence verification:
  - EF migration introduces order status event storage and indexes for list/search filters
  - Blocking schema migration execution before final verification

## Research Outcome

No new external dependency is required. Phase 05 should be split into focused plans: (1) lifecycle domain/application contracts + transition matrix tests, (2) persistence/event history + migration, and (3) API endpoints for customer timeline and admin search/actions with end-to-end verification.
