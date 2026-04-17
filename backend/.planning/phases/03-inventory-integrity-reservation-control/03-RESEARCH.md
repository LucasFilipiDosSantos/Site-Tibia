# Phase 03 Research — Inventory Integrity & Reservation Control

**Phase:** 03 — Inventory Integrity & Reservation Control  
**Date:** 2026-04-17  
**Requirements in scope:** INV-01, INV-02, INV-03, INV-04

## Standard Stack

- ASP.NET Core 10 Minimal APIs (`MapGroup`) for inventory read/admin/write endpoints
- EF Core 10 + Npgsql 10 for transactional stock reservation and optimistic concurrency writes
- PostgreSQL transaction semantics (single-transaction reserve/release) for oversell prevention
- Existing RFC7807 path via `GlobalExceptionHandler` for deterministic 409/400 API contracts
- xUnit unit tests for domain/application invariants and integration tests for API contracts

## Architecture Patterns

- Keep new inventory bounded context aligned to existing layering:
  - `Domain/Inventory`: stock aggregate, reservation, adjustment audit invariants
  - `Application/Inventory`: orchestration contracts/services (availability, reserve, release, adjust)
  - `Infrastructure/Inventory`: EF configs + repositories + migrations
  - `API/Inventory`: DTOs and route mapping only
- Reservation lifecycle is order-intent keyed, not cart keyed (D-01, D-07).
- Checkout sufficiency remains advisory before reserve; authoritative guard occurs in reserve transaction (D-10).
- Admin stock adjustments are delta-based and audit-first, with optimistic concurrency retries on stale writes (D-08, D-13, D-15).

## Locked Decision Translation (Context Fidelity)

- D-01..D-04: Reserve on checkout submit with 15-minute TTL, immediate release on cancel/fail, aggregate per product line quantities.
- D-05..D-08: Use atomic availability predicate writes for reserve; return HTTP 409 on race conflicts; idempotent intent-key reserve path; optimistic concurrency with retry for admin stock updates.
- D-09..D-12: Availability contract exposes `{available, reserved, total}` per product; no cache layer in this phase; 409 responses include actionable available quantity detail.
- D-13..D-16: Admin adjustment uses signed delta only, blocks negative resulting stock, and writes complete audit record including required reason text.

## Don’t Hand-Roll

- Do not use in-memory locks or process-local mutexes for inventory correctness; enforce at database transaction level.
- Do not emit generic validation errors for reservation races; preserve conflict semantics with 409 and available-quantity detail (D-06, D-11).
- Do not model admin adjustment as absolute set in this phase; enforce delta-only input (D-13).
- Do not add cache/read replicas for stock reads in this phase (D-12).

## Common Pitfalls

1. **Double reservation under retry storms** when client repeats checkout submit.  
   Mitigation: unique idempotency key constraint on reservation intent + deterministic replay result (D-07).
2. **Partial reserve across multi-line order intents** causing phantom availability.  
   Mitigation: reserve all lines in one transaction; rollback entire intent if any line fails predicate (D-05).
3. **TTL expiry drift** from background-only release.  
   Mitigation: run expiry sweep in reserve/read/release entry points so release is automatic even without scheduler (D-02).
4. **Stale admin writes** overwriting fresh stock state.  
   Mitigation: EF optimistic concurrency token with bounded retry policy (D-08).
5. **Audit gaps** that omit before/after quantities or actor identity.  
   Mitigation: immutable adjustment-audit entity with required fields + integration assertions (D-15).

## Validation Architecture

- Unit tests:
  - Reservation idempotency by order intent key (D-07)
  - Reserve conflict detection and available-quantity contract mapping (D-06, D-11)
  - TTL expiry release behavior using controlled clock (D-02)
  - Admin delta adjustment invariants + negative-stock guard + reason required (D-13, D-14, D-16)
- Integration tests:
  - Availability endpoint returns `{available, reserved, total}` with live DB state (INV-01, D-09, D-12)
  - Reservation endpoint succeeds/fails deterministically under sufficiency/insufficiency and returns 409 ProblemDetails on conflict (INV-02, INV-03)
  - Release endpoint supports cancellation/failure path immediate release (D-03)
  - Admin adjustment endpoint enforces AdminOnly and writes audit rows with full required shape (INV-04, D-15)
- Schema verification:
  - EF migration generated for inventory tables and applied before verification run
  - Integration tests execute against migrated schema

## Research Outcome

Proceed with planning using current stack/patterns; no external dependencies required. The phase should be split into contract/domain, persistence/migration, and API/integration verification plans to keep context quality stable while implementing all locked decisions at full fidelity.
