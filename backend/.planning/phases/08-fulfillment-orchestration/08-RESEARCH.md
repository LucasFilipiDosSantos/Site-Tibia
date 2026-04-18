# Phase 08 Research — Fulfillment Orchestration

**Phase:** 08 — Fulfillment Orchestration  
**Date:** 2026-04-18  
**Requirements in scope:** FUL-01, FUL-02, FUL-03, FUL-04

## Standard Stack

- ASP.NET Core 10 Minimal APIs with route-group endpoint modules
- Clean Architecture flow (`API -> Application -> Domain`, `Infrastructure -> Application/Domain`)
- EF Core 10 + Npgsql for transactional persistence
- Hangfire for scheduled fulfillment automation and retry jobs (from Phase 7)
- Existing OrderLifecycleService for system transitions (from Phase 5/6)
- xUnit unit/integration tests + deterministic RFC7807 contracts

## Architecture Patterns

- Keep delivery status in the Order aggregate or linked DeliveryInstruction — no separate saga/process.
- Automated fulfillment routes at payment confirmation time (D-04), same transaction scope (D-05).
- Use two-state delivery status: Pending → Completed or Failed (D-01).
- Admin force-complete captures actor identity + reason, appends timeline event (D-10, D-11, D-15).
- Customer delivery visibility extends existing order detail endpoint, includes per-item status + timestamp + method (D-13, D-14).

## Locked Decision Translation (Context Fidelity)

- **D-01, D-02, D-03:** DeliveryInstruction gets DeliveryStatus enum (Pending, Completed, Failed) and CompletedAtUtc timestamp. Failed includes failure reason string.
- **D-04, D-05, D-06:** Fulfillment routing called within PaymentWebhookProcessor job after Paid transition succeeds. Same transaction scope guarantees atomicity. Job failure triggers Hangfire retry, not separate scheduling.
- **D-07, D-08, D-09:** Automated fulfillment (gold, items, Tibia Coins) marks items Completed immediately. DeliveryInstruction fields used for fulfillment execution.
- **D-10, D-11, D-12:** Admin force-complete endpoint sets status to Completed, records AdminNote, sets CompletedAtUtc. No customer cancellation — orders are final.
- **D-13, D-14, D-15:** Customer order detail includes per-item: DeliveryStatus, FulfillmentType (Automated/Manual), CompletedAtUtc when available. Timeline events appended for status changes.

## Don'T Hand-Roll

- Do not create separate delivery workflow orchestration service when DeliveryInstruction can hold status inline (D-01).
- Do not schedule fulfillment routing as separate background job when it belongs in payment confirmation transaction (D-04, D-05).
- Do not expose delivery status without fulfillment type and timestamp — incomplete for customer (D-14).
- Do not allow customer delivery cancellation — not in scope, orders are final (D-12).

## Common Pitfalls

1. **Delivery status not persisted with order.**  
   Mitigation: Add DeliveryStatus field to DeliveryInstruction entity, persist in same transaction as Paid transition.
2. **Missing timestamp for completed deliveries.**  
   Mitigation: Record CompletedAtUtc when status becomes Completed, display in API response (D-02, D-14).
3. **Admin force-complete not capturing actor/reason.**  
   Mitigation: Require admin note in request, record actor from auth context, append timeline event (D-11, D-15).
4. **Customer看不到 delivery method.**  
   Mitigation: Include FulfillmentType in response, not just status (D-14).
5. **Fulfillment routing not atomic with payment.**  
   Mitigation: Same DbContext transaction scope, rollback on failure (D-05).

## Validation Architecture

- Unit tests:
  - DeliveryStatus enum transitions (Pending→Completed, Pending→Failed) (D-01)
  - CompletedAtUtc set only on Completed transition (D-02)
  - Admin force-complete validation (D-10, D-11)
- Integration tests:
  - Payment confirmation triggers fulfillment routing (D-04, D-05)
  - Automated fulfillment completes instantly for digital goods (D-07, D-08)
  - Admin force-complete persists note and timestamp (D-11)
  - Customer order detail shows delivery status + type + timestamp (D-13, D-14)
- Persistence verification:
  - Migration adds DeliveryStatus and CompletedAtUtc to DeliveryInstruction table
  - Add index on (OrderId, ProductId) for delivery lookup performance

## Security and Threat Focus

- Trust boundary: Admin force-complete → delivery status mutation.
- ASVS L1 controls focus: authorization for admin-only action, input validation for note/reason, audit trail via timeline.
- Block-high policy: missing actor/reason capture on admin force-complete is planning defect.

## Research Outcome

Phase 08 should execute in three dependent plans:
1. Delivery status model + persistence (DeliveryStatus enum, CompletedAtUtc, failure reason)
2. Fulfillment routing at payment confirmation + automated completion logic
3. Customer delivery visibility API + admin correction endpoint + timeline integration