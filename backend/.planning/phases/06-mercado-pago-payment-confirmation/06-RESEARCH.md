# Phase 06 Research — Mercado Pago Payment Confirmation

**Phase:** 06 — Mercado Pago Payment Confirmation  
**Date:** 2026-04-17  
**Requirements in scope:** PAY-01, PAY-02, PAY-03, PAY-04

## Standard Stack

- ASP.NET Core 10 Minimal APIs with route-group endpoint modules
- Clean Architecture flow (`API -> Application -> Domain`, `Infrastructure -> Application/Domain`)
- EF Core 10 + Npgsql for transactional persistence and audit logs
- Mercado Pago .NET SDK (`PreferenceClient`, `MercadoPagoConfig.AccessToken`) for payment preference creation
- Hangfire for asynchronous webhook processing/retries/dead-letter handling
- xUnit unit/integration tests + deterministic RFC7807 contracts

## Architecture Patterns

- Keep webhook authenticity and idempotency gates in Application/Infrastructure services, never in thin endpoint-only code.
- Use fail-closed trust gate: invalid or missing signature produces no mutation and only auditable reject log.
- Use durable append-only payment event/history records and explicit dedupe key to absorb provider retries.
- Keep order transition authority in existing lifecycle service (`OrderLifecycleService`) and call it only from verified approved/processed flow.
- Acknowledge webhook fast (`200/201`) after trust gate + minimal inbound log, then enqueue heavy processing.

## Mercado Pago Documentation Findings (MCP)

- Checkout Pro preference creation is per-order/per-flow operation and official .NET SDK supports `PreferenceClient.CreateAsync(...)` payload creation.
- Webhook signature validation uses header `x-signature` with `ts` + `v1`, and manifest format:
  - `id:[data.id_lowercase];request-id:[x-request-id];ts:[ts];`
  - HMAC SHA256(hex) with app webhook secret must match `v1`.
- `data.id` must be normalized to lowercase in manifest even when notification sends uppercase.
- Mercado Pago expects `200` or `201` response quickly (22s timeout). Retries occur every ~15 minutes and continue after initial attempts.

## Locked Decision Translation (Context Fidelity)

- **D-01, D-02, D-03:** Payment creation uses Checkout Pro preference via SDK, binds `external_reference=orderId`, persists local payment-link snapshot (`orderId`, `preferenceId`, expected amount/currency).
- **D-04, D-05:** Webhook is strict fail-closed with exact `x-signature` manifest and HMAC-SHA256 validation.
- **D-06, D-07, D-08:** Processing dedupe key uses provider identity + action; duplicate deliveries are no-op for lifecycle; out-of-order regressions are ignored/logged.
- **D-09..D-12:** Only verified approved/processed transitions order to `Paid`; non-approved/failure statuses never mark paid; already-paid confirmations are lifecycle no-op with audit.
- **D-13, D-14:** Endpoint acks quickly after minimal validated log and schedules heavy processing through Hangfire retries and dead-letter marking.

## Don’t Hand-Roll

- Do not call Mercado Pago preference REST manually when SDK client already provides it (D-01).
- Do not trust webhook body without signature check and canonical manifest reconstruction (D-04, D-05).
- Do not process webhook synchronously end-to-end in HTTP request thread (D-13).
- Do not mutate order status directly in API/Infrastructure bypassing lifecycle service (D-09).
- Do not overwrite/rollback to older provider status on late-arriving events (D-08).

## Common Pitfalls

1. **Manifest mismatch due to uppercase `data.id`.**  
   Mitigation: always lowercase before HMAC manifest composition.
2. **Duplicate webhook retries generating duplicate lifecycle events.**  
   Mitigation: unique dedupe guard on provider identity + action and idempotent lifecycle no-op handling.
3. **Webhook endpoint timing out due to heavy synchronous work.**  
   Mitigation: minimal inline work, enqueue Hangfire processing immediately after trust gate.
4. **Paid regression from late out-of-order events.**  
   Mitigation: monotonic status policy with explicit ignore/log path.
5. **Lack of operational traceability for audit/debug.**  
   Mitigation: persist inbound payload summary, request-id, outcome, and timestamps for each attempt.

## Validation Architecture

- Unit tests:
  - Signature parser/validator tests (`ts`,`v1`, manifest format, lowercase `data.id`) (D-04, D-05)
  - Dedupe/ordering policy tests for duplicate and regression events (D-06, D-07, D-08)
  - Payment status mapping to lifecycle policy tests (D-09..D-12)
- Integration tests:
  - Checkout payment-init endpoint creates preference through SDK adapter and persists payment link snapshot (D-01..D-03)
  - Webhook endpoint returns `200/201` rapidly, stores minimal inbound log, and enqueues async processor (D-13)
  - Async processor path transitions order to Paid only on verified approved/processed and remains no-op otherwise (D-09..D-12, D-14)
  - Duplicate deliveries do not duplicate timeline/payment transition writes (D-06, D-07)
- Persistence verification:
  - Migration adds payment link + webhook inbound log + payment status history + dedupe guard indexes
  - Blocking `dotnet ef database update` before final phase verification

## Security and Threat Focus

- Trust boundary: external Mercado Pago webhook -> internal order/payment mutation pipeline.
- ASVS L1 controls focus: authenticity validation, replay/idempotency protection, authorization of privileged state transition, auditable logs without secret leakage.
- Block-high policy: treat signature bypass or replay gap as blocking planning defects.

## Research Outcome

Phase 06 should execute in three dependent plans:
1. Payment preference creation + local payment-link snapshot persistence.
2. Webhook trust gate + idempotent async processing/logging infrastructure.
3. Verified paid transition mapping and full integration proofs against existing order lifecycle invariants.
