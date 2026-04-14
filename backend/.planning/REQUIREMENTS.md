# Requirements: Tibia Webstore Backend

**Defined:** 2026-04-14
**Core Value:** Customers can reliably buy Tibia goods and receive confirmed, trackable delivery with minimal manual intervention.

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Catalog

- [ ] **CAT-01**: User can browse products filtered by Tibia server (Aurera or Eternia)
- [ ] **CAT-02**: User can view products categorized by type (gold, items, characters, Tibia Coins, scripts, macros, services)
- [ ] **CAT-03**: User can access products and categories through SEO-friendly slugs
- [ ] **CAT-04**: Admin can create and update product metadata including descriptions and pricing

### Inventory

- [ ] **INV-01**: System can track available stock per product in real time
- [ ] **INV-02**: System can reserve stock when an order is pending payment and release it on cancellation/expiration
- [ ] **INV-03**: System prevents checkout when requested quantity exceeds available stock
- [ ] **INV-04**: Admin can adjust stock with audit visibility

### Cart and Checkout

- [ ] **CHK-01**: Authenticated user can add products to cart with quantity
- [ ] **CHK-02**: User can submit checkout creating an order with immutable item price snapshot
- [ ] **CHK-03**: System records delivery instructions required for manual and automated fulfillment paths

### Orders

- [ ] **ORD-01**: System tracks order lifecycle states (Pending, Paid, Cancelled) with legal transitions only
- [ ] **ORD-02**: System stores order status history with timestamped events
- [ ] **ORD-03**: User can view order history and order-level status timeline
- [ ] **ORD-04**: Admin can search and manage orders by status and customer

### Payments

- [ ] **PAY-01**: System can create Mercado Pago payment requests linked to a specific order
- [ ] **PAY-02**: System processes Mercado Pago webhooks idempotently to confirm payments without duplicate order transitions
- [ ] **PAY-03**: System records payment status changes and payload logs for debugging and audits
- [ ] **PAY-04**: Paid status is applied only after verified payment confirmation event

### Fulfillment

- [ ] **FUL-01**: System can route paid order items to automated or manual delivery workflows based on product type
- [ ] **FUL-02**: System tracks delivery status per order with delivery type and completion timestamp
- [ ] **FUL-03**: User can see delivery progress from customer area
- [ ] **FUL-04**: Admin can manually complete or correct fulfillment tasks when automation fails

### Authentication and Access

- [ ] **AUTH-01**: User can register and authenticate via JWT access token and refresh token flow
- [x] **AUTH-02**: User can verify email and reset password via secure tokenized flow
- [ ] **AUTH-03**: System enforces role-based access so admin actions are restricted to authorized users
- [ ] **AUTH-04**: User session can persist securely across requests via refresh token rotation

### Custom Orders and Marketplace Assets

- [ ] **CUS-01**: User can submit custom script or macro requests with description and lifecycle tracking
- [ ] **CUS-02**: Admin can update custom order status (Pending, InProgress, Delivered)
- [ ] **MKT-01**: User can purchase paid scripts/macros and download files when entitled
- [ ] **MKT-02**: User can download free scripts/macros without payment when allowed by policy

### Notifications

- [ ] **NTF-01**: System can enqueue WhatsApp sale notifications on key order/payment/delivery events
- [ ] **NTF-02**: System can send optional email notifications for the same lifecycle events
- [ ] **NTF-03**: System retries transient notification failures through background processing

### Admin Operations and Audit

- [ ] **ADM-01**: Admin can manage products, stock, users, and orders through backend APIs compatible with existing dashboard integration
- [ ] **ADM-02**: System records audit logs for critical write actions and status changes
- [ ] **ADM-03**: Admin can inspect payment webhook logs and processing outcomes

### Security and Reliability

- [ ] **SEC-01**: System enforces HTTPS-only communication in deployed environments
- [ ] **SEC-02**: System stores passwords using strong one-way hashing
- [ ] **REL-01**: System executes background jobs for payment confirmation, delivery automation, notifications, and failed-operation retries
- [ ] **REL-02**: System provides structured logging and monitoring signals for critical flows

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Risk and Scale

- **RSK-01**: System applies anti-fraud rules for suspicious orders before fulfillment
- **RSK-02**: System applies API rate limiting policies per client and endpoint class
- **SCL-01**: System supports multi-server and multi-region scaling strategy with explicit data partitioning rules

### Growth and Analytics

- **ANL-01**: Admin can view advanced commerce analytics and cohort-level performance reports

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Full frontend rewrite | Scope is backend-only initialization and planning; frontend is intentionally untouched |
| Native mobile apps | API-first backend supports mobile clients later, but app development is not part of v1 |
| Multi-game commerce platform | v1 focus is Tibia-specific operations and reliability |
| Real-time chat support in customer area | Not core to checkout-payment-fulfillment value chain for initial release |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| CAT-01 | Phase 2 | Pending |
| CAT-02 | Phase 2 | Pending |
| CAT-03 | Phase 2 | Pending |
| CAT-04 | Phase 2 | Pending |
| INV-01 | Phase 3 | Pending |
| INV-02 | Phase 3 | Pending |
| INV-03 | Phase 3 | Pending |
| INV-04 | Phase 3 | Pending |
| CHK-01 | Phase 4 | Pending |
| CHK-02 | Phase 4 | Pending |
| CHK-03 | Phase 4 | Pending |
| ORD-01 | Phase 5 | Pending |
| ORD-02 | Phase 5 | Pending |
| ORD-03 | Phase 5 | Pending |
| ORD-04 | Phase 5 | Pending |
| PAY-01 | Phase 6 | Pending |
| PAY-02 | Phase 6 | Pending |
| PAY-03 | Phase 6 | Pending |
| PAY-04 | Phase 6 | Pending |
| FUL-01 | Phase 8 | Pending |
| FUL-02 | Phase 8 | Pending |
| FUL-03 | Phase 8 | Pending |
| FUL-04 | Phase 8 | Pending |
| AUTH-01 | Phase 1 | Pending |
| AUTH-02 | Phase 1 | Complete |
| AUTH-03 | Phase 1 | Pending |
| AUTH-04 | Phase 1 | Pending |
| CUS-01 | Phase 9 | Pending |
| CUS-02 | Phase 9 | Pending |
| MKT-01 | Phase 9 | Pending |
| MKT-02 | Phase 9 | Pending |
| NTF-01 | Phase 7 | Pending |
| NTF-02 | Phase 7 | Pending |
| NTF-03 | Phase 7 | Pending |
| ADM-01 | Phase 9 | Pending |
| ADM-02 | Phase 9 | Pending |
| ADM-03 | Phase 9 | Pending |
| SEC-01 | Phase 1 | Pending |
| SEC-02 | Phase 1 | Pending |
| REL-01 | Phase 7 | Pending |
| REL-02 | Phase 7 | Pending |

**Coverage:**
- v1 requirements: 40 total
- Mapped to phases: 40
- Unmapped: 0 ✓

---
*Requirements defined: 2026-04-14*
*Last updated: 2026-04-14 after roadmap mapping*
