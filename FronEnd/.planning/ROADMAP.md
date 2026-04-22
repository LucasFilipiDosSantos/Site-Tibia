# Roadmap: Tibia Webstore Frontend

## Overview

Frontend phases targeting backend integration. Progress below reflects the current codebase, not the original empty-state bootstrap.

## Phases

- [~] **Phase 1: Foundation & API Integration** - Core setup, API client, auth flow
- [~] **Phase 2: Catalog & Product Discovery** - Product browsing with backend
- [ ] **Phase 3: Cart & Checkout Flow** - Cart operations and payment
- [ ] **Phase 4: Order Management** - Order history and tracking
- [ ] **Phase 5: Payment Integration** - Mercado Pago checkout
- [ ] **Phase 6: Delivery Tracking** - Order fulfillment visibility
- [ ] **Phase 7: Notifications & UX** - Feedback and responsiveness
- [ ] **Phase 8: Admin Dashboard** - Admin operations and metrics

## Phase Details

### Phase 1: Foundation & API Integration
**Goal**: Connect frontend to backend API with auth
**Status**: In progress
**Completed now**:
1. Shared API client created with auth header and refresh retry
2. User can login/register through backend JWT endpoints
3. Auth session persists locally and restores on reload

**Still missing**:
1. Dedicated forms for email verification and password reset
2. Protected pages still use placeholder loading states in several flows

### Phase 2: Catalog & Product Discovery
**Goal**: Product browsing connected to backend
**Status**: In progress
**Completed now**:
1. Homepage and listing load products from `/products`
2. Product detail loads from `/products/{slug}`
3. Category navigation now uses backend category slugs

**Blocked by backend contract gaps**:
1. Public catalog responses do not include `productId`
2. Public catalog responses do not include `server`
3. Public catalog responses do not include `stock`
4. Public catalog responses do not include merchandising metrics such as rating/sales

### Phase 3: Cart & Checkout Flow
**Goal**: Cart and checkout connected to backend
**Status**: Blocked
**Blocker**: Checkout/cart endpoints require GUID product IDs, but public catalog endpoints expose only slug-based product identity.

### Phase 4: Order Management
**Goal**: Order tracking UI connected to backend
**Status**: Not started

### Phase 5: Payment Integration
**Goal**: Mercado Pago checkout integration
**Status**: Not started

### Phase 6: Delivery Tracking
**Goal**: Fulfillment visibility in UI
**Status**: Not started

### Phase 7: Notifications & UX
**Goal**: Enhanced user feedback
**Status**: Partial
**Notes**: Loading and error states were added to public catalog pages, but the rest of the app still needs a UX audit.

### Phase 8: Admin Dashboard
**Goal**: Complete admin operations
**Status**: Not started

## Progress

| Phase | Status |
|-------|--------|
| 1. Foundation & API Integration | In progress |
| 2. Catalog & Product Discovery | In progress |
| 3. Cart & Checkout Flow | Blocked by missing product IDs in public catalog |
| 4. Order Management | Not started |
| 5. Payment Integration | Not started |
| 6. Delivery Tracking | Not started |
| 7. Notifications & UX | Partial |
| 8. Admin Dashboard | Not started |
