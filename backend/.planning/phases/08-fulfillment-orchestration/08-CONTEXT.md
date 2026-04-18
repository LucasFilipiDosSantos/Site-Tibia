# Phase 8: Fulfillment Orchestration - Context

**Gathered:** 2026-04-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Paid orders reach completion through tracked automated/manual fulfillment workflows. Delivery status tracked per order with delivery type and completion timestamp. Customer can view delivery progress. Admin can manually complete or correct fulfillment when automation fails.

</domain>

<decisions>
## Implementation Decisions

### Delivery Status Model
- **D-01:** Two-state model: `Pending` → `Completed` or `Failed`
- **D-02:** Completion recorded with `CompletedAtUtc` timestamp when status transitions to Completed
- **D-03:** Failed state captures failure reason for admin review

### Routing Trigger
- **D-04:** Routing happens within Phase 6 payment confirmation job immediately after order transitions to Paid
- **D-05:** Same transaction scope: payment confirmation + fulfillment routing as atomic unit
- **D-06:** If routing fails, job fails with retry (not separate scheduling)

### Automated Fulfillment Logic
- **D-07:** Automated fulfillment marks items as Completed immediately on Paid transition
- **D-08:** For instant digital goods (gold, items, Tibia Coins): immediate completion
- **D-09:** DeliveryInstruction fields (TargetCharacter, TargetServer, DeliveryChannelOrContact) captured at checkout are used for fulfillment

### Admin Correction Actions
- **D-10:** Admin can force-complete any Pending or Failed delivery
- **D-11:** Force-complete adds admin note and sets CompletedAtUtc
- **D-12:** Admin-only: no customer-facing delivery cancellation (orders are final)

### Customer Delivery Visibility
- **D-13:** Customer sees per-item delivery status in order detail: Pending → Completed
- **D-14:** Display shows: delivery status, fulfillment type (Automated/Manual), and CompletedAtUtc timestamp when available
- **D-15:** Status timeline events appended for delivery status changes

### Agent Discretion
- Exact entity persistence structure for delivery status (separate table or extended DeliveryInstruction)
- Exact delivery status enum values (Pending, Completed, Failed) — can use consistent naming
- Exact DTO field names for customer delivery visibility endpoint
- Exact service/repository method signatures for fulfillment orchestration

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Requirements
- `.planning/ROADMAP.md` § Phase 8 — FUL-01, FUL-02, FUL-03, FUL-04
- `.planning/REQUIREMENTS.md` — Fulfillment requirements

### Upstream Context
- `.planning/phases/06-mercado-pago-payment-confirmation/06-CONTEXT.md` — Payment confirmation (routing trigger here)
- `.planning/phases/05-order-lifecycle-timeline-visibility/05-CONTEXT.md` — Order lifecycle, timeline events pattern
- `.planning/phases/04-cart-checkout-capture/04-CONTEXT.md` — DeliveryInstruction captured at checkout

### Existing Code Anchors
- `src/Domain/Checkout/FulfillmentType.cs` — Automated, Manual enum
- `src/Domain/Checkout/DeliveryInstruction.cs` — existing delivery instruction model
- `src/Application/Checkout/Services/OrderLifecycleService.cs` — system transition entrypoint
- `src/API/Checkout/CheckoutEndpoints.cs` — existing order detail endpoint

[If no external specs: "No external specs — requirements fully captured in decisions above"]

</canonical_refs>

  [@code_context]
## Existing Code Insights

### Reusable Assets
- `FulfillmentType` enum (Automated, Manual) already exists
- `DeliveryInstruction` captures target character, server, channel at checkout
- `OrderLifecycleService.ApplySystemTransitionAsync` for system-owned transitions
- Append-only timeline event pattern from Phase 5

### Established Patterns
- Order lifecycle state machine for status transitions
- Timeline events for audit/history
- Admin actions with actor and reason
- RFC7807 error responses via GlobalExceptionHandler

### Integration Points
- Extend `DeliveryInstruction` or add delivery status entity
- Add fulfillment routing call within `PaymentWebhookProcessor` or chained job
- Add customer delivery visibility to existing order detail endpoint
- Add admin correction endpoint with admin-only policy

</code_context>

<specifics>
## Specific Ideas

- Keep it simple: automated = instant completion for v1
- Manual fulfillment (characters, gold) — admin force-completes when done
- Customer always sees status + timestamp + method for full transparency

</specifics>

<deferred>
## Deferred Ideas

- Delivery retry automation for Failed items — can add in Phase 9 or later
- Automated status polling for external system integration — v1 does instant completion only
- Delivery cancellation/refund flow — order-level concern, not delivery-level

</deferred>

---

*Phase: 08-fulfillment-orchestration*
*Context gathered: 2026-04-18*