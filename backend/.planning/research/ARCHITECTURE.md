# Architecture Research

**Domain:** Tibia virtual goods commerce backend  
**Updated:** 2026-04-14  
**Intent:** Keep layer boundaries strict and aligned with `backend.slnx`

## Solution Shape (matches `backend.slnx`)

```text
src/
  API/             # HTTP entrypoint, DI composition root
  Application/     # Use cases, orchestration, ports/interfaces
  Domain/          # Entities, value objects, domain rules/events
  Infrastructure/  # EF Core, adapters, external providers, background jobs
tests/
  UnitTests/       # Domain + Application unit tests
```

## Dependency Rule (Clean Architecture)

```text
API -> Application
Infrastructure -> Application
Application -> Domain
Domain -> (no project references)
UnitTests -> Domain, Application
```

Allowed direction is inward only. No reverse references.

## Layer Responsibilities

| Layer | Owns | Must not own |
|---|---|---|
| API | Controllers/endpoints, request validation, auth, DTO mapping, middleware, DI wiring | Business rules, persistence logic, provider SDK calls directly |
| Application | Commands/queries/use cases, transactions, interfaces (`IRepository`, `IPaymentGateway`, `IClock`) | EF entities/configuration, HTTP concerns, framework-heavy code |
| Domain | Aggregates, invariants, value objects, domain events | Database/ORM code, external service calls, controller DTOs |
| Infrastructure | DbContext, repository implementations, migrations, Mercado Pago/WhatsApp clients, Hangfire jobs | Business decision logic that belongs in Domain/Application |
| UnitTests | Domain invariants + Application behavior in isolation | End-to-end infra verification (keep those in future integration tests) |

## Practical Boundary Rules

1. Put interfaces in `src/Application`; implementations in `src/Infrastructure`.
2. Keep domain models free of ASP.NET Core, EF Core, and SDK types.
3. API only calls Application use cases; never `DbContext` directly.
4. Infrastructure never references `src/API`.
5. Use DTOs/contracts in API/Application; do not expose domain entities over HTTP.
6. Run async side effects (notifications, retries, reconciliation) from Infrastructure jobs that invoke Application use cases.

## Request + Job Flow

1. `API` receives request/webhook and validates input.
2. `Application` executes use case and coordinates transaction.
3. `Domain` enforces invariants/state transitions.
4. `Infrastructure` persists changes and handles provider communication through adapters.
5. Background processing (Hangfire) lives in `Infrastructure`, but business decisions stay in `Application`/`Domain`.

## Short Checklist for New Code

- New business rule? Add to `src/Domain`.
- New use case/orchestration? Add to `src/Application`.
- New endpoint/auth/middleware? Add to `src/API`.
- New DB/provider implementation? Add to `src/Infrastructure`.
- New unit test for rule/use case? Add to `tests/UnitTests`.

---
*Concise architecture baseline for this repository structure.*
