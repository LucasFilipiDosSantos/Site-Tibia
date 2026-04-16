# Phase 2: Catalog & Product Governance - Context

**Gathered:** 2026-04-16
**Status:** Ready for planning

<domain>
## Phase Boundary

Deliver backend catalog discovery and product governance contracts so customers can browse products by category and slug, and admins can create/update product metadata and pricing safely.

</domain>

<decisions>
## Implementation Decisions

### Catalog Query Shape
- **D-01:** Customer browsing uses a single endpoint contract: `GET /products` with composable query filters.
- **D-02:** Filter combination uses AND semantics when multiple filters are provided.
- **D-03:** Product slug resolution is canonical via dedicated endpoint: `GET /products/{slug}`.

### Slug Rules
- **D-04:** Product slugs are globally unique across the catalog.
- **D-05:** Product slugs are immutable after creation.
- **D-06:** Attempts to change immutable product slug in update flows are rejected with validation error.

### Category and Server Taxonomy
- **D-07:** Catalog is global: products are not segmented by Tibia server and product model has no server field.
- **D-08:** Product type/category is modeled as a database `Category` entity (not enum/free text).
- **D-09:** Category slug is immutable after creation.
- **D-10:** Requirement CAT-01 server filter is intentionally deferred as a requirements/roadmap alignment follow-up because current product vision is "just products" without server segmentation.

### Admin Product Mutation Contract
- **D-11:** Admin product updates use PUT replace model (single full-update contract).
- **D-12:** Product price allows zero value.
- **D-13:** Product write contract references category by category slug.

### the agent's Discretion
- Exact pagination strategy for `GET /products` list responses (if needed by planner/research based on existing API conventions).
- Exact ProblemDetails error code naming while preserving validation semantics above.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase definition and acceptance
- `.planning/ROADMAP.md` - Phase 2 goal, dependency chain, and success criteria.
- `.planning/REQUIREMENTS.md` - CAT-01..CAT-04 contracts and traceability mapping.

### Project constraints and architecture guardrails
- `.planning/PROJECT.md` - stack, architecture, security, and reliability non-negotiables.
- `AGENTS.md` - enforceable Clean Architecture boundary constraints for layering.

### Prior locked context influencing this phase
- `.planning/phases/01-identity-security-foundation/01-CONTEXT.md` - prior RBAC/security decisions that carry into admin catalog governance.

### Existing implementation anchors
- `src/API/Auth/AuthPolicies.cs` - `AdminOnly` policy baseline for admin governance endpoints.
- `src/API/Program.cs` - API composition pattern and service wiring baseline.
- `src/Infrastructure/Persistence/AppDbContext.cs` - current persistence baseline (identity-only sets) indicating catalog domain starts in this phase.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `src/API/Program.cs`: established minimal API composition with shared middleware, auth, and DI bootstrap.
- `src/API/ErrorHandling/GlobalExceptionHandler.cs`: existing RFC7807/ProblemDetails handling path to reuse for catalog validation errors.
- `src/API/Auth/AuthPolicies.cs`: existing `AdminOnly` policy for product governance endpoints.

### Established Patterns
- API endpoints are defined via extension methods and route groups (`MapAuthEndpoints`) rather than MVC controllers.
- Layering follows `API -> Application -> Domain` with `Infrastructure` implementing contracts, consistent with AGENTS architecture guardrails.
- EF Core + PostgreSQL integration is centralized in `Infrastructure` (`AddInfrastructure`, `AppDbContext`, entity configurations).

### Integration Points
- Add customer catalog read endpoints and admin product write endpoints in API route groups.
- Introduce catalog/product/category contracts and use cases in Application layer.
- Introduce product/category domain entities and invariants in Domain layer.
- Extend `AppDbContext` and Infrastructure repositories/configurations for catalog persistence.

</code_context>

<specifics>
## Specific Ideas

- Catalog should be product-centric (no server-specific product split).
- Product/category slugs must stay stable to preserve consistent SEO-friendly access.
- Admin write flow should be strict and explicit (PUT replace with validation errors for immutable field edits).

</specifics>

<deferred>
## Deferred Ideas

- Server-scoped catalog filtering from CAT-01 is deferred for requirements alignment with product vision (global catalog only).

</deferred>

---

*Phase: 02-catalog-product-governance*
*Context gathered: 2026-04-16*
