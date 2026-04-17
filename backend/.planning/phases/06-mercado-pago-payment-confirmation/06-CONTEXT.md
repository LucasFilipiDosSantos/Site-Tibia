# Phase 6: Mercado Pago Payment Confirmation - Context

**Gathered:** 2026-04-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Deliver Mercado Pago payment confirmation flow: create payment requests linked to exact order, validate webhook authenticity, process provider events idempotently, persist payment/webhook audit logs, and transition order from Pending to Paid only after verified approved/processed confirmation.

</domain>

<decisions>
## Implementation Decisions

### Payment Creation Contract
- **D-01:** Use Checkout Pro preference flow via Mercado Pago .NET SDK (backend creates preference, frontend consumes returned init payload).
- **D-02:** Use `orderId` as canonical provider binding (`external_reference = orderId`).
- **D-03:** Persist local payment-link record with `orderId`, `preferenceId`, expected amount snapshot, and expected currency snapshot for reconciliation.

### Webhook Trust Gate
- **D-04:** Webhook processing is strict fail-closed: invalid/missing signature never mutates payment/order state.
- **D-05:** Validate `x-signature` using Mercado Pago canonical manifest (`id:data.id` in lowercase + `x-request-id` + `ts`) and HMAC-SHA256 with app webhook secret.

### Idempotency and Event Ordering
- **D-06:** Dedupe key is provider resource identity plus action (`data.id/paymentId + action/type`) for processed-event idempotency.
- **D-07:** Duplicate webhook deliveries are idempotent no-ops for order lifecycle, with processing audit retained.
- **D-08:** Enforce monotonic status guard: ignore/log out-of-order regressions (no state rollback from later-arriving older events).

### Paid Transition Mapping
- **D-09:** Order becomes `Paid` only on verified provider `approved/processed` confirmation.
- **D-10:** `pending`, `in_process`, `authorized` keep order `Pending`.
- **D-11:** `rejected`, `cancelled`, `refunded`, invalid-signature, or unverified events never mark order `Paid`.
- **D-12:** If order already `Paid`, additional paid confirmations are lifecycle no-op with audit log only (no duplicate timeline event).

### Acknowledgement and Processing Flow
- **D-13:** Webhook endpoint acknowledges fast (`200/201`) after signature validation + minimal inbound log persistence; heavy processing runs async.
- **D-14:** Async processing uses Hangfire retries with backoff; terminal failures are marked dead-letter for admin inspection/replay path.

### the agent's Discretion
- Exact DTO names and endpoint route naming for payment creation and webhook intake.
- Exact persistence table split (inbound webhook log vs normalized payment status history), while preserving D-06 through D-14 semantics.
- Exact retry schedule/backoff intervals and dead-letter retention period.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase definition and contracts
- `.planning/ROADMAP.md` - Phase 6 goal, success criteria, and planned work split.
- `.planning/REQUIREMENTS.md` - PAY-01, PAY-02, PAY-03, PAY-04 requirement contracts.
- `.planning/PROJECT.md` - mandatory stack/integration constraints (`Mercado Pago`, `PostgreSQL`, `Clean Architecture`, reliability requirements).

### Upstream lifecycle invariants to preserve
- `.planning/phases/05-order-lifecycle-timeline-visibility/05-CONTEXT.md` - D-03 (system owns Paid), D-04/D-05 idempotent timeline behavior, D-15 conflict semantics.
- `.planning/phases/04-cart-checkout-capture/04-CONTEXT.md` - D-14 atomic no-partial-side-effects constraint.
- `.planning/phases/03-inventory-integrity-reservation-control/03-CONTEXT.md` - established idempotency/conflict semantics posture.

### Existing code anchors
- `src/Domain/Checkout/Order.cs` - legal state machine and idempotent transition behavior.
- `src/Application/Checkout/Services/OrderLifecycleService.cs` - system transition entrypoint to mark `Paid`.
- `src/Application/Checkout/Contracts/OrderLifecycleContracts.cs` - lifecycle conflict contract model.
- `src/API/Checkout/CheckoutEndpoints.cs` - checkout route group and current order-facing API conventions.
- `src/API/ErrorHandling/GlobalExceptionHandler.cs` - RFC7807 error/extension mapping style.
- `src/Infrastructure/Checkout/Repositories/OrderLifecycleRepository.cs` - append-only timeline persistence path.
- `src/Infrastructure/Persistence/AppDbContext.cs` - db-set registration pattern.
- `src/Infrastructure/DependencyInjection.cs` - service/repository binding conventions.

### External integration references
- `https://www.mercadopago.com/developers/en/docs/checkout-pro/create-payment-preference` - SDK preference creation contract and .NET examples.
- `https://www.mercadopago.com/developers/en/docs/checkout-api-payments/payment-notifications` - webhook setup and notification model.
- `https://www.mercadopago.com/developers/en/docs/checkout-api-payments/payment-notifications/notifications` - `x-signature` validation manifest (`id`, `request-id`, `ts`) and HMAC guidance.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Order` aggregate already enforces legal transitions and idempotent no-op for duplicate target status.
- `OrderLifecycleService.ApplySystemTransitionAsync` already provides system-owned Pending->Paid command path.
- `GlobalExceptionHandler` already supports typed conflict mappings with extensions.
- `OrderLifecycleRepository` already persists append-only status events and guards duplicate event persistence.

### Established Patterns
- Minimal API endpoint modules (`MapGroup` + extension methods), no MVC controllers.
- Application services own orchestration and invariant enforcement; infrastructure owns persistence/integration adapters.
- Conflict semantics exposed as explicit exceptions mapped to RFC7807 responses.
- Timeline/event behavior treated as append-only auditable history.

### Integration Points
- Add payment creation endpoint/service in checkout boundary (`API` + `Application`) linked to existing order model.
- Add Mercado Pago SDK adapter and webhook signature validator in `Infrastructure`.
- Add payment + webhook log persistence entities/configurations in `Infrastructure/Persistence`.
- Add webhook intake endpoint in `API` with fast ack and async dispatch to Hangfire job.
- Reuse `OrderLifecycleService` for verified paid transition.

</code_context>

<specifics>
## Specific Ideas

- Fail-closed trust model: authenticity check before any mutation.
- Paid means settled/confirmed only; no optimistic paid mapping from weak statuses.
- Retry-safe operations: provider retries must not duplicate transitions or logs beyond dedupe/audit intent.

</specifics>

<deferred>
## Deferred Ideas

None - discussion stayed within phase scope.

</deferred>

---

*Phase: 06-mercado-pago-payment-confirmation*
*Context gathered: 2026-04-17*
