---
phase: 06-mercado-pago-payment-confirmation
plan: 02
subsystem: payments
tags: [mercadopago, webhook, idempotency, hangfire]

# Dependency graph
requires:
  - phase: 06-01
    provides: "Payment preference creation and local payment-link snapshot"
provides:
  - "Webhook trust gate with x-signature HMAC validation"
  - "Fast-ack webhook endpoint with async processing"
  - "Webhook idempotency/dedupe persistence and guard"
  - "Monotonic status guard for regression prevention"
affects: [06-03 (paid transition mapping)]

# Tech tracking
tech-stack:
  added: [Hangfire.Core 1.8.23, Hangfire.PostgreSql 1.21.1]
  patterns: [fail-closed signature validation, idempotent async processing, monotonic status guard]

key-files:
  created:
    - src/Application/Payments/Contracts/PaymentWebhookContracts.cs
    - src/Application/Payments/Contracts/IPaymentWebhookSignatureValidator.cs
    - src/Application/Payments/Contracts/IPaymentWebhookProcessor.cs
    - src/Application/Payments/Contracts/IPaymentWebhookLogRepository.cs
    - src/Application/Payments/Contracts/IPaymentStatusEventRepository.cs
    - src/Application/Payments/Contracts/IPaymentEventDedupRepository.cs
    - src/Application/Payments/Services/PaymentWebhookIngressService.cs
    - src/Application/Payments/Services/PaymentWebhookProcessor.cs
    - src/Infrastructure/Payments/MercadoPago/MercadoPagoWebhookSignatureValidator.cs
    - src/Infrastructure/Payments/Repositories/PaymentWebhookLogRepository.cs
    - src/Infrastructure/Payments/Repositories/PaymentStatusEventRepository.cs
    - src/Infrastructure/Payments/Repositories/PaymentEventDedupRepository.cs
    - src/API/Payments/PaymentWebhookEndpoints.cs
    - src/Infrastructure/Persistence/Configurations/PaymentWebhookLogConfiguration.cs
    - src/Infrastructure/Persistence/Configurations/PaymentStatusEventConfiguration.cs
    - src/Infrastructure/Persistence/Configurations/PaymentEventDedupConfiguration.cs
    - src/Infrastructure/Persistence/Migrations/20260418133852_AddMercadoPagoPaymentWebhookTracking.cs
    - tests/UnitTests/Payments/WebhookSignatureValidatorTests.cs
    - tests/IntegrationTests/Payments/PaymentWebhookEndpointsTests.cs
  modified:
    - src/API/API.csproj
    - src/API/Program.cs
    - src/Infrastructure/Infrastructure.csproj
    - src/Infrastructure/DependencyInjection.cs
    - src/Infrastructure/Persistence/AppDbContext.cs
    - src/Infrastructure/Payments/MercadoPago/MercadoPagoOptions.cs

key-decisions:
  - "Webhook signature validation uses fail-closed (D-04)"
  - "Manifest format: id:{data.id.lowercase};request-id:{x-request-id};ts:{ts}; (D-05)"
  - "Dedupe key: providerResourceId + action (D-06)"
  - "Duplicate deliveries are audit no-ops for lifecycle (D-07)"
  - "Monotonic status guard ignores regressions (D-08)"
  - "Fast ack path with minimal work before async enqueue (D-13)"

patterns-established:
  - "Pattern: x-signature HMAC-SHA256 validation with constant-time comparison"
  - "Pattern: idempotent webhook processing with dedupe lock"
  - "Pattern: monotonic status guard for out-of-order events"

requirements-completed: [PAY-02, PAY-03]

# Metrics
duration: 12min
completed: 2026-04-18
---

# Phase 06-02 Plan: Webhook Trust Gate + Idempotent Processing Summary

**Mercado Pago webhook trust gate with x-signature validation and idempotent async processing pipeline**

## Performance

- **Duration:** 12 min
- **Started:** 2026-04-18
- **Completed:** 2026-04-18
- **Tasks:** 3
- **Files modified:** 28

## Accomplishments
- Webhook authenticity trust gate with Mercado Pago canonical x-signature HMAC-SHA256 validation (D-04, D-05)
- Fast-ack webhook endpoint with async processing via Hangfire (D-13)
- Idempotent webhook processing with dedupe guard and monotonic status guard (D-06, D-07, D-08)
- Persistent webhook logs, payment status events, and dedupe tables

## Task Commits

Each task was committed atomically:

1. **Task 1: Webhook trust gate** - `97723d3` (feat)
2. **Task 2: Webhook endpoint + async processor** - `05c5d7a` (feat)
3. **Task 3: Migration** - `c88c425` (feat)

**Plan metadata:** `c88c425` (docs: complete plan)

## Files Created/Modified
- `src/API/Payments/PaymentWebhookEndpoints.cs` - Fast-ack webhook route
- `src/Application/Payments/Services/PaymentWebhookProcessor.cs` - Idempotent async processor
- `src/Infrastructure/Payments/MercadoPago/MercadoPagoWebhookSignatureValidator.cs` - HMAC validator
- `src/Infrastructure/Payments/Repositories/PaymentWebhookLogRepository.cs` - Webhook persistence
- `src/Infrastructure/Persistence/Migrations/*AddMercadoPagoPaymentWebhookTracking*` - Schema migration

## Decisions Made
- Fail-closed trust model: invalid signature never mutates order/payment state (D-04)
- Canonical manifest format with lowercase data.id normalization (D-05)
- Dedupe via unique constraint on providerResourceId + action (D-06)
- Duplicate webhooks are lifecycle no-ops with audit retained (D-07)
- Monotonic guard ignores out-of-order regressions (D-08)
- Fast ack pattern defers heavy work to Hangfire (D-13)

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Schema migrated and ready for PAY-04 paid transition mapping
- Webhook processing pipeline in place for transition wiring in plan 06-03

---
*Phase: 06-mercado-pago-payment-confirmation*
*Completed: 2026-04-18*