# Phase 6: Mercado Pago Payment Confirmation - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md - this log preserves alternatives considered.

**Date:** 2026-04-17
**Phase:** 06-mercado-pago-payment-confirmation
**Areas discussed:** Payment creation contract, Webhook trust gate, Idempotency + dedupe model, Paid transition mapping, Ack + processing flow

---

## Payment creation contract

| Option | Description | Selected |
|--------|-------------|----------|
| Checkout Pro preference | Backend creates Preference via .NET SDK, stores provider preference id + external_reference=orderId, returns init payload for frontend redirect/brick. | ✓ |
| Direct payment intent | Backend creates payment directly with payment method data now. More fields now, tighter coupling to method-specific flows. | |
| Hybrid | Support both now. More flexibility, more complexity and test surface. | |

**User's choice:** Checkout Pro preference.
**Notes:** Canonical provider binding chosen as `external_reference = orderId`; persist preference id + expected amount/currency snapshot.

---

## Webhook trust gate

| Option | Description | Selected |
|--------|-------------|----------|
| Strict fail-closed | Require valid x-signature HMAC + required headers/query. Invalid/missing signature never mutates state. | ✓ |
| Soft fail-open in sandbox | Allow sandbox/test without signature for speed. Production strict. | |
| No signature, token-only | Use endpoint secret token only. Simpler, weaker authenticity proof. | |

**User's choice:** Strict fail-closed.
**Notes:** Signature manifest policy chosen as Mercado Pago canonical template (`id:data.id(lowercase);request-id:x-request-id;ts:ts`) with HMAC-SHA256.

---

## Idempotency + dedupe model

| Option | Description | Selected |
|--------|-------------|----------|
| Provider resource id + action | Use data.id/payment id + action/type as unique processed key; duplicates become no-op. | ✓ |
| Raw payload hash | Hash full payload as key. Sensitive to harmless payload changes/order. | |
| OrderId-only dedupe | One event per order. Risks dropping legitimate multi-step updates. | |

**User's choice:** Provider resource id + action.
**Notes:** Out-of-order policy chosen as monotonic guard (ignore/log status regressions rather than rollback).

---

## Paid transition mapping

| Option | Description | Selected |
|--------|-------------|----------|
| Only approved/processed verified | Map to Paid only on verified approved/processed state; pending/in_process/authorized stay Pending; rejected/cancelled/refunded never mark Paid. | ✓ |
| Approved + authorized | Treat authorized as Paid. Faster, but not fully settled. | |
| Any non-failure | Mark Paid on broad success-like statuses. High false-positive risk. | |

**User's choice:** Only approved/processed verified.
**Notes:** Duplicate paid confirmations for already-paid order are lifecycle no-op with audit log retained.

---

## Ack + processing flow

| Option | Description | Selected |
|--------|-------------|----------|
| Ack fast, process async | Validate signature + minimal parse, persist inbound log, return 200/201 quickly, enqueue durable job for heavy processing/reconciliation. | ✓ |
| Process fully sync | Complete all DB/order transitions before response. Simple flow, risks timeout/retry storms. | |
| Mixed by event | Some sync some async. More branching complexity. | |

**User's choice:** Ack fast, process async.
**Notes:** Async failure policy chosen: Hangfire retries with backoff + dead-letter marker for admin inspection/manual replay path.

---

## the agent's Discretion

- Exact route/DTO naming for payment creation and webhook APIs.
- Exact persistence table split for inbound webhook logs vs payment status history.
- Exact backoff schedule and retention window.

## Deferred Ideas

None.
