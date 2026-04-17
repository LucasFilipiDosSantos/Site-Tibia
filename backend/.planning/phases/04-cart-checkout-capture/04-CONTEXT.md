# Phase 4: Cart & Checkout Capture - Context

**Gathered:** 2026-04-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Deliver cart operations and checkout order creation so authenticated customers can add/update/remove cart items, submit checkout, create orders with immutable item price snapshots, and capture fulfillment-ready delivery instructions.

</domain>

<decisions>
## Implementation Decisions

### Cart line behavior
- **D-01:** Re-adding the same product merges into a single cart line and increments quantity.
- **D-02:** Cart quantity updates use absolute-set semantics (client sends desired final quantity).
- **D-03:** Add/update requests that exceed available stock are rejected with HTTP 409 conflict semantics.
- **D-04:** Cart line deletion uses an explicit remove endpoint (not quantity-zero shorthand).

### Delivery instructions contract
- **D-05:** Checkout captures delivery instructions using structured payloads by fulfillment type.
- **D-06:** Delivery instructions are conditionally required based on product fulfillment path.
- **D-07:** Automated fulfillment requires target character, target server/world, and delivery channel/contact.
- **D-08:** Manual fulfillment requires a free-text request brief plus contact handle.

### Order snapshot payload
- **D-09:** Checkout freezes item `unitPrice`, `currency`, `productName`, `productSlug`, and `categorySlug` into immutable order-item snapshots.
- **D-10:** Money snapshots persist decimal amount plus explicit currency code.
- **D-11:** Snapshot fields are fully immutable after order creation.
- **D-12:** Historic order read models always return stored snapshot values, never live catalog joins.

### Reservation and checkout handshake
- **D-13:** Keep Phase 3 reservation timing unchanged: reserve stock only on checkout submit (never at add-to-cart).
- **D-14:** Checkout is atomic across lines: any reservation conflict fails the entire checkout and no partial order is created.
- **D-15:** Reservation conflicts return ProblemDetails 409 with per-line available quantity details.
- **D-16:** Successful checkout immediately clears the cart.

### the agent's Discretion
- Exact cart entity persistence strategy (single table vs normalized line table structure) while preserving D-01 through D-04.
- Exact ProblemDetails extension key names for per-line conflict payloads while preserving D-15 semantics.
- Exact DTO naming and endpoint URI conventions, as long as API contracts preserve locked behaviors above.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase definition and requirement contracts
- `.planning/ROADMAP.md` - Phase 4 goal, dependencies, and success criteria.
- `.planning/REQUIREMENTS.md` - CHK-01, CHK-02, CHK-03 requirement contracts and traceability.
- `.planning/PROJECT.md` - global constraints for stack, architecture, integrations, and reliability.

### Prior locked decisions that constrain Phase 4
- `.planning/phases/01-identity-security-foundation/01-CONTEXT.md` - auth/RBAC baseline for authenticated checkout operations.
- `.planning/phases/02-catalog-product-governance/02-CONTEXT.md` - catalog model, immutable slugs, and product/category semantics used in snapshots.
- `.planning/phases/03-inventory-integrity-reservation-control/03-CONTEXT.md` - reservation timing, TTL, idempotency, and 409 conflict contract carried into checkout.

### Existing implementation anchors
- `src/API/Program.cs` - endpoint registration and middleware composition baseline.
- `src/API/ErrorHandling/GlobalExceptionHandler.cs` - shared RFC7807 mapping and inventory conflict extension pattern.
- `src/API/Catalog/CatalogEndpoints.cs` - route grouping and minimal API endpoint style conventions.
- `src/API/Inventory/InventoryEndpoints.cs` - existing reservation endpoint contracts and release semantics.
- `src/Application/Catalog/Services/CatalogService.cs` - validation and orchestration style for application services.
- `src/Application/Inventory/Services/InventoryService.cs` - reservation idempotency/conflict behavior and stock checks to integrate with checkout.
- `src/Application/Inventory/Contracts/InventoryContracts.cs` - canonical reservation DTO contracts used by checkout.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `src/API/Inventory/InventoryEndpoints.cs`: existing reservation and release endpoints already model order-intent-key-driven reservation lifecycle.
- `src/Application/Inventory/Services/InventoryService.cs`: enforces reservation validation, conflict exceptions, and TTL behavior reusable by checkout flow.
- `src/API/ErrorHandling/GlobalExceptionHandler.cs`: maps domain/application exceptions to RFC7807 and already carries conflict detail extensions.

### Established Patterns
- API uses minimal endpoint extension modules and route groups, not MVC controllers.
- Application services own validation/orchestration and throw explicit exceptions for API-level mapping.
- Conflict semantics in this codebase are explicit 409 ProblemDetails with actionable details, not silent clamping.

### Integration Points
- Add cart and checkout endpoint group(s) in `src/API` and map them in `src/API/Program.cs`.
- Add cart/checkout contracts and services in `src/Application` that orchestrate catalog reads + inventory reservation.
- Add order/cart domain models and invariants in `src/Domain` for immutable snapshots and delivery instructions.
- Add persistence mappings and repositories in `src/Infrastructure` for cart state and checkout-created orders.

</code_context>

<specifics>
## Specific Ideas

- Keep checkout deterministic and fail-fast: no partial checkout success when stock conflicts occur.
- Keep history trustworthy: order snapshots are immutable and become the source of truth for historic reads.

</specifics>

<deferred>
## Deferred Ideas

None - discussion stayed within phase scope.

</deferred>

---

*Phase: 04-cart-checkout-capture*
*Context gathered: 2026-04-17*
