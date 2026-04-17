# Phase 3: Inventory Integrity & Reservation Control - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-17
**Phase:** 03-inventory-integrity-reservation-control
**Areas discussed:** Reservation lifecycle, Concurrency control, Availability contract, Admin stock adjustments + audit

---

## Reservation lifecycle

| Option | Description | Selected |
|--------|-------------|----------|
| At checkout submit | Reserve only when creating pending-payment order; avoids cart hoarding and matches Phase 4->6 flow. | ✓ |
| At add-to-cart | Stronger buyer guarantee, but high risk of abandoned-cart stock lock. | |
| Hybrid hold | Soft cart hold then hard reserve at checkout; more complex lifecycle. | |

**User's choice:** At checkout submit
**Notes:** Reservation begins only at checkout/order creation.

| Option | Description | Selected |
|--------|-------------|----------|
| 15 minutes | Balances conversion and stock turnover; aligns with existing short-lived security windows. | ✓ |
| 30 minutes | More payment time, but increases stock lock duration. | |
| 60 minutes | Maximum buyer cushion, but highest dead-lock stock risk. | |

**User's choice:** 15 minutes
**Notes:** Reservation TTL locked to 15 minutes.

| Option | Description | Selected |
|--------|-------------|----------|
| Immediate release | Release as soon as status becomes canceled/failed to maximize availability. | ✓ |
| Grace delay | Keep brief cooldown before release to absorb transient provider states. | |
| Manual release only | Operator-controlled, safest for edge cases but operationally heavy. | |

**User's choice:** Immediate release
**Notes:** Release occurs immediately on failed/canceled payment.

| Option | Description | Selected |
|--------|-------------|----------|
| Per product quantity aggregate | Track reserved units per product/order line; simpler and fits current product model. | ✓ |
| Per-unit tokenized reservations | Strong traceability per unit, but much heavier schema and logic. | |
| Mixed strategy | Aggregate for most products, tokenized for selected SKUs; adds policy complexity. | |

**User's choice:** Per product quantity aggregate
**Notes:** No per-unit reservation model required.

---

## Concurrency control

| Option | Description | Selected |
|--------|-------------|----------|
| Atomic DB decrement | Single transactional update with availability predicate; best fit for PostgreSQL + current EF architecture. | ✓ |
| Optimistic version check | Use row version/concurrency token and retry conflicts; clear but more retry logic at app layer. | |
| App-level distributed lock | Lock by product key in Redis/app layer; strongest serialization but adds infra coupling. | |

**User's choice:** Atomic DB decrement
**Notes:** Oversell prevention must be guaranteed at DB transaction level.

| Option | Description | Selected |
|--------|-------------|----------|
| Reject line with clear 409 | Conflict response with remaining quantity detail; consistent with current ProblemDetails style. | ✓ |
| Reject order with generic 400 | Simpler mapping, less precise client behavior. | |
| Auto-reduce quantity | Attempts partial fulfillment automatically; may surprise users. | |

**User's choice:** Reject line with clear 409
**Notes:** Conflict semantics chosen over generic validation errors.

| Option | Description | Selected |
|--------|-------------|----------|
| Idempotent by order intent key | Same request key returns same reservation outcome; avoids double-reserve under retries. | ✓ |
| Best-effort without key | Rely on transaction safety only; less contract certainty for client retries. | |
| Strict duplicate rejection | Reject repeated attempts regardless; safer but can hurt UX on network retries. | |

**User's choice:** Idempotent by order intent key
**Notes:** Retry-safe reservation behavior is required.

| Option | Description | Selected |
|--------|-------------|----------|
| Last write wins + audit trail | Simpler operator flow; rely on audit for traceability. | |
| Require concurrency token retry | Detect stale writes and force explicit retry; safer inventory integrity. | ✓ |
| Queue adjustments serially | Centralized sequencing, but adds processing latency and queue complexity. | |

**User's choice:** Require concurrency token retry
**Notes:** Admin writes should detect stale updates explicitly.

---

## Availability contract

| Option | Description | Selected |
|--------|-------------|----------|
| Expose available + reserved + total | Most transparent and supports admin/customer diagnostics without extra calls. | ✓ |
| Expose available only | Simpler contract but less insight for debugging reservation pressure. | |
| Expose boolean in-stock only | Simplest UI contract, but loses quantity semantics required by INV-01/03. | |

**User's choice:** Expose available + reserved + total
**Notes:** Quantity transparency is preferred.

| Option | Description | Selected |
|--------|-------------|----------|
| Authoritative at reservation transaction | Final decision happens atomically during reserve; pre-check is advisory only. | ✓ |
| Authoritative at cart view | Stronger early certainty, but stale quickly under concurrency. | |
| Authoritative at payment start | Defers stock lock too late; higher oversell/collision risk. | |

**User's choice:** Authoritative at reservation transaction
**Notes:** Checkout pre-checks are non-authoritative.

| Option | Description | Selected |
|--------|-------------|----------|
| ProblemDetails 409 + available qty | Actionable conflict message so client can adjust quantity immediately. | ✓ |
| ProblemDetails 400 generic | Minimal payload, less actionable UX. | |
| Silent clamp to available | Auto-adjusts request without explicit client intent. | |

**User's choice:** ProblemDetails 409 + available qty
**Notes:** Client receives actionable conflict data.

| Option | Description | Selected |
|--------|-------------|----------|
| Read-through DB truth each request | No cache in phase 3; prioritize correctness over read optimization. | ✓ |
| Short TTL cache | Lower DB load but risks stale availability under bursts. | |
| Eventual consistency feed | Scalable later, but not sufficient for this phase integrity goals. | |

**User's choice:** Read-through DB truth each request
**Notes:** Correctness prioritized over read optimization in this phase.

---

## Admin stock adjustments + audit

| Option | Description | Selected |
|--------|-------------|----------|
| Delta only (+/- units) | Safer under concurrency and clearer audit intent (what changed, not just final value). | ✓ |
| Absolute set only | Simple UI but higher stale-write risk if current stock changed since read. | |
| Support both delta + absolute | Flexible but broader validation and audit complexity. | |

**User's choice:** Delta only (+/- units)
**Notes:** Adjustment model stays additive/subtractive only.

| Option | Description | Selected |
|--------|-------------|----------|
| No, block negatives | Preserves inventory integrity and clear oversell prevention contract. | ✓ |
| Allow with warning | Operational flexibility for reconciliation, but weakens hard integrity rule. | |
| Allow only for privileged role | Controlled escape hatch, but adds role/policy complexity in phase 3. | |

**User's choice:** No, block negatives
**Notes:** Negative post-adjustment stock is disallowed.

| Option | Description | Selected |
|--------|-------------|----------|
| Admin, product, delta, before/after, reason, timestamp | Full traceability for INV-04 and operational debugging. | ✓ |
| Admin, product, delta, timestamp | Minimal logs; may be insufficient for investigations. | |
| Event log only without before/after | Cheaper write path, weaker reconciliation evidence. | |

**User's choice:** Admin, product, delta, before/after, reason, timestamp
**Notes:** Full audit payload is mandatory.

| Option | Description | Selected |
|--------|-------------|----------|
| Required free-text reason | Improves accountability; supports audit investigations. | ✓ |
| Optional reason | Lower friction, less consistent operational data. | |
| Enum reason only | Structured analytics, but may not capture real incident context. | |

**User's choice:** Required free-text reason
**Notes:** Free-text rationale is required on each adjustment.

---

## the agent's Discretion

- Exact storage model for reservation state and idempotency keys.
- Exact ProblemDetails extension field naming and error codes.

## Deferred Ideas

None.
