# Tibia Webstore Backend

## What This Is

This project is a backend platform for a Tibia-focused webstore that sells virtual goods and services for the Aurera and Eternia servers. It supports the full commerce lifecycle: product catalog, inventory control, ordering, payment confirmation, and delivery orchestration. It is built to serve both customers (self-service purchasing and tracking) and operators (admin management and fulfillment workflows).

## Core Value

Customers can reliably buy Tibia goods and receive confirmed, trackable delivery with minimal manual intervention.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] End-to-end digital commerce flow for Tibia products from catalog to fulfilled delivery
- [ ] Payment-driven automation with Mercado Pago webhooks and resilient order state transitions
- [ ] Admin and customer operational visibility for orders, stock, custom requests, and notifications

### Out of Scope

- Native mobile app clients — backend-first scope targets web and API consumption
- Anti-fraud and advanced analytics in v1 — explicitly listed as future enhancements after core platform stability

## Context

- Domain: MMORPG virtual goods commerce (gold, items, characters, Tibia Coins, scripts, macros, and services)
- Product delivery model mixes automated fulfillment (digital assets) and manual workflows (characters and gold)
- Existing business requirement includes WhatsApp notifications and optional email notifications
- Payment processor is Mercado Pago with webhook-based confirmation and payment logging for audit/debugging
- Architecture direction is Clean Architecture + DDD with explicit layers (API, Application, Domain, Infrastructure)
- Persistence target is PostgreSQL with domain entities already outlined for users, sessions, products, stock, carts, orders, payments, deliveries, notifications, coupons, and audit logs

## Constraints

- **Tech stack**: C# with .NET and ASP.NET Core Web API — required to align with existing implementation direction
- **Persistence**: PostgreSQL — required for transactional consistency and relational domain modeling
- **Architecture**: Clean Architecture + DDD layering — required for maintainability and bounded-context clarity
- **Integrations**: Mercado Pago and WhatsApp API — required to support payment processing and operational alerts
- **Security**: HTTPS, password hashing, email verification, password reset — required to protect accounts and transactions
- **Reliability**: Background jobs for retries, payment confirmation, and delivery automation — required to reduce fulfillment failures

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Build backend scope only for this initialization | User explicitly requested backend-only planning and no frontend changes | — Pending |
| Use auto-mode workflow initialization | Provided requirements document is detailed and suitable for direct synthesis | — Pending |
| Commit planning artifacts to git | Selected in workflow preferences for traceability and collaboration | — Pending |
| Use Fine granularity with parallel execution and quality agents enabled | Chosen to decompose broad scope into focused, verifiable phases | — Pending |
| Close v1 milestone and carry unresolved backend P0/P1 items forward explicitly | Prevent hidden debt and planning drift between completed and upcoming milestone work | v1 marked complete; carryover tracked in `260418-tlr-gap-register.md` |
| Initialize next backend milestone with Phase 10 hardening-first scope | Highest-risk reliability/security/performance gaps remain and should precede new feature expansion | Phase 10 bootstrap created as backend-only, no-priority-downgrade boundary |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? -> Move to Out of Scope with reason
2. Requirements validated? -> Move to Validated with phase reference
3. New requirements emerged? -> Add to Active
4. Decisions to log? -> Add to Key Decisions
5. "What This Is" still accurate? -> Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-04-19 after v1 milestone closure and Phase 10 bootstrap*
