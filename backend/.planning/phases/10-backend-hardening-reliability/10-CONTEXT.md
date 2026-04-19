# Phase 10: backend-hardening-reliability - Context

**Gathered:** 2026-04-19
**Status:** Ready for planning

<domain>
## Phase Boundary

Close unresolved backend P0/P1 hardening gaps from milestone carryover by implementing reliable notification automation wiring, deterministic notification contact sourcing, deployed-environment HTTPS/HSTS verification evidence, and evidence-gated audit/observability closure for reliability/security requirements.

This phase is backend-only and does not introduce new product capabilities.

</domain>

<decisions>
## Implementation Decisions

### Lifecycle auto-enqueue
- **D-01:** Automatic WhatsApp notification enqueue triggers on `Order Paid`, `Delivery Completed`, and `Delivery Failed` events.
- **D-02:** Enqueue idempotency key is `OrderId + EventType + StatusAtUtc` to dedupe duplicates while preserving legitimate later events.
- **D-03:** Auto-enqueue orchestration belongs in Application lifecycle services (transition orchestration paths), not API endpoints or persistence hooks.
- **D-04:** If enqueue fails after a lifecycle transition succeeds, business transition is not rolled back; failure is persisted as retryable signal/outbox state.
- **D-05:** Existing manual notification trigger endpoints remain as admin fallback/replay path, not the primary execution path.
- **D-06:** Delivery notifications are per-order aggregate, not per-item fan-out.

### Notification contact source
- **D-07:** Canonical notification phone source is the customer profile phone (normalized/validated account-level value).
- **D-08:** Selected phone is snapshotted immutably on order notification metadata at checkout/order creation time for deterministic replay/audit.
- **D-09:** Missing profile phone does not block checkout; order proceeds with explicit `notification unavailable/missing-contact` state and later retry path.
- **D-10:** Phone numbers are persisted in canonical E.164 format; invalid numbers are rejected at phone-set/update boundaries.

### HTTPS/HSTS proof contract
- **D-11:** SEC-01 runtime proof executes in staging with production-like ingress/TLS termination topology.
- **D-12:** Required verification checks are: HTTP->HTTPS redirect, HSTS header on HTTPS responses, and absence of publicly reachable insecure HTTP endpoints.
- **D-13:** Proof artifacts are evidence-based: CI smoke output plus dated artifact folder containing requests/responses/headers and environment stamp, then linked from verification artifacts.

### Reliability and observability closure gate
- **D-14:** REL-02 mandatory correlation coverage spans full chain: Payment -> Order -> Fulfillment -> Notification.
- **D-15:** ADM-02 closure requires representative critical admin write coverage (product/stock/order mutation paths plus webhook-inspection actions) with integration assertions for actor/action/entity/before-after metadata.
- **D-16:** REL-01/REL-02 status transitions to complete only through evidence-gated checklist entries (tests + telemetry assertions + artifact links), never narrative-only updates.
- **D-17:** Observability closure includes failure-path assertions (invalid signature, dedupe duplicate, notification retry exhaustion), not only happy-path telemetry.

### the agent's Discretion
- Exact implementation shape for enqueue outbox/retry persistence (table/entity naming and retention), provided D-01 through D-06 remain true.
- Exact API/DTO naming for notification fallback/replay operational endpoints, provided role boundaries and auditability remain intact.
- Exact metric/log field naming conventions, provided cross-hop correlation and required evidence gating remain intact.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase scope and carryover mandate
- `.planning/ROADMAP.md` - Phase 10 goal/dependency anchor and milestone context.
- `.planning/REQUIREMENTS.md` - REL-01, REL-02, SEC-01, SEC-02, ADM-02 requirement contracts.
- `.planning/PROJECT.md` - backend-only, Clean Architecture, reliability/security non-negotiables.
- `.planning/quick/260418-tlr-complete-current-milestone-and-start-nex/260418-tlr-gap-register.md` - authoritative P0/P1 carryover list and closure intent.

### Prior-phase context that constrains implementation
- `.planning/phases/07-async-processing-notifications-monitoring/07-CONTEXT.md` - existing notification channel, retry strategy, and monitoring decisions.
- `.planning/phases/07-async-processing-notifications-monitoring/07-03-SUMMARY.md` - known issue baseline (manual trigger + missing phone wiring).
- `.planning/phases/06-mercado-pago-payment-confirmation/06-CONTEXT.md` - webhook trust/idempotency and async processing guardrails.
- `.planning/phases/05-order-lifecycle-timeline-visibility/05-CONTEXT.md` - legal lifecycle transitions and timeline idempotency constraints.
- `.planning/phases/01-identity-security-foundation/01-CONTEXT.md` - security boundary and role-policy baseline.
- `.planning/phases/01-identity-security-foundation/01-VERIFICATION.md` - SEC-01 human-needed verification gap definition.
- `.planning/phases/01-identity-security-foundation/01-HUMAN-UAT.md` - pending HTTPS/HSTS runtime validation checkpoint.

### Existing code anchors for this phase
- `src/API/Program.cs` - endpoint wiring, middleware ordering, health checks, Hangfire dashboard mapping.
- `src/API/Auth/HttpsSecurityExtensions.cs` - current `UseHttpsRedirection` and `UseHsts` behavior.
- `src/Application/Checkout/Services/CheckoutService.cs` - order creation and delivery-instruction flow where notification metadata snapshot context can be integrated.
- `src/Domain/Checkout/DeliveryInstruction.cs` - existing contact-related fields and fulfillment status transitions.
- `src/Infrastructure/Notifications/NotificationJobs.cs` - current notification job contracts/retry policy.
- `src/API/Jobs/NotificationJobEndpoints.cs` - manual fallback endpoints retained as admin replay path.
- `src/Infrastructure/Notifications/WhatsAppNotificationService.cs` - channel adapter behavior/logging.
- `src/API/Payments/PaymentWebhookEndpoints.cs` - webhook ingest + async enqueue pattern.
- `src/Application/Payments/Services/PaymentWebhookProcessor.cs` - idempotent processing and status transition coupling.
- `src/API/Admin/AdminAuditEndpoints.cs` - audit query surface used for evidence closure.
- `src/API/Admin/AdminWebhookLogEndpoints.cs` - webhook inspection surface tied to ADM-03/ADM-02 evidence alignment.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `src/Infrastructure/Notifications/NotificationJobs.cs`: already defines Hangfire retry schedule and typed job args; can be reused for automatic lifecycle enqueue payloads.
- `src/API/Payments/PaymentWebhookEndpoints.cs`: established fast-ack + background job pattern for critical async processing.
- `src/Application/Payments/Services/PaymentWebhookProcessor.cs`: existing idempotency and monotonic status handling patterns suitable for notification enqueue dedupe discipline.
- `src/API/Auth/HttpsSecurityExtensions.cs`: current HTTPS/HSTS middleware hook, providing base for runtime proof criteria.
- `src/API/Admin/AdminAuditEndpoints.cs` and `src/API/Admin/AdminWebhookLogEndpoints.cs`: operational visibility endpoints for evidence collection.

### Established Patterns
- Lifecycle-critical operations are application-service orchestrated with idempotent guards and conflict-safe behavior.
- Async work uses Hangfire with explicit retry strategy; operator fallback endpoints already exist.
- API layer remains transport/composition; business orchestration belongs in Application; persistence/integration concerns remain in Infrastructure.
- Reliability/security requirement closure has shown drift risk when not tied to executable evidence.

### Integration Points
- Add automatic notification enqueue hooks in lifecycle transition paths (order paid + delivery status updates).
- Extend checkout/order notification metadata persistence with immutable phone snapshot and missing-contact state markers.
- Add/extend verification artifacts pipeline for SEC-01 staging proof and REL-01/REL-02 evidence-gated closure.
- Expand integration-level telemetry/audit assertions across payment->order->fulfillment->notification path including failure scenarios.

</code_context>

<specifics>
## Specific Ideas

- Keep commerce transitions resilient: notification failures must not roll back lifecycle state.
- Preserve deterministic operations: replay and incident forensics depend on immutable contact snapshot and correlation continuity.
- Treat requirement closure as evidence, not narrative: every completion claim must include executable proof links.

</specifics>

<deferred>
## Deferred Ideas

- Frontend delivery-status UX refinements (out of backend-only phase scope).
- Advanced anti-fraud heuristics and analytics dashboards (v2 scope unless they become hard blockers).
- Broader system-wide "audit every write" expansion beyond representative critical paths (potential future phase).

</deferred>

---

*Phase: 10-backend-hardening-reliability*
*Context gathered: 2026-04-19*
