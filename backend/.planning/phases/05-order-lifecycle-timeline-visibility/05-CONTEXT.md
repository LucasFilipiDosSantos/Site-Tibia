# Phase 5: Order Lifecycle & Timeline Visibility - Context

**Gathered:** 2026-04-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Deliver legal order lifecycle transitions and timeline visibility so orders move only through valid statuses (Pending, Paid, Cancelled), each status change is persisted as timestamped history, customers can view order history with timeline detail, and admins can search/manage orders by status and customer.

</domain>

<decisions>
## Implementation Decisions

### Lifecycle Rules
- **D-01:** Enforce status transitions with a domain/application state machine, not API-only checks.
- **D-02:** `Cancelled` is allowed only from `Pending` in Phase 5.
- **D-03:** Transition authority is split: system paths own `Paid` transitions; customer/admin can cancel only while `Pending`.
- **D-04:** Duplicate transition requests to the current status are idempotent no-ops and do not append duplicate history events.

### Timeline Events
- **D-05:** Append status history events only when status actually changes.
- **D-06:** Each history event stores `fromStatus`, `toStatus`, `occurredAtUtc`, and `sourceType` (`System`, `Admin`, `Customer`).
- **D-07:** Timeline timestamps use backend-generated UTC clock at transition commit.
- **D-08:** History is append-only and immutable; corrections happen via new events.

### Customer History API
- **D-09:** Provide customer order history as paged list plus order detail timeline (extend current order detail capability rather than detail-only).
- **D-10:** Customer order history default sort is newest-first by order `CreatedAtUtc`.
- **D-11:** Timeline/API status payload exposes stable raw status code plus display label.
- **D-12:** Customer history pagination uses existing offset contract (`page`, `pageSize`).

### Admin Search & Actions
- **D-13:** Phase 5 admin search must support filters for status, customer identifier, and created date range.
- **D-14:** Admin status management uses explicit transition actions (operation-specific commands), not generic set-status.
- **D-15:** Illegal transition attempts return `409 ProblemDetails` with actionable state conflict detail (including current status and allowed transitions).
- **D-16:** Manual admin transitions must record actor identity and required reason in audit/event metadata.

### the agent's Discretion
- Exact endpoint route naming for order history and admin transition actions, as long as contracts preserve D-09 through D-16.
- Exact persistence table/entity split for order status event history, as long as append-only immutability and required metadata are preserved.
- Exact display-label formatting/localization strategy, while raw status codes remain stable.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase definition and requirement contracts
- `.planning/ROADMAP.md` - Phase 5 goal, dependency boundary, and success criteria.
- `.planning/REQUIREMENTS.md` - ORD-01, ORD-02, ORD-03, ORD-04 requirement contracts and traceability.
- `.planning/PROJECT.md` - architecture, integration, security, and reliability non-negotiables.

### Prior locked context that constrains Phase 5
- `.planning/phases/01-identity-security-foundation/01-CONTEXT.md` - auth/RBAC baseline, including existing AdminOnly policy usage.
- `.planning/phases/02-catalog-product-governance/02-CONTEXT.md` - API contract and pagination conventions to preserve.
- `.planning/phases/03-inventory-integrity-reservation-control/03-CONTEXT.md` - conflict semantics and operational audit posture.
- `.planning/phases/04-cart-checkout-capture/04-CONTEXT.md` - existing order snapshot model and checkout-owned order detail baseline.

### Existing implementation anchors
- `src/API/Checkout/CheckoutEndpoints.cs` - current customer order detail endpoint and checkout route grouping style.
- `src/Application/Checkout/Services/CheckoutService.cs` - current order creation flow and invariants.
- `src/Application/Checkout/Contracts/CheckoutContracts.cs` - existing order and conflict contracts to extend safely.
- `src/Domain/Checkout/Order.cs` - order aggregate baseline where lifecycle state machine extension should anchor.
- `src/Infrastructure/Checkout/Repositories/CheckoutRepository.cs` - current order read/write repository baseline.
- `src/Infrastructure/Persistence/Configurations/OrderConfiguration.cs` - persistence mapping conventions for order aggregate.
- `src/Infrastructure/Persistence/AppDbContext.cs` - aggregate registration point for any new order history event entities.
- `src/API/ErrorHandling/GlobalExceptionHandler.cs` - canonical RFC7807 conflict/error mapping behavior.
- `src/API/Auth/AuthPolicies.cs` - existing admin authorization policy constants and registration.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `src/API/Checkout/CheckoutEndpoints.cs`: already exposes authenticated customer order detail retrieval and checkout route-group conventions.
- `src/API/ErrorHandling/GlobalExceptionHandler.cs`: established `409 ProblemDetails` mapping with extension payload patterns for conflict details.
- `src/API/Auth/AuthPolicies.cs`: existing `AdminOnly` and verified-customer policy primitives for secure order endpoints.
- `src/Infrastructure/Checkout/Repositories/CheckoutRepository.cs`: existing order aggregate loading with item + delivery includes.

### Established Patterns
- API uses minimal endpoint modules and route groups, not MVC controllers.
- Domain/Application layers own invariants and orchestration; API remains transport/composition.
- Conflict and illegal operation semantics are surfaced through explicit exceptions mapped to RFC7807 responses.
- List endpoints in the project already use offset paging contracts (`page`, `pageSize`).

### Integration Points
- Extend `Domain.Checkout.Order` (and related contracts) with lifecycle state machine + transition event recording.
- Add lifecycle/timeline services and repositories in `Application`/`Infrastructure` for state transitions and event persistence.
- Add customer order-history list + timeline-enabled detail responses under checkout/customer scope.
- Add admin order search and explicit transition action endpoints protected by admin policy.

</code_context>

<specifics>
## Specific Ideas

- Keep timeline signal high: status-change events only, immutable append-only history.
- Keep retry-safe behavior: duplicate transition intents resolve idempotently without timeline spam.
- Keep operator traceability strong: admin manual transitions always carry actor + reason.

</specifics>

<deferred>
## Deferred Ideas

None - discussion stayed within phase scope.

</deferred>

---

*Phase: 05-order-lifecycle-timeline-visibility*
*Context gathered: 2026-04-17*
