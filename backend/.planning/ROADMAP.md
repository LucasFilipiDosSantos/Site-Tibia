# Roadmap: Tibia Webstore Backend

## Overview

This roadmap delivers a reliability-first Tibia commerce backend from secure access and product/stock control through checkout, payment confirmation, delivery orchestration, and operational visibility. Phases are ordered by dependency so each one unlocks the next without leaving core v1 requirements orphaned.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

- [ ] **Phase 1: Identity & Security Foundation** - Secure authentication, authorization, and account protection baseline.
- [ ] **Phase 2: Catalog & Product Governance** - Server-scoped catalog discovery and product administration.
- [ ] **Phase 3: Inventory Integrity & Reservation Control** - Real-time stock control with reservation/release safeguards.
- [ ] **Phase 4: Cart & Checkout Capture** - Cart operations and checkout order creation with delivery instructions.
- [x] **Phase 5: Order Lifecycle & Timeline Visibility** - Legal order state transitions with customer/admin tracking. (completed 2026-04-17)
- [ ] **Phase 6: Mercado Pago Payment Confirmation** - Idempotent payment flow with verified paid transitions.
- [ ] **Phase 7: Async Processing, Notifications & Monitoring** - Durable background execution, lifecycle notifications, and observability.
- [ ] **Phase 8: Fulfillment Orchestration** - Automated/manual delivery routing and fulfillment correction workflows.
- [ ] **Phase 9: Custom Orders, Marketplace Assets & Admin Ops** - Custom request lifecycle, script/macro access, and admin audit operations.

## Phase Details

### Phase 1: Identity & Security Foundation
**Goal**: Users can securely access accounts and protected backend capabilities with role-safe boundaries.
**Depends on**: Nothing (first phase)
**Requirements**: AUTH-01, AUTH-02, AUTH-03, AUTH-04, SEC-01, SEC-02
**Success Criteria** (what must be TRUE):
  1. User can register, authenticate, and receive JWT access/refresh credentials.
  2. User can verify email ownership and complete password reset through secure tokenized flow.
  3. Session continuity works via refresh token rotation without weakening account security.
  4. Admin-only endpoints reject non-admin users while allowing authorized admin actions.
  5. Account credentials are protected with HTTPS transport and strong one-way password hashing.
**Plans**: 7 plans

Plans:
- [x] 01-01-PLAN.md — Define identity domain/application contracts with token, password, lockout invariants and unit tests.
- [x] 01-02-PLAN.md — Implement EF persistence, repositories, JWT and password hashing adapters with DI wiring.
- [x] 01-03-PLAN.md — Expose auth endpoints and enforce RBAC, verification gates, throttling/lockout, HTTPS hardening.
- [x] 01-04-PLAN.md — Close AUTH-02 gap by wiring verification/reset token delivery channel and round-trip tests.
- [x] 01-05-PLAN.md — Close AUTH-03 gap by configuring JWT bearer validation and admin positive-path authorization tests.
- [x] 01-06-PLAN.md — Close UAT blocker by mapping registration validation exceptions to ProblemDetails and adding weak/strong password HTTP contract tests.
- [x] 01-07-PLAN.md — Close remaining AUTH-02 verification gap with provider-backed external token delivery and integration proof.

### Phase 2: Catalog & Product Governance
**Goal**: Customers can discover valid Tibia products and admins can maintain catalog data safely.
**Depends on**: Phase 1
**Requirements**: CAT-01, CAT-02, CAT-03, CAT-04
**Alignment note:** CAT-01 server-filter phrasing is superseded in Phase 2 by locked decision D-14 (global catalog model without server segmentation).
**Success Criteria** (what must be TRUE):
  1. Customer can browse the global catalog using category and slug discovery without server-specific segmentation.
  2. Customer can access product groupings by Tibia goods/service category.
  3. Catalog and category endpoints resolve SEO-friendly slugs consistently.
  4. Admin can create and update product descriptions and pricing for catalog operations.
**Plans**: 3 plans

Plans:
- [x] 02-01-PLAN.md — Define catalog domain/application contracts and invariants with TDD for immutable slugs, category references, and filter+paging behavior.
- [x] 02-02-PLAN.md — Implement EF persistence/repositories plus blocking migration and schema push for catalog/category governance constraints.
- [x] 02-03-PLAN.md — Expose customer/admin catalog endpoints with RBAC integration tests and requirements alignment updates for the global catalog scope decision.

### Phase 3: Inventory Integrity & Reservation Control
**Goal**: Stock remains accurate under concurrent demand and checkout safety rules.
**Depends on**: Phase 2
**Requirements**: INV-01, INV-02, INV-03, INV-04
**Success Criteria** (what must be TRUE):
  1. System reports current available stock per product in real time.
  2. Pending-payment orders reserve stock and automatically release it on cancellation/expiration.
  3. Checkout blocks quantities that exceed currently available inventory.
  4. Admin stock adjustments are persisted with audit visibility.
**Plans**: 3 plans

Plans:
- [x] 03-01-PLAN.md — Define inventory domain/application contracts and TDD invariants for reservation lifecycle, idempotency, and auditable adjustments.
- [x] 03-02-PLAN.md — Implement EF persistence, transactional reservation repository, and blocking migration/schema push for inventory tables and audit trails.
- [x] 03-03-PLAN.md — Expose inventory endpoints with 409 ProblemDetails conflict semantics, AdminOnly adjustment routes, and end-to-end integration verification.

### Phase 4: Cart & Checkout Capture
**Goal**: Authenticated customers can build carts and submit checkout with fulfillment-ready order input.
**Depends on**: Phase 3
**Requirements**: CHK-01, CHK-02, CHK-03
**Success Criteria** (what must be TRUE):
  1. Authenticated customer can add products to cart with selected quantity.
  2. Checkout creates an order containing immutable item price snapshots.
  3. Checkout records required delivery instructions for both manual and automated fulfillment paths.
**Plans**: 7 plans

Plans:
- [x] 04-01-PLAN.md — Define cart domain/application contracts and TDD invariants for merge/set/remove/conflict semantics.
- [x] 04-02-PLAN.md — Implement checkout domain/application TDD flow for immutable snapshots, delivery-instruction validation, and atomic reserve behavior.
- [x] 04-03-PLAN.md — Implement checkout/cart persistence plus blocking migration/schema push and persistence integration verification.
- [x] 04-04-PLAN.md — Expose cart/checkout API contracts with auth + 409 ProblemDetails mapping and end-to-end integration proof.
- [x] 04-05-PLAN.md — Close D-14 atomic checkout gap by adding reservation compensation rollback and explicit no-partial-side-effect verification.
- [x] 04-06-PLAN.md — Close remaining D-14 inventory idempotency gap by product-scoping reserve replay semantics and proving real-path multi-line reserve-all atomicity.
- [x] 04-07-PLAN.md — Close remaining D-14 compensation gap by releasing all reservations for shared checkout intent and proving 3+ line late-conflict rollback leaves zero residual reservations.

### Phase 5: Order Lifecycle & Timeline Visibility
**Goal**: Orders follow legal lifecycle transitions and are traceable by customers and operators.
**Depends on**: Phase 4
**Requirements**: ORD-01, ORD-02, ORD-03, ORD-04
**Success Criteria** (what must be TRUE):
  1. Orders transition only through legal lifecycle states (Pending, Paid, Cancelled).
  2. Every order status change is stored as a timestamped history event.
  3. Customer can view order history with order-level status timeline.
  4. Admin can search and manage orders by status and customer.
**Plans**: 3 plans
**UI hint**: yes

Plans:
- [x] 05-01-PLAN.md — Define lifecycle state-machine/timeline domain contracts with TDD and transition authority/idempotency proofs.
- [x] 05-02-PLAN.md — Implement lifecycle persistence/event history repositories with blocking migration/schema update and integration persistence verification.
- [x] 05-03-PLAN.md — Expose customer timeline/admin order management endpoints with explicit actions, conflict ProblemDetails, and end-to-end contract tests.

### Phase 6: Mercado Pago Payment Confirmation
**Goal**: Mercado Pago SDK-backed payment creation and verified webhook confirmations reliably drive payment state and paid-order transitions.
**Depends on**: Phase 5
**Requirements**: PAY-01, PAY-02, PAY-03, PAY-04
**Success Criteria** (what must be TRUE):
  1. Checkout uses the Mercado Pago .NET SDK (`MercadoPagoConfig.AccessToken`, `PreferenceClient`/payment client) to create payment requests linked to one exact order external reference.
  2. Webhook handler validates notification origin via `x-signature` (`ts`,`v1`) HMAC-SHA256 secret verification before applying any order transition.
  3. Webhook processing is idempotent (provider event/payment id + local idempotency guard) so retries/duplicates do not duplicate payment logs or order transitions.
  4. Payment status changes and raw/minified webhook payload logs are persisted with processing outcome, request id, and timestamps for admin inspection.
  5. Orders move to `Paid` only after a verified approved/processed confirmation path; invalid signatures or non-approved statuses never mark paid.
  6. Webhook endpoint acknowledges with `200/201` quickly and defers heavy processing to durable async flow to respect provider retry semantics.
**Plans**: 3 plans

Plans:
- [x] 06-01-PLAN.md — Add Mercado Pago SDK payment request creation flow linked to immutable order reference and checkout return metadata.
- [x] 06-02-PLAN.md — Implement webhook endpoint + signature validation + idempotent payment event processing and persistence audit trail.
- [x] 06-03-PLAN.md — Wire verified payment confirmation to legal lifecycle transition to `Paid`, with conflict-safe behavior and integration tests.

### Phase 7: Async Processing, Notifications & Monitoring
**Goal**: Critical side effects run durably with retries, while operators can observe flow health.
**Depends on**: Phase 6
**Requirements**: NTF-01, NTF-02, NTF-03, REL-01, REL-02
**Success Criteria** (what must be TRUE):
  1. Background jobs execute payment confirmation, fulfillment automation hooks, notification dispatch, and retry workloads.
  2. Key order/payment/delivery events enqueue WhatsApp notifications and optional email notifications.
  3. Transient notification failures retry automatically through background processing.
  4. Structured logs and monitoring signals expose critical flow outcomes for operations.
**Plans**: 3 plans

Plans:
- [ ] 07-01-PLAN.md — Hangfire infrastructure setup with PostgreSQL storage, dashboard, health checks
- [ ] 07-02-PLAN.md — WhatsApp notification service via Meta Cloud API
- [ ] 07-03-PLAN.md — Event-to-notification wiring with retry jobs

### Phase 8: Fulfillment Orchestration
**Goal**: Paid orders reach completion through tracked automated/manual fulfillment workflows.
**Depends on**: Phase 7
**Requirements**: FUL-01, FUL-02, FUL-03, FUL-04
**Success Criteria** (what must be TRUE):
  1. Paid order items are routed to automated or manual delivery paths based on product type.
  2. Delivery status is tracked per order with delivery type and completion timestamp.
  3. Customer can view delivery progress for each order.
  4. Admin can manually complete or correct fulfillment tasks when automation fails.
**Plans**: TBD
**UI hint**: yes

### Phase 9: Custom Orders, Marketplace Assets & Admin Ops
**Goal**: Specialized product flows and operational governance are complete for production operation.
**Depends on**: Phase 8
**Requirements**: CUS-01, CUS-02, MKT-01, MKT-02, ADM-01, ADM-02, ADM-03
**Success Criteria** (what must be TRUE):
  1. Customer can submit custom script/macro requests and track lifecycle status.
  2. Admin can update custom order progression from Pending to InProgress to Delivered.
  3. Entitled users can download paid scripts/macros, and policy-allowed free assets are downloadable without payment.
  4. Admin APIs support product, stock, user, and order operations required by existing dashboard integration.
  5. Critical write actions and webhook processing outcomes are auditable via admin-inspectable logs.
**Plans**: TBD
**UI hint**: yes

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Identity & Security Foundation | 6/7 | In progress | - |
| 2. Catalog & Product Governance | 0/3 | Not started | - |
| 3. Inventory Integrity & Reservation Control | 0/TBD | Not started | - |
| 4. Cart & Checkout Capture | 0/TBD | Not started | - |
| 5. Order Lifecycle & Timeline Visibility | 3/3 | Complete   | 2026-04-17 |
| 6. Mercado Pago Payment Confirmation | 0/3 | Not started | - |
| 7. Async Processing, Notifications & Monitoring | 0/TBD | Not started | - |
| 8. Fulfillment Orchestration | 0/TBD | Not started | - |
| 9. Custom Orders, Marketplace Assets & Admin Ops | 0/TBD | Not started | - |
