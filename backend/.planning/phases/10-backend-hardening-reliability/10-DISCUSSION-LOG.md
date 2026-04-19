# Phase 10: backend-hardening-reliability - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-19T10:48:22-03:00
**Phase:** 10-backend-hardening-reliability
**Areas discussed:** Lifecycle auto-enqueue, Notification contact source, HTTPS/HSTS proof contract, Reliability observability gate

---

## Lifecycle auto-enqueue

| Option | Description | Selected |
|--------|-------------|----------|
| Paid + delivery events | Enqueue on order Paid, delivery Completed, and delivery Failed. Covers critical lifecycle communication with current domain events. | ✓ |
| Paid only | Minimal wiring now; defer delivery notifications to later. | |
| All state changes | Also enqueue for created/pending/cancelled. Broader visibility but noisier and higher spam risk. | |

**User's choice:** Paid + delivery events
**Notes:** Selected as baseline trigger matrix.

| Option | Description | Selected |
|--------|-------------|----------|
| OrderId+event+attempt | Allows repeated notifications per retry cycle; weaker dedupe across duplicates. | |
| OrderId+event+statusAtUtc | Stable per concrete lifecycle event instance; dedupes duplicate deliveries while allowing later distinct events. | ✓ |
| OrderId only | Over-dedupes and can suppress legitimate later lifecycle notifications. | |

**User's choice:** OrderId+event+statusAtUtc
**Notes:** Locked as enqueue dedupe key shape.

| Option | Description | Selected |
|--------|-------------|----------|
| Application lifecycle services | Emit enqueue requests where transitions are applied (OrderLifecycleService + fulfillment update path), keeping API thin and preserving layer boundaries. | ✓ |
| API endpoints | Hook enqueue at transport layer; simpler initial wiring but can miss non-HTTP transition paths. | |
| Infrastructure repository hooks | Trigger from persistence layer; risks hidden side effects and harder reasoning/testing. | |

**User's choice:** Application lifecycle services
**Notes:** Keeps orchestration in Application layer.

| Option | Description | Selected |
|--------|-------------|----------|
| Persist outbox/retry signal + continue | Do not roll back business transition; record failure with retry path and observability signal. | ✓ |
| Fail transition transaction | Strong consistency but can block core order/delivery progress due to notification channel issues. | |
| Fire-and-forget log only | Simplest but risks silent notification loss. | |

**User's choice:** Persist outbox/retry signal + continue
**Notes:** Transition remains authoritative; notification failure is retriable.

| Option | Description | Selected |
|--------|-------------|----------|
| Keep as admin fallback | Retain `/jobs/notifications/*` for replay/ops intervention, but not as primary path. | ✓ |
| Remove them | Force all notifications through automatic path only. | |
| Keep and equal priority | Allow both paths equally; increases duplication/operational ambiguity. | |

**User's choice:** Keep as admin fallback
**Notes:** Manual endpoints remain operational safety net.

| Option | Description | Selected |
|--------|-------------|----------|
| Per-order aggregate | Single message per transition wave; less noise and aligns with current order-centric notification args. | ✓ |
| Per-item | More granular but noisy and requires richer item-level templating/context. | |
| Configurable per product type | Flexible but adds complexity and policy surface now. | |

**User's choice:** Per-order aggregate
**Notes:** Avoids message fan-out noise.

---

## Notification contact source

| Option | Description | Selected |
|--------|-------------|----------|
| Customer profile phone | Use account-level verified/normalized phone as source of truth; checkout can only override if explicitly allowed by policy. | ✓ |
| Checkout delivery contact only | Use per-order checkout input as sole source; simple for order context but can be inconsistent/dirty. | |
| Fallback chain profile->checkout | Use profile first, fallback to checkout contact when profile missing; more robust but needs clear precedence/audit. | |

**User's choice:** Customer profile phone
**Notes:** Canonical source locked at profile level.

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, immutable order snapshot | Persist normalized phone on order notification metadata for deterministic delivery and later audit/replay. | ✓ |
| No, resolve from profile at send time | Always current data, but historical replay may target changed number and break traceability. | |
| Hybrid with latest-preferred | Store snapshot but default to latest profile at send; adds policy complexity and ambiguity. | |

**User's choice:** Yes, immutable order snapshot
**Notes:** Snapshot required for deterministic replay.

| Option | Description | Selected |
|--------|-------------|----------|
| Block checkout until phone exists | Strong data quality but higher conversion friction. | |
| Allow checkout; mark notification unavailable | Do not break commerce flow; persist explicit missing-contact state and expose retry path when profile updated. | ✓ |
| Require manual phone input in checkout | Solves availability but introduces new capture UX/API surface in this hardening phase. | |

**User's choice:** Allow checkout; mark notification unavailable
**Notes:** Protect commerce path while surfacing retryable gap.

| Option | Description | Selected |
|--------|-------------|----------|
| E.164 normalized, reject invalid | Store canonical format only; invalid profile phone fails validation when set, not during notification send. | ✓ |
| Best-effort normalize, keep raw fallback | Higher acceptance but noisy delivery failures later. | |
| No normalization, validate at send time | Fast to implement but weak reliability and poor audit consistency. | |

**User's choice:** E.164 normalized, reject invalid
**Notes:** Validation moved upfront.

---

## HTTPS/HSTS proof contract

| Option | Description | Selected |
|--------|-------------|----------|
| Staging with prod-like ingress | Run probes in non-dev environment with same TLS termination/reverse-proxy behavior expected in production. | ✓ |
| Local docker only | Fast but weak evidence for real ingress behavior. | |
| Production only | Strongest realism but risky and slower feedback for verification loops. | |

**User's choice:** Staging with prod-like ingress
**Notes:** Runtime proof target environment locked.

| Option | Description | Selected |
|--------|-------------|----------|
| Redirect+HSTS+no insecure endpoints | Assert HTTP->HTTPS redirect, Strict-Transport-Security on HTTPS, and no public plaintext routes. | ✓ |
| Redirect only | Partial coverage; misses HSTS and route leaks. | |
| Header check only | Misses redirect behavior and endpoint exposure risk. | |

**User's choice:** Redirect+HSTS+no insecure endpoints
**Notes:** Full SEC-01 proof bundle required.

| Option | Description | Selected |
|--------|-------------|----------|
| CI smoke output + dated artifact folder | Store script output, request/response headers, and environment stamp in versioned artifact path linked from verification docs. | ✓ |
| Manual screenshot in wiki | Harder to automate/audit and prone to drift. | |
| Narrative in verification file only | Insufficient evidence for hard requirement closure. | |

**User's choice:** CI smoke output + dated artifact folder
**Notes:** Evidence pointer is mandatory for closure.

---

## Reliability observability gate

| Option | Description | Selected |
|--------|-------------|----------|
| Payment->Order->Fulfillment->Notification | Require trace/log correlation across the full critical commerce chain from webhook/payment confirmation through notification dispatch. | ✓ |
| Payment->Order only | Partial chain; misses fulfillment/notification blind spots. | |
| Order->Notification only | Misses payment/webhook diagnosis path. | |

**User's choice:** Payment->Order->Fulfillment->Notification
**Notes:** Full-chain correlation is required.

| Option | Description | Selected |
|--------|-------------|----------|
| Representative critical write set + tests | Audit must include admin product/stock/order mutation paths plus webhook-inspection actions, with integration assertions for actor/action/entity/before-after. | ✓ |
| Best-effort logging only | No hard closure guarantee; risk of silent gaps. | |
| All writes in system | Strong but too broad for this hardening scope and likely phase creep. | |

**User's choice:** Representative critical write set + tests
**Notes:** ADM-02 closure scope locked to representative critical paths.

| Option | Description | Selected |
|--------|-------------|----------|
| Evidence-gated checklist | Each requirement status flips only when linked tests + telemetry assertions + artifact paths are recorded in verification/state docs. | ✓ |
| Code merged + manual confidence | Fast but repeats prior paper-complete drift. | |
| Runtime monitoring only | Useful ops signal but insufficient as deterministic closure proof. | |

**User's choice:** Evidence-gated checklist
**Notes:** Requirement completion requires explicit evidence links.

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, include failure-path assertions | Require logs/fields and test evidence for key negative paths, not just happy path. | ✓ |
| Happy path only | Simpler but leaves incident-blind spots. | |
| Optional if time permits | Weak closure criteria and likely to be skipped. | |

**User's choice:** Yes, include failure-path assertions
**Notes:** Negative-path telemetry/testing is mandatory.

---

## the agent's Discretion

- Exact outbox/retry persistence structure and retention details.
- Exact DTO/endpoint naming for admin fallback/replay path.
- Exact telemetry field names/metric naming convention.

## Deferred Ideas

- Frontend delivery-status UX refinements (out of scope for backend-only phase).
- Advanced anti-fraud heuristics and analytics dashboards (v2 track unless hardening blocker).
