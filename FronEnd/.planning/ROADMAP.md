# Roadmap: Tibia Webstore Frontend

## Overview

Frontend phases targeting backend integration. Phases ordered by dependency.

## Phases

- [ ] **Phase 1: Foundation & API Integration** - Core setup, API client, auth flow
- [ ] **Phase 2: Catalog & Product Discovery** - Product browsing with backend
- [ ] **Phase 3: Cart & Checkout Flow** - Cart operations and payment
- [ ] **Phase 4: Order Management** - Order history and tracking
- [ ] **Phase 5: Payment Integration** - Mercado Pago checkout
- [ ] **Phase 6: Delivery Tracking** - Order fulfillment visibility
- [ ] **Phase 7: Notifications & UX** - Feedback and responsiveness
- [ ] **Phase 8: Admin Dashboard** - Admin operations and metrics

## Phase Details

### Phase 1: Foundation & API Integration
**Goal**: Connect frontend to backend API with auth
**Depends on**: Nothing
**Requirements**: AUTH-01, AUTH-02, AUTH-03, UX-01, UX-02, UX-03
**Success Criteria**:
1. Frontend connects to backend API endpoints
2. User can register/login with JWT
3. API client handles errors gracefully
4. Responsive layout works on mobile
**Plans**: 3 plans
- [ ] 01-01 — API client setup with TanStack Query
- [ ] 01-02 — Auth flow integration with JWT
- [ ] 01-03 — Responsive layout verification

### Phase 2: Catalog & Product Discovery
**Goal**: Product browsing connected to backend
**Depends on**: Phase 1
**Requirements**: CAT-01, CAT-02, CAT-03, CAT-04, INV-01, INV-02, INV-03
**Success Criteria**:
1. Products load from backend API
2. Category and server filtering works
3. Product detail shows stock in real-time
4. Admin product management works
**Plans**: 3 plans
- [ ] 02-01 — Product list API integration
- [ ] 02-02 — Product detail and filtering
- [ ] 02-03 — Admin product CRUD

### Phase 3: Cart & Checkout Flow
**Goal**: Cart and checkout connected to backend
**Depends on**: Phase 2
**Requirements**: CHK-01, CHK-02, CHK-03
**Success Criteria**:
1. Cart persists and syncs with backend
2. Checkout creates order via API
3. Delivery instructions captured
**Plans**: 3 plans
- [ ] 03-01 — Cart API integration
- [ ] 03-02 — Checkout flow
- [ ] 03-03 — Order creation API

### Phase 4: Order Management
**Goal**: Order tracking UI connected to backend
**Depends on**: Phase 3
**Requirements**: ORD-01, ORD-02, ORD-03
**Success Criteria**:
1. User sees order history from API
2. Order status timeline visible
3. Admin can filter/manage orders
**Plans**: 3 plans
- [ ] 04-01 — Order history API
- [ ] 04-02 — Order detail and timeline
- [ ] 04-03 — Admin order management

### Phase 5: Payment Integration
**Goal**: Mercado Pago checkout integration
**Depends on**: Phase 4
**Requirements**: PAY-01, PAY-02, PAY-03
**Success Criteria**:
1. Checkout redirects to Mercado Pago
2. Success/cancel handled
3. Payment status reflected in order
**Plans**: 2 plans
- [ ] 05-01 — Mercado Pago SDK integration
- [ ] 05-02 — Payment callbacks

### Phase 6: Delivery Tracking
**Goal**: Fulfillment visibility in UI
**Depends on**: Phase 5
**Requirements**: FUL-01, FUL-02
**Success Criteria**:
1. Delivery status visible per order
2. Admin can update fulfillment
**Plans**: 2 plans
- [ ] 06-01 — Delivery status display
- [ ] 06-02 — Admin fulfillment actions

### Phase 7: Notifications & UX
**Goal**: Enhanced user feedback
**Depends on**: Phase 6
**Requirements**: NTF-01, UX-01, UX-02, UX-03
**Success Criteria**:
1. Toast notifications for events
2. Loading states everywhere
3. Mobile responsive confirmed
**Plans**: 2 plans
- [ ] 07-01 — Toast notification system
- [ ] 07-02 — Loading states audit

### Phase 8: Admin Dashboard
**Goal**: Complete admin operations
**Depends on**: Phase 7
**Requirements**: ADM-01, ADM-02
**Success Criteria**:
1. Dashboard shows metrics
2. All CRUD operations functional
**Plans**: 2 plans
- [ ] 08-01 — Dashboard metrics
- [ ] 08-02 — Admin CRUD full

## Progress

| Phase | Plans | Status |
|-------|------|-------|
| 1. Foundation & API Integration | 0/3 | Not started |
| 2. Catalog & Product Discovery | 0/3 | Not started |
| 3. Cart & Checkout Flow | 0/3 | Not started |
| 4. Order Management | 0/3 | Not started |
| 5. Payment Integration | 0/2 | Not started |
| 6. Delivery Tracking | 0/2 | Not started |
| 7. Notifications & UX | 0/2 | Not started |
| 8. Admin Dashboard | 0/2 | Not started |