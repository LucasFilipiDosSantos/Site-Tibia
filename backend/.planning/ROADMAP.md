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
- [ ] **Phase 5: Order Lifecycle & Timeline Visibility** - Legal order state transitions with customer/admin tracking.
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
**Plans**: 3 plans

Plans:
- [ ] 01-01-PLAN.md — Define identity domain/application contracts with token, password, lockout invariants and unit tests.
- [ ] 01-02-PLAN.md — Implement EF persistence, repositories, JWT and password hashing adapters with DI wiring.
- [ ] 01-03-PLAN.md — Expose auth endpoints and enforce RBAC, verification gates, throttling/lockout, HTTPS hardening.

### Phase 2: Catalog & Product Governance
**Goal**: Customers can discover valid Tibia products and admins can maintain catalog data safely.
**Depends on**: Phase 1
**Requirements**: CAT-01, CAT-02, CAT-03, CAT-04
**Success Criteria** (what must be TRUE):
  1. Customer can browse products filtered by Aurera and Eternia server scope.
  2. Customer can access product groupings by Tibia goods/service category.
  3. Catalog and category endpoints resolve SEO-friendly slugs consistently.
  4. Admin can create and update product descriptions and pricing for catalog operations.
**Plans**: TBD

### Phase 3: Inventory Integrity & Reservation Control
**Goal**: Stock remains accurate under concurrent demand and checkout safety rules.
**Depends on**: Phase 2
**Requirements**: INV-01, INV-02, INV-03, INV-04
**Success Criteria** (what must be TRUE):
  1. System reports current available stock per product in real time.
  2. Pending-payment orders reserve stock and automatically release it on cancellation/expiration.
  3. Checkout blocks quantities that exceed currently available inventory.
  4. Admin stock adjustments are persisted with audit visibility.
**Plans**: TBD

### Phase 4: Cart & Checkout Capture
**Goal**: Authenticated customers can build carts and submit checkout with fulfillment-ready order input.
**Depends on**: Phase 3
**Requirements**: CHK-01, CHK-02, CHK-03
**Success Criteria** (what must be TRUE):
  1. Authenticated customer can add products to cart with selected quantity.
  2. Checkout creates an order containing immutable item price snapshots.
  3. Checkout records required delivery instructions for both manual and automated fulfillment paths.
**Plans**: TBD

### Phase 5: Order Lifecycle & Timeline Visibility
**Goal**: Orders follow legal lifecycle transitions and are traceable by customers and operators.
**Depends on**: Phase 4
**Requirements**: ORD-01, ORD-02, ORD-03, ORD-04
**Success Criteria** (what must be TRUE):
  1. Orders transition only through legal lifecycle states (Pending, Paid, Cancelled).
  2. Every order status change is stored as a timestamped history event.
  3. Customer can view order history with order-level status timeline.
  4. Admin can search and manage orders by status and customer.
**Plans**: TBD
**UI hint**: yes

### Phase 6: Mercado Pago Payment Confirmation
**Goal**: Verified Mercado Pago events reliably drive payment state and paid-order transitions.
**Depends on**: Phase 5
**Requirements**: PAY-01, PAY-02, PAY-03, PAY-04
**Success Criteria** (what must be TRUE):
  1. Checkout creates Mercado Pago payment requests linked to the exact target order.
  2. Webhook processing is idempotent so duplicate events do not duplicate order transitions.
  3. Payment status changes and webhook payload logs are stored for audit/debugging.
  4. Orders become Paid only after verified payment confirmation.
**Plans**: TBD

### Phase 7: Async Processing, Notifications & Monitoring
**Goal**: Critical side effects run durably with retries, while operators can observe flow health.
**Depends on**: Phase 6
**Requirements**: NTF-01, NTF-02, NTF-03, REL-01, REL-02
**Success Criteria** (what must be TRUE):
  1. Background jobs execute payment confirmation, fulfillment automation hooks, notification dispatch, and retry workloads.
  2. Key order/payment/delivery events enqueue WhatsApp notifications and optional email notifications.
  3. Transient notification failures retry automatically through background processing.
  4. Structured logs and monitoring signals expose critical flow outcomes for operations.
**Plans**: TBD

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
| 1. Identity & Security Foundation | 0/3 | Not started | - |
| 2. Catalog & Product Governance | 0/TBD | Not started | - |
| 3. Inventory Integrity & Reservation Control | 0/TBD | Not started | - |
| 4. Cart & Checkout Capture | 0/TBD | Not started | - |
| 5. Order Lifecycle & Timeline Visibility | 0/TBD | Not started | - |
| 6. Mercado Pago Payment Confirmation | 0/TBD | Not started | - |
| 7. Async Processing, Notifications & Monitoring | 0/TBD | Not started | - |
| 8. Fulfillment Orchestration | 0/TBD | Not started | - |
| 9. Custom Orders, Marketplace Assets & Admin Ops | 0/TBD | Not started | - |
