# Phase 3: Inventory Integrity & Reservation Control - Context

**Gathered:** 2026-04-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Deliver inventory correctness controls so available stock stays accurate under concurrent demand: reserve stock for pending-payment orders, release reservations on cancellation/expiration, block oversell at checkout, and keep admin stock adjustments auditable.

</domain>

<decisions>
## Implementation Decisions

### Reservation Lifecycle
- **D-01:** Stock is reserved at checkout submit when a pending-payment order is created (not at add-to-cart).
- **D-02:** Reservation TTL is 15 minutes before automatic expiration/release.
- **D-03:** Reservation release is immediate when payment fails or order is canceled.
- **D-04:** Reservation quantity is tracked as per-product aggregate units per order line (no per-unit token model).

### Concurrency Control
- **D-05:** Oversell prevention uses atomic database decrement/reserve in a single transactional write with availability predicate.
- **D-06:** Failed reservation races return conflict semantics (HTTP 409) instead of generic validation failure.
- **D-07:** Reservation operations are idempotent by order intent key so client retries do not double-reserve.
- **D-08:** Concurrent admin stock writes must use optimistic concurrency token checks and explicit retry on stale updates.

### Availability Contract
- **D-09:** Inventory availability responses expose `available`, `reserved`, and `total` quantities.
- **D-10:** Checkout stock sufficiency is authoritative only at reservation transaction time; pre-checks are advisory.
- **D-11:** Quantity-overrun responses use ProblemDetails 409 and include actionable available-quantity detail.
- **D-12:** Phase 3 inventory reads use read-through database truth per request (no caching layer in this phase).

### Admin Stock Adjustments & Audit
- **D-13:** Admin adjustment input model is delta-only (`+/-` units), not absolute set.
- **D-14:** Negative resulting stock is blocked.
- **D-15:** Each stock adjustment audit record must include: admin identity, product identity, delta, before quantity, after quantity, reason, timestamp.
- **D-16:** Adjustment reason is required free-text.

### the agent's Discretion
- Exact persistence schema for reservation rows vs derived counters, as long as decisions above remain true.
- Exact idempotency key storage and retention window.
- Exact ProblemDetails code names/extension field names while preserving the locked status semantics.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase definition and requirement contracts
- `.planning/ROADMAP.md` - Phase 3 goal, dependency boundary, and success criteria.
- `.planning/REQUIREMENTS.md` - INV-01, INV-02, INV-03, INV-04 requirement contracts.
- `.planning/PROJECT.md` - non-negotiable architecture, stack, security, and reliability constraints.

### Prior locked context that constrains Phase 3
- `.planning/phases/01-identity-security-foundation/01-CONTEXT.md` - RBAC and secure contract patterns for protected admin writes.
- `.planning/phases/02-catalog-product-governance/02-CONTEXT.md` - global product model (no server segmentation) and API contract conventions.

### Existing code patterns and integration anchors
- `src/API/Program.cs` - minimal API composition, middleware order, and service registration baseline.
- `src/API/ErrorHandling/GlobalExceptionHandler.cs` - established ProblemDetails mapping behavior and conflict/validation semantics.
- `src/Application/Catalog/Services/CatalogService.cs` - application-service validation and repository orchestration pattern to mirror.
- `src/Infrastructure/Persistence/AppDbContext.cs` - current aggregate registration pattern for new inventory entities.
- `src/Infrastructure/Persistence/Configurations/ProductConfiguration.cs` - EF mapping/index conventions and relationship constraints.
- `src/Infrastructure/Persistence/Migrations/20260416180310_AddCatalogAndCategoryGovernance.cs` - migration style and transactional schema evolution baseline.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `src/API/Program.cs`: endpoint mapping, authz middleware, and service wiring patterns are already established.
- `src/API/ErrorHandling/GlobalExceptionHandler.cs`: shared exception-to-ProblemDetails pipeline can be reused for inventory conflicts and validation failures.
- `src/Application/Catalog/Services/CatalogService.cs`: input normalization, invariant validation, and repository transaction flow patterns are reusable for inventory use cases.
- `src/Infrastructure/Persistence/AppDbContext.cs`: central place to register new inventory entities and apply configurations.

### Established Patterns
- API layer uses route-group extension methods and contracts, not MVC controllers.
- Application layer owns orchestration and invariants; Infrastructure implements repository contracts.
- EF Core mappings use dedicated configuration classes and explicit migration files for schema changes.
- Error handling favors explicit domain/application exceptions translated to RFC7807 responses.

### Integration Points
- Add inventory and admin stock adjustment endpoints in API route groups with AdminOnly policy where required.
- Add reservation/availability/adjustment contracts and services in Application layer.
- Add stock/reservation domain entities and invariants in Domain layer.
- Add inventory repositories, EF configurations, and migration updates in Infrastructure.

</code_context>

<specifics>
## Specific Ideas

- Reservation locking should optimize correctness first; no cache in this phase.
- Client conflict responses should be actionable (include available quantity).
- Audit visibility is treated as an operational requirement, not optional telemetry.

</specifics>

<deferred>
## Deferred Ideas

None - discussion stayed within phase scope.

</deferred>

---

*Phase: 03-inventory-integrity-reservation-control*
*Context gathered: 2026-04-17*
