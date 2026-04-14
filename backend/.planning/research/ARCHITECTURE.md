# Architecture Research

**Domain:** Tibia virtual goods commerce backend (webstore + fulfillment)
**Researched:** 2026-04-14
**Confidence:** MEDIUM

## Standard Architecture

### System Overview

Recommended shape for this project: **modular monolith with DDD bounded contexts**, async jobs, and webhook-driven integrations. Start as one deployable service, split later only if scaling/ownership pressure appears.

```
┌──────────────────────────────────────────────────────────────────────────┐
│                             API Layer (HTTP)                            │
├──────────────────────────────────────────────────────────────────────────┤
│ Auth API │ Catalog API │ Cart/Checkout API │ Orders API │ Admin API     │
│ Webhook API (Mercado Pago inbound)                                      │
└───────────────┬──────────────────────────────────────────────────────────┘
                │ calls use-cases
┌───────────────▼──────────────────────────────────────────────────────────┐
│                     Application Layer (Use Cases)                       │
├──────────────────────────────────────────────────────────────────────────┤
│ Checkout │ Reserve Stock │ Confirm Payment │ Create Delivery Task        │
│ Send Notification │ Retry Failed Steps │ Reconciliation                 │
└───────────────┬───────────────────────┬──────────────────────────────────┘
                │                       │ publishes jobs/events
┌───────────────▼───────────────────────▼──────────────────────────────────┐
│                  Domain Layer (Entities + Rules + Events)               │
├──────────────────────────────────────────────────────────────────────────┤
│ Users │ Products │ Stock │ Orders │ Payments │ Deliveries │ Coupons      │
│ AuditLog │ Domain Events (OrderPaid, DeliveryFailed, etc.)              │
└───────────────┬───────────────────────┬──────────────────────────────────┘
                │ persistence            │ external adapters
┌───────────────▼───────────────────────▼──────────────────────────────────┐
│                        Infrastructure Layer                              │
├──────────────────────────────────────────────────────────────────────────┤
│ PostgreSQL (OLTP) │ Outbox table │ Worker/Hosted Services              │
│ Mercado Pago adapter │ WhatsApp adapter │ Email adapter │ Cache (opt.)  │
└──────────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities (with boundaries)

| Component | Responsibility | Talks To | Must NOT Own |
|-----------|----------------|----------|--------------|
| API Layer | Request validation, authN/authZ, DTO mapping, idempotency keys for webhooks | Application layer | Business rules, SQL |
| Checkout context | Cart→order creation, price lock, coupon application | Stock, Payments, Orders contexts | Provider-specific webhook logic |
| Payments context | Payment intent records, webhook status transitions, payment audit trail | Orders context, Mercado Pago adapter | Product/stock rules |
| Inventory context | Stock reservation/release/commit with concurrency control | Orders context, PostgreSQL | Payment decisions |
| Fulfillment context | Delivery orchestration (auto/manual), status machine, retry routing | Orders, Notifications, workers | Payment confirmation |
| Notifications context | Template + channel dispatch (WhatsApp/email), delivery receipts | WhatsApp adapter, Email adapter | Order state transitions |
| Admin/Ops context | Backoffice workflows, manual overrides, reconciliation triggers | All application services (through use cases) | Direct DB writes bypassing use cases |
| Worker subsystem | Async retries, outbox dispatch, polling/reconciliation, dead-letter handling | Outbox table, adapters, domain use cases | Synchronous API responsibilities |
| Data layer (PostgreSQL) | Source of truth for transactional entities | Application repositories | External API calls |

## Recommended Project Structure

```text
src/
├── Api/                              # ASP.NET Core controllers, filters, middleware
│   ├── Controllers/
│   ├── Webhooks/
│   └── Contracts/
├── Application/                      # Use-cases, command/query handlers, DTOs
│   ├── Checkout/
│   ├── Orders/
│   ├── Payments/
│   ├── Fulfillment/
│   └── Notifications/
├── Domain/                           # Entities, value objects, domain services/events
│   ├── Orders/
│   ├── Payments/
│   ├── Inventory/
│   └── Shared/
├── Infrastructure/                   # EF Core, repositories, adapters, outbox, workers
│   ├── Persistence/
│   ├── Integrations/
│   │   ├── MercadoPago/
│   │   ├── WhatsApp/
│   │   └── Email/
│   └── BackgroundJobs/
└── SharedKernel/                     # Cross-cutting abstractions and primitives
```

### Structure Rationale

- **Bounded contexts map to folders** so roadmap phases can be scoped without touching every layer.
- **Integration adapters isolated in Infrastructure** prevents external API schema leakage into domain model (anti-corruption boundary).
- **Workers isolated from request path** keeps checkout latency stable and supports retry/compensation.

## Architectural Patterns

### Pattern 1: Modular Monolith + Bounded Contexts

**What:** One deployable backend, internally split by domain boundaries and explicit contracts.
**When to use:** Greenfield, one team, rapidly evolving requirements, need fast iteration and transactional consistency.
**Trade-offs:** Faster delivery than microservices now; requires discipline to avoid context coupling.

### Pattern 2: Transactional Outbox for reliable integration side effects

**What:** Persist domain state + outbound message in same DB transaction; background relay publishes later.
**When to use:** Order paid → notify user / trigger fulfillment without 2PC.
**Trade-offs:** Extra table + relay complexity; consumers must be idempotent.

### Pattern 3: Competing Consumers for delivery/notification workloads

**What:** Multiple workers consume queued/outbox tasks concurrently.
**When to use:** Burst payments, many fulfillment tasks, retry backlog.
**Trade-offs:** Ordering is not guaranteed globally; task handlers must be idempotent and poison-message aware.

## Data Flow

### Flow A — Checkout to fulfillment (happy path)

1. **Client → API:** create checkout/order request.
2. **API → Application:** `CreateOrder` use case validates and writes `Order(PendingPayment)` + stock reservation.
3. **Application → DB:** transaction commits order + reservation.
4. **Client pays via Mercado Pago** (redirect/SDK outside backend).
5. **Mercado Pago → Webhook API:** payment event callback.
6. **Webhook API → Payments use case:** verify signature/idempotency, persist payment log + transition order (`Paid`).
7. **Payments use case → Outbox:** write `OrderPaid` event in same transaction.
8. **Outbox relay worker → Fulfillment use case:** create delivery tasks.
9. **Fulfillment → Notifications:** emit `DeliveryStarted/Completed` events.
10. **Notification worker → WhatsApp/Email adapters:** send user/operator updates.

**Direction summary:** Inbound webhooks are always API → Application → DB; outbound calls are always Worker/Application → Integration adapters.

### Flow B — Failure and compensation

1. Payment approved but fulfillment fails (inventory inconsistency/manual handoff needed).
2. Fulfillment marks `DeliveryFailed` with reason and retry policy.
3. Worker retries according to backoff; after threshold, dead-letter/manual queue.
4. Admin action triggers compensate path (refund trigger or manual completion) through explicit use case.

### Concurrency rules (critical)

- Use **row-level locking / optimistic concurrency** on stock reservations.
- Keep transactional order workflow in PostgreSQL (default `Read Committed` with explicit locking for reservation hotspots).
- Make webhook handlers **idempotent** (event ID + payment ID uniqueness constraints).

## Suggested Build Order (for roadmap dependency planning)

1. **Foundation:** project skeleton, Clean Architecture layers, auth/session, base observability.
2. **Catalog + Inventory core:** products, stock model, reservation semantics.
3. **Orders + Checkout core:** cart/order lifecycle without live payment automation.
4. **Payments integration:** Mercado Pago webhook endpoint + robust payment state machine.
5. **Outbox + Worker pipeline:** reliable async processing + retries.
6. **Fulfillment orchestration:** automated + manual delivery workflows.
7. **Notifications:** WhatsApp first, email second, delivery receipts and retry/dead-letter.
8. **Admin/Ops tooling:** reconciliation dashboards, manual override, audit search.
9. **Hardening:** rate limits, anti-abuse hooks, SLO-driven tuning.

**Why this order:** Payment and fulfillment are unsafe without stable order/inventory invariants first. Notifications depend on reliable event emission. Admin tooling is most valuable after real operational data exists.

## Scaling Considerations

| Scale | Architecture Adjustment |
|-------|--------------------------|
| 0-1k buyers/month | Single modular monolith + PostgreSQL + in-process workers is enough |
| 1k-100k buyers/month | Move workers to separate process, add queue broker, read replicas, stricter outbox throughput controls |
| 100k+ buyers/month | Split by bounded contexts with highest write pressure first (Payments/Fulfillment), keep contract-first APIs/events |

## Anti-Patterns

### Anti-Pattern 1: Treat webhook as final truth without verification

**What people do:** Trust webhook payload blindly and immediately mutate order state.
**Why it’s wrong:** Duplicate/out-of-order callbacks and spoofed requests create incorrect transitions.
**Do this instead:** Verify provider signature/reference, enforce idempotency keys, and model explicit state transitions.

### Anti-Pattern 2: External API calls inside core DB transaction

**What people do:** Call WhatsApp/Mercado Pago while order transaction is open.
**Why it’s wrong:** Long locks, timeout cascades, partial commits.
**Do this instead:** Commit domain state first; publish side effects through outbox + workers.

### Anti-Pattern 3: “Microservices first” before domain maturity

**What people do:** Split by technical layers into many services from day one.
**Why it’s wrong:** Distributed transactions, tracing, and deployment overhead before product-market fit.
**Do this instead:** Modular monolith now; extract services only when boundaries and scale bottlenecks are proven.

## Integration Points

### External Services

| Service | Integration Pattern | Notes |
|---------|---------------------|-------|
| Mercado Pago | Inbound webhook + outbound payment query APIs via adapter | Webhook idempotency + signature validation mandatory |
| WhatsApp API | Outbound async message dispatch from worker | Retries + template versioning + provider error mapping |
| Email provider (optional) | Outbound async fallback channel | Use for degraded WhatsApp cases |

### Internal Boundaries (what talks to what)

| Boundary | Communication | Notes |
|----------|---------------|-------|
| API ↔ Application | In-process command/query calls | No direct infrastructure calls from controllers |
| Application ↔ Domain | Direct method calls/events | Domain holds invariants |
| Application ↔ Infrastructure | Repository + adapter interfaces | Dependency inversion only |
| Payments ↔ Orders | Domain events + application service contracts | Avoid direct DB access across contexts |
| Fulfillment ↔ Notifications | Async events/outbox | Prevent user-facing latency from delivery pipeline |

## Confidence & Gaps

- **Architecture confidence: MEDIUM-HIGH** for layered DDD + outbox + worker model (strong cross-domain evidence).
- **Provider-specific confidence: MEDIUM** because Mercado Pago and WhatsApp docs were partially inaccessible via this run; integration details should be revalidated during payment/notification implementation phase.

## Sources

- Microsoft Learn — DDD and bounded contexts (updated 2026-02-23): https://learn.microsoft.com/en-us/azure/architecture/microservices/model/domain-analysis
- Microsoft Learn — ASP.NET Core hosted/background services (updated 2025-05-28): https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
- Microsoft Learn — Competing Consumers pattern (updated 2026-04-02): https://learn.microsoft.com/en-us/azure/architecture/patterns/competing-consumers
- PostgreSQL docs — Transaction isolation and concurrency behavior (v18 current): https://www.postgresql.org/docs/current/transaction-iso.html
- Microservices.io — Transactional outbox pattern (community/industry reference): https://microservices.io/patterns/data/transactional-outbox.html

---
*Architecture research for: Tibia virtual goods webstore backend*
*Researched: 2026-04-14*
