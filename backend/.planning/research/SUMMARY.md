# Project Research Summary

**Project:** Tibia Webstore Backend
**Domain:** Tibia virtual-goods commerce backend (single-store, Aurera/Eternia)
**Researched:** 2026-04-14
**Confidence:** MEDIUM-HIGH

## Executive Summary

This project is best treated as a **Tibia-first digital commerce operations system**, not just a checkout API. The research converges on a backend that must reliably coordinate catalog/inventory constraints, webhook-based payment confirmation, and mixed manual/automated fulfillment while preserving an auditable timeline for disputes. Expert implementations in this domain start with transactional consistency and idempotent async workflows first, then layer operational tooling and growth features.

The recommended implementation strategy is a **modular monolith on .NET 10 + EF Core 10 + PostgreSQL 17**, organized by bounded contexts (Catalog/Inventory, Orders/Payments, Fulfillment, Notifications, Admin/Ops). Payments should be webhook-first (Mercado Pago), with explicit order/payment state machines, transactional outbox, and background workers for delivery orchestration and notifications. This preserves correctness under retries, latency spikes, and partial failures while keeping v1 operationally simple.

The highest risks are duplicate fulfillment from non-idempotent webhooks, overselling due to weak inventory modeling/concurrency, and post-delivery loss from missing dispute/reversal lifecycle. Mitigation is clear and should be phase-gating: enforce monotonic transitions and dedup constraints before automation, split inventory semantics (fungible/unique/slot-based) with reservation TTLs, and ship immutable audit evidence plus reconciliation workflows immediately after payment integration.

## Key Findings

### Recommended Stack

Research strongly supports a pragmatic, supportable stack centered on **.NET 10 LTS + EF Core 10 + PostgreSQL 17 + Npgsql**, with Hangfire for durable background jobs and Serilog/OpenTelemetry for operational traceability. This stack is aligned with current support windows and directly fits the project’s reliability constraints around payment webhooks, retries, and fulfillment orchestration.

**Core technologies:**
- **.NET 10 + ASP.NET Core Web API:** service runtime and middleware — strongest fit for clean architecture, throughput, and long-term support.
- **EF Core 10 + Npgsql provider:** transactional persistence and migrations — mature ecosystem and strong compatibility with DDD aggregates.
- **PostgreSQL 17:** OLTP source of truth — robust locking/indexing/JSONB support for mixed commerce and audit workloads.
- **Mercado Pago SDK (.NET):** payment integration baseline — reduces custom integration surface and supports idempotency/retry patterns.
- **Hangfire + PostgreSQL storage:** durable async execution — essential for webhook-safe side effects, fulfillment retries, and reconciliation jobs.
- **Serilog + OpenTelemetry:** observability from day one — required for tracing webhook→order→delivery paths and incident response.

Critical version guidance: keep EF and Npgsql provider majors aligned (10.x), and align ASP.NET/Serilog majors to avoid hosting incompatibilities.

### Expected Features

v1 must focus on reliability and trust primitives expected in game-goods commerce: server-scoped cataloging, webhook-confirmed order lifecycle, trackable fulfillment, and dispute-capable auditability.

**Must have (table stakes):**
- Server/world-scoped catalog + compatibility enforcement.
- Product-type-aware workflows (gold/coins/items/services with distinct fulfillment schemas).
- Checkout + Mercado Pago webhook confirmation with idempotent processing.
- Shared customer/admin order timeline states.
- Delivery-instruction capture and validation (character/world + optional windows).
- Stock visibility + reservation/release semantics.
- WhatsApp-first notification pipeline (email optional fallback).
- Baseline dispute/refund workflow with evidence trail.

**Should have (competitive):**
- SLA-driven fulfillment orchestration with escalation queues.
- Proactive admin exception alerts (timeouts/mismatch/retry exhaustion).
- Repeat-order shortcuts and saved delivery defaults.
- Trust transparency panel from immutable order timeline.

**Defer (v2+):**
- Multi-game expansion.
- C2C marketplace/escrow.
- Advanced fraud ML scoring.

### Architecture Approach

The architecture recommendation is a **modular monolith with DDD bounded contexts** and a strict sync/async split: API validates/authenticates and invokes use cases; application/domain enforce invariants; infrastructure persists state and handles external adapters. Reliability-critical side effects (fulfillment creation, notifications) should flow via **transactional outbox + workers**, not inline webhook transactions.

**Major components:**
1. **API layer (incl. webhook API)** — input validation, auth, idempotency envelopes.
2. **Application layer (use cases)** — checkout, reserve stock, confirm payment, create delivery tasks, retries/reconciliation.
3. **Domain layer (entities + events)** — orders/payments/inventory invariants and legal state transitions.
4. **Infrastructure layer** — PostgreSQL persistence, outbox relay, Hangfire/background workers, Mercado Pago + WhatsApp/email adapters.
5. **Admin/Ops surface** — manual overrides, reconciliation triggers, audit search through use cases (no direct DB writes).

### Critical Pitfalls

1. **Non-idempotent webhook processing** — duplicate callbacks can trigger duplicate fulfillment. Mitigate with dedup keys, unique constraints, monotonic transition guards, and async side-effect workers.
2. **No post-delivery reversal lifecycle** — approved→delivered without dispute/reversal modeling creates hidden margin loss. Add risk/dispute states and immutable evidence bundles.
3. **Hybrid inventory oversell** — single quantity model across fungible/unique assets causes double allocation. Split inventory types, reservation TTL, and reconciliation jobs.
4. **Character/world mismatch fulfillment errors** — free-text capture leads to misdelivery. Enforce structured identity fields + pre-delivery checklist + immutable receipt.
5. **Weak worker durability/concurrency controls** — restarts/retries can drop or duplicate tasks. Persist work items, bound concurrency, add poison/dead-letter handling, and end-to-end correlation IDs.

## Implications for Roadmap

Based on combined research, the roadmap should be ordered by **invariant-first dependencies**: correctness in catalog/inventory/order/payment transitions before automation and scale features.

### Phase 1: Foundation, Boundaries, and Observability
**Rationale:** All later phases depend on stable architecture boundaries and traceability.  
**Delivers:** Modular monolith skeleton (API/Application/Domain/Infrastructure), base auth/RBAC separation, Serilog+OpenTelemetry, health checks, migration pipeline.  
**Addresses:** Architecture baseline + security pitfall prevention (admin/customer scope separation).  
**Avoids:** Early coupling, low-visibility failures, untraceable incidents.

### Phase 2: Catalog Governance + Inventory Core
**Rationale:** Product and stock invariants are prerequisites for safe checkout and fulfillment.  
**Delivers:** Server-scoped catalog, policy flags/reviewer audit metadata, inventory type split (FUNGIBLE/UNIQUE/SLOT_BASED), reservation TTL + concurrency controls.  
**Addresses:** Table-stakes catalog/stock visibility; policy/TOS and oversell pitfalls.  
**Avoids:** Sanction-prone listings, double allocation, negative stock drift.

### Phase 3: Orders/Checkout State Machine
**Rationale:** Order lifecycle must exist before payment and async orchestration can be reliable.  
**Delivers:** Order aggregate/state machine, validated delivery instructions (character/world), transaction-safe order+reservation creation, shared timeline model.  
**Addresses:** Checkout baseline, order tracking, delivery capture requirements.  
**Avoids:** Illegal transitions, wrong-character/world fulfillment starts, lost updates.

### Phase 4: Payments Integration + Idempotent Webhooks
**Rationale:** Payment confirmation is the core business trigger and highest-risk failure point.  
**Delivers:** Mercado Pago integration, signed/idempotent webhook handler, payment log model, monotonic payment→order transitions, duplicate-event safety tests.  
**Addresses:** P1 payment-confirmed lifecycle requirement.  
**Avoids:** Double fulfillment and spoofed/out-of-order webhook corruption.

### Phase 5: Outbox + Worker Backbone + Notifications
**Rationale:** Side effects must be decoupled from request path before fulfillment scale-up.  
**Delivers:** Transactional outbox, Hangfire workers, retry/backoff/dead-letter flow, WhatsApp-first templates + email fallback, operator alerts.  
**Addresses:** Notification expectations and architecture async patterns.  
**Avoids:** Dropped jobs, duplicate sends, latency coupling to providers.

### Phase 6: Fulfillment Orchestration + Dispute/Reconciliation
**Rationale:** Core value is reliable delivery with trust-preserving recovery paths.  
**Delivers:** Manual+automated fulfillment workflows, SLA timers/escalation hooks, immutable delivery evidence, dispute/refund/reversal states, reconciliation actions in admin ops.  
**Addresses:** P1 trust workflows + P2 SLA differentiator.  
**Avoids:** Delivered-but-reversed blind spots, spreadsheet-driven dispute handling, hidden margin leak.

### Phase 7: Hardening and Growth Features (v1.x)
**Rationale:** Only after operational reliability is proven should optimization features be added.  
**Delivers:** Repeat-order shortcuts, coupon engine, anti-automation controls, read-model optimization for dashboards, partition/retention strategy.  
**Addresses:** v1.x differentiators and performance traps.  
**Avoids:** premature complexity and unstable growth.

### Phase Ordering Rationale

- Dependencies require catalog/inventory/order invariants before payment and fulfillment automation.
- Outbox/workers must precede high-volume delivery/notification behavior to avoid reliability debt.
- Dispute/reconciliation should follow payment integration immediately to close revenue-risk gaps.
- Growth features (coupons, repeat shortcuts) are intentionally delayed until core reliability metrics stabilize.

### Research Flags

Phases likely needing deeper research during planning:
- **Phase 4 (Payments integration):** Mercado Pago webhook signature/idempotency specifics had partial source accessibility; revalidate against latest official docs and sandbox behavior.
- **Phase 5 (WhatsApp provider behavior):** message status semantics, rate limits, and retry contract details need provider-specific verification.
- **Phase 6 (Policy/risk operations):** Tibia rule interpretation for catalog governance and reversal handling needs explicit operational policy decisions.

Phases with standard patterns (can usually skip extra research-phase):
- **Phase 1:** clean/modular architecture setup in ASP.NET Core is well documented.
- **Phase 2/3 core persistence and concurrency:** EF Core + PostgreSQL patterns are mature and strongly sourced.
- **Phase 5 outbox/worker baseline:** transactional outbox and competing-consumer patterns are established.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Mostly official support/version docs plus NuGet evidence; strong version clarity. |
| Features | MEDIUM | Good competitive/context synthesis, but some payment-provider feature assumptions inferred from standard patterns. |
| Architecture | MEDIUM-HIGH | Strong canonical patterns and Microsoft/PostgreSQL references; provider-specific adapter details remain open. |
| Pitfalls | MEDIUM-HIGH | Failure modes are realistic and backed by official concurrency/hosted-service guidance; some policy interpretation is contextual. |

**Overall confidence:** MEDIUM-HIGH

### Gaps to Address

- **Mercado Pago webhook/security details:** validate signature scheme, retry semantics, and event ordering guarantees in implementation planning.
- **WhatsApp integration contract:** confirm provider-specific delivery receipts, throttling, and template lifecycle constraints.
- **Catalog compliance policy:** formalize allowed/restricted categories and reviewer workflow consistent with Tibia rules and business risk tolerance.
- **SLA and hold-window policy:** decide product-tier thresholds for immediate delivery vs manual/risk hold.

## Sources

### Primary (HIGH confidence)
- .NET support policy: https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- ASP.NET Core 10 release notes: https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0
- EF Core release/support documentation: https://learn.microsoft.com/en-us/ef/core/what-is-new/
- PostgreSQL support/versioning: https://www.postgresql.org/support/versioning/
- PostgreSQL transaction isolation: https://www.postgresql.org/docs/current/transaction-iso.html
- EF Core concurrency docs: https://learn.microsoft.com/en-us/ef/core/saving/concurrency
- ASP.NET Core hosted services docs: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
- Npgsql provider docs: https://www.npgsql.org/efcore/
- NuGet version indexes (Npgsql, Hangfire, Serilog, OpenTelemetry, MercadoPago SDK) as cited in STACK.md
- `.planning/PROJECT.md`

### Secondary (MEDIUM confidence)
- Microsoft domain-analysis and competing-consumers architecture guidance:
  - https://learn.microsoft.com/en-us/azure/architecture/microservices/model/domain-analysis
  - https://learn.microsoft.com/en-us/azure/architecture/patterns/competing-consumers
- Microservices.io transactional outbox pattern: https://microservices.io/patterns/data/transactional-outbox.html
- Competitive signals:
  - https://www.eldorado.gg/tibia-gold/g/371
  - https://support.eldorado.gg/en/articles/8408994-tradeshield-buying
  - https://www.mmopixel.com/tibia-gold
  - https://mmoshops.com/Tibia-Gold%20-1

### Tertiary (LOW confidence / requires validation)
- Mercado Pago docs details partially inaccessible in this environment; webhook recommendations include informed inference from standard payment-integration reliability patterns.
- Tibia policy enforcement interpretation for commerce catalog gating is context-dependent despite official rules source: https://www.tibia.com/support/?subtopic=tibiarules

---
*Research completed: 2026-04-14*
*Ready for roadmap: yes*
