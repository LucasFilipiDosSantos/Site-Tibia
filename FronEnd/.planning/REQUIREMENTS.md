# Requirements: Tibia Webstore Frontend

**Defined:** 2026-04-19
**Core Value:** Customers can easily browse Tibia products, complete purchases, and track deliveries through an intuitive web interface.

## v1 Requirements

### Catalog

- [ ] **CAT-01**: User can browse products filtered by Tibia server (Aurera or Eternia)
- [ ] **CAT-02**: User can view products categorized by type (gold, items, characters, Tibia Coins, scripts, macros, services)
- [ ] **CAT-03**: User can access products and categories through SEO-friendly slugs
- [ ] **CAT-04**: Admin can create and update product metadata including descriptions and pricing

### Inventory

- [ ] **INV-01**: User can see available stock per product in real time
- [ ] **INV-02**: System warns when requested quantity exceeds available stock
- [ ] **INV-03**: Admin can adjust stock with audit visibility

### Cart and Checkout

- [ ] **CHK-01**: Authenticated user can add products to cart with quantity
- [ ] **CHK-02**: User can submit checkout creating an order with immutable item price snapshot
- [ ] **CHK-03**: User can enter delivery instructions required for manual fulfillment

### Orders

- [ ] **ORD-01**: User can view order history with status timeline
- [ ] **ORD-02**: User can track current order status
- [ ] **ORD-03**: Admin can search and manage orders by status and customer

### Payments

- [ ] **PAY-01**: System presents Mercado Pago payment interface for order
- [ ] **PAY-02**: System handles payment redirect and success/cancel callbacks
- [ ] **PAY-03**: System shows payment status and confirmation

### Fulfillment

- [ ] **FUL-01**: User can view delivery progress for each order
- [ ] **FUL-02**: Admin can manually complete or correct fulfillment

### Authentication and Access

- [ ] **AUTH-01**: User can register and authenticate via backend JWT
- [ ] **AUTH-02**: User can verify email and reset password
- [ ] **AUTH-03**: System enforces role-based access for admin areas

### Notifications

- [ ] **NTF-01**: User sees order/payment/delivery notifications in UI

### Admin Operations

- [ ] **ADM-01**: Admin can manage products, stock, users, and orders
- [ ] **ADM-02**: Admin has dashboard with key metrics

### User Experience

- [ ] **UX-01**: Responsive design works on mobile and desktop
- [ ] **UX-02**: Loading states shown during API operations
- [ ] **UX-03**: Error messages displayed clearly

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| CAT-01 | Phase 2 | Pending |
| CAT-02 | Phase 2 | Pending |
| CAT-03 | Phase 2 | Pending |
| CAT-04 | Phase 2 | Pending |
| INV-01 | Phase 2 | Pending |
| INV-02 | Phase 2 | Pending |
| INV-03 | Phase 2 | Pending |
| CHK-01 | Phase 3 | Pending |
| CHK-02 | Phase 3 | Pending |
| CHK-03 | Phase 3 | Pending |
| ORD-01 | Phase 4 | Pending |
| ORD-02 | Phase 4 | Pending |
| ORD-03 | Phase 4 | Pending |
| PAY-01 | Phase 5 | Pending |
| PAY-02 | Phase 5 | Pending |
| PAY-03 | Phase 5 | Pending |
| FUL-01 | Phase 6 | Pending |
| FUL-02 | Phase 6 | Pending |
| AUTH-01 | Phase 1 | Pending |
| AUTH-02 | Phase 1 | Pending |
| AUTH-03 | Phase 1 | Pending |
| NTF-01 | Phase 7 | Pending |
| ADM-01 | Phase 8 | Pending |
| ADM-02 | Phase 8 | Pending |
| UX-01 | Phase 1 | Pending |
| UX-02 | Phase 1 | Pending |
| UX-03 | Phase 1 | Pending |

---
*Requirements defined: 2026-04-19*