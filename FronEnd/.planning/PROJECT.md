# Tibia Webstore Frontend

## What This Is

This project is the customer-facing frontend for the Tibia-focused webstore. It provides the UI for browsing products, authenticating users, managing a local cart, and preparing checkout/order flows against the backend API.

## Core Value

Customers can browse Tibia products and authenticate against the platform through a reliable web interface that is aligned with the real backend contracts.

## Requirements

### Validated

- [x] Frontend can authenticate against backend JWT endpoints
- [x] Frontend can browse catalog data from the backend public catalog endpoints

### Active

- [ ] User can browse and filter products by category and server
- [ ] User can view product details with stock and pricing
- [ ] User can add products to cart with quantity selection
- [ ] User can complete checkout with payment integration
- [ ] User can view order history and tracking
- [ ] User can manage account profile
- [ ] Admin can manage products, orders, users, and inventory
- [ ] Responsive design for mobile and desktop

### Out of Scope

- Native mobile app - web-first scope
- Real-time chat support
- Advanced analytics dashboard

## Context

- Domain: MMORPG virtual goods e-commerce (Tibia gold, items, characters, services)
- Backend endpoints currently used: `/auth/*` and `/products*`
- Uses React + TypeScript + Vite + Tailwind
- Design system: shadcn/ui (Radix UI components)
- State: TanStack Query for remote catalog data, React Context for auth and cart

## Constraints

- **Tech stack**: React 18 + TypeScript + Vite - required
- **Styling**: Tailwind CSS - required
- **UI**: shadcn/ui + Radix primitives - required
- **API**: Backend REST API - required for real operations
- **Auth**: JWT via backend - required
- **Payment**: Mercado Pago integration - required

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Switch frontend auth from mock to backend JWT flow | Backend auth endpoints are already available and unblock Phase 1 | Done |
| Switch public catalog from mock data to backend `/products` endpoints | Makes homepage, listing, and details reflect real API contracts | Done |
| Keep cart local for now | Backend public catalog does not expose product IDs required by checkout/cart endpoints | Temporary |
| Treat missing `server`, `stock`, `rating`, and `productId` fields as a backend contract gap | Frontend cannot reliably implement server filtering or real checkout wiring without them | Open |

## Evolution

This document evolves at phase transitions and milestone boundaries.

---
*Last updated: 2026-04-19 after frontend API/auth integration pass*
