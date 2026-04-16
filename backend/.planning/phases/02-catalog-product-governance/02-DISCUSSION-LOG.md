# Phase 2: Catalog & Product Governance - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md - this log preserves the alternatives considered.

**Date:** 2026-04-16T00:00:00Z
**Phase:** 02-catalog-product-governance
**Areas discussed:** Catalog query shape, Slug rules, Category and server taxonomy, Admin product mutation contract

---

## Catalog query shape

| Option | Description | Selected |
|--------|-------------|----------|
| Single products endpoint | `GET /products` with query filters (`server`, `category`, `slug`) for one consistent contract and easier composition. | ✓ |
| Split discovery endpoints | Separate endpoints for server/category lists and product retrieval; more explicit but more API surface. | |
| Hybrid | Primary `/products` plus helper endpoints (for example categories metadata) where useful. | |

**User's choice:** Single products endpoint
**Notes:** Filter semantics locked to AND. Slug lookup locked to dedicated endpoint `GET /products/{slug}`.

---

## Slug rules

| Option | Description | Selected |
|--------|-------------|----------|
| Global unique slugs | One product slug across entire catalog; simple SEO and unambiguous `/products/{slug}` resolution. | ✓ |
| Unique per server | Same slug can exist in Aurera and Eternia separately; needs server-aware lookup enforcement. | |
| Unique per category | Same slug allowed in different categories; requires category context to disambiguate. | |

**User's choice:** Global unique slugs
**Notes:** Product slug is immutable after create. Category slug policy selected as fixed/immutable.

---

## Category and server taxonomy

| Option | Description | Selected |
|--------|-------------|----------|
| Explicit enum field | Product has server scope as enum (`Aurera`, `Eternia`); strong validation and clear filtering. | |
| Free-text server name | Flexible strings but weaker validation and inconsistent filtering risk. | |
| Separate mapping table | Supports future multi-server per product but adds complexity for current v1 scope. | |
| No server on product model | Product model is global and not segmented by server. | ✓ |

| Option | Description | Selected |
|--------|-------------|----------|
| Predefined enum + slug map | Strongly typed categories with fixed slugs; safe and consistent for v1. | |
| Database category entity | Admin-manageable category model in persistence. | ✓ |
| Free-text type on product | Fastest to start but weak validation and filtering consistency. | |

**User's choice:** No server on product model; Database category entity
**Notes:** User clarified catalog vision is "just products" with no Aurera/Eternia split. Category slug should be immutable.

---

## Admin product mutation contract

| Option | Description | Selected |
|--------|-------------|----------|
| PUT replace model | Single full-update contract with explicit required fields; clearer validation and consistency. | ✓ |
| PATCH partial updates | Flexible for dashboards, but more conditional validation paths. | |
| Both PUT and PATCH | Max flexibility, larger API surface and test matrix. | |

| Option | Description | Selected |
|--------|-------------|----------|
| Decimal > 0 required | Reject zero/negative values; commerce-safe baseline. | |
| Allow zero price | Supports freebies in same flow. | ✓ |
| Price + currency object | Multi-currency ready, broader scope. | |

| Option | Description | Selected |
|--------|-------------|----------|
| Category by slug | Stable human-readable relation in write contract. | ✓ |
| Category by ID | Simple FK-style writes; less ergonomic externally. | |
| Accept both slug and ID | Flexible but broader validation surface. | |

| Option | Description | Selected |
|--------|-------------|----------|
| Reject immutable slug edits | Strict immutable contract with validation error. | ✓ |
| Silently ignore field | Lenient but can hide dashboard bugs. | |
| Allow privileged override | Escape hatch with added policy complexity. | |

**User's choice:** PUT replace model; allow zero price; category relation by slug; reject immutable slug edits
**Notes:** Validation strictness preferred for immutable fields.

---

## the agent's Discretion

- Exact pagination strategy for `GET /products` list responses.
- Exact ProblemDetails error code names while preserving chosen behavior.

## Deferred Ideas

- Server-scoped product filtering from CAT-01 deferred for roadmap/requirements alignment with global-product catalog vision.
