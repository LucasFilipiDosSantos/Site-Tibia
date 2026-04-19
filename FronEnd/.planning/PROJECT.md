# Tibia Webstore Frontend

## What This Is

This project is the customer-facing frontend for the Tibia-focused webstore. It provides the UI for browsing products, managing cart, checkout, and order tracking for virtual goods on Aurera and Eternia servers. It serves customers and connects to the backend API for all operations.

## Core Value

Customers can easily browse Tibia products, complete purchases, and track deliveries through an intuitive web interface.

## Requirements

### Validated

(None yet — ship to validate)

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

- Native mobile app — web-first scope
- Real-time chat support
- Advanced analytics dashboard

## Context

- Domain: MMORPG virtual goods e-commerce (Tibia gold, items, characters, services)
- Integrates with backend API at `/api` endpoints
- Uses React + TypeScript + Vite + Tailwind
- Design system: shadcn/ui (Radix UI components)
- State: TanStack Query for API, React Context for local state

## Constraints

- **Tech stack**: React 18 + TypeScript + Vite — required
- **Styling**: Tailwind CSS — required
- **UI**: shadcn/ui + Radix primitives — required
- **API**: Backend REST API — required for real operations
- **Auth**: JWT via backend — required
- **Payment**: Mercado Pago integration — required

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Frontend-only initialization | Backend already planned in backend/.planning | In progress |
| Connect to existing backend | Backend has Phase 5+ ready | — Pending |
| Use shadcn/ui pattern | Already in codebase | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-04-19 after codebase mapping*
