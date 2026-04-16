# Phase 02 Research — Catalog & Product Governance

**Phase:** 02 — Catalog & Product Governance  
**Date:** 2026-04-16  
**Requirements in scope:** CAT-01, CAT-02, CAT-03, CAT-04

## Standard Stack

- ASP.NET Core Minimal APIs (`MapGroup`) for endpoint mapping parity with Phase 01
- EF Core 10 + Npgsql 10 for relational persistence and transactional updates
- Domain/Application/Infrastructure layering with API-only transport concerns
- xUnit unit + integration tests (WebApplicationFactory) for contract verification

## Architecture Patterns

- Keep catalog as a new bounded context under existing modular monolith structure:
  - `Domain/Catalog`: entities + invariants (slug immutability, relationship guardrails)
  - `Application/Catalog`: use-case contracts/services (filters, pagination, admin mutations)
  - `Infrastructure/Catalog`: EF configurations + repositories + migration
  - `API/Catalog`: DTOs, route groups, authorization wiring
- Preserve existing API composition pattern from `Program.cs` + route extension methods.
- Reuse global RFC7807 exception mapping (`GlobalExceptionHandler`) for validation/operation errors.

## Locked Decision Translation (Context Fidelity)

- D-01..D-05: `GET /products` must support composable query filters (`category`, `slug`) with AND semantics + offset pagination (`page`, `pageSize`).
- D-03: canonical product lookup via `GET /products/{slug}`.
- D-06..D-08: product slug globally unique + immutable; update attempts rejected as validation errors.
- D-09..D-13: no server field in product model; category modeled as DB entity with immutable slug, admin-manageable lifecycle, and delete blocked when products reference it.
- D-15..D-18: admin product update contract uses PUT replace semantics, accepts zero price, category referenced by category slug, and unknown category slug returns 400 validation.
- D-14: phase must include requirements/roadmap alignment artifact updates to reflect global catalog model overriding CAT-01 server filtering assumption.

## Don’t Hand-Roll

- Do not encode category as enum/string literal in product rows; use relational `Category` table + FK.
- Do not place catalog business rules inside API handlers; enforce through Domain/Application services.
- Do not allow partial PATCH-like behavior for product updates in this phase; PUT replace only (D-15).
- Do not mutate slugs once persisted; reject attempts deterministically (D-07, D-08, D-11).

## Common Pitfalls

1. **Slug collision drift** between DB and API validation.  
   Mitigation: unique index on `product.slug` and application pre-check for explicit 400/409 mapping.
2. **Category deletion orphaning products.**  
   Mitigation: application guard + FK restriction and explicit domain error.
3. **Silent scope conflict with CAT-01 legacy wording.**  
   Mitigation: update requirements/roadmap alignment artifacts per D-14 in same phase.
4. **Filter semantics regression** (OR instead of AND).  
   Mitigation: integration tests covering combined filters and slug/category coexistence.
5. **Pagination inconsistency** between API and data access layer.  
   Mitigation: standardize `page >= 1`, bounded `pageSize`, deterministic ordering (`CreatedAtUtc DESC`, fallback `Id`).

## Validation Architecture

- Unit tests: domain invariants (slug immutability, zero-price validity, category delete guard conditions).
- Integration tests: customer catalog read contracts (`GET /products`, `GET /products/{slug}`), admin governance contracts (create/update category/product, authz), and D-14 alignment assertion on docs updates.
- Schema verification: migration generated + `dotnet ef database update` executed before plan-level verification.
- Security verification: admin routes protected by `AdminOnly`; unprivileged and unauthenticated probes return forbidden/unauthorized.

## Research Outcome

Proceed with implementation planning using existing stack/patterns (Level 0 discovery with explicit phase research requested). No new external dependencies are required.
