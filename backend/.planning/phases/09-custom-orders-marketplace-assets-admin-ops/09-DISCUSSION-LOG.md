# Phase 9: Custom Orders, Marketplace Assets & Admin Ops - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions captured in CONTEXT.md — this log preserves the analysis.

**Date:** 2026-04-18
**Phase:** 09-custom-orders-marketplace-assets-admin-ops
**Areas discussed:** Custom Orders, Marketplace Downloads, Admin Operations

---

## Custom Orders (CUS-01, CUS-02)

| Option | Description | Selected |
|--------|-------------|----------|
| Linked to Order | New CustomOrder entity with reference to the Order that purchased it | |
| Standalone | Independent from orders, tracked only by CustomOrder ID with separate purchase flow | |
| Product-like | Request created at checkout like any other product, special handling only for delivery | ✓ |

**User's choice:** Product-like (Recommended)
**Notes:** Custom requests are just a special product type at checkout

---

| Option | Description | Selected |
|--------|-------------|----------|
| Pending → InProgress → Delivered | Request requires admin review before work begins | ✓ |
| Available → Completed | Instantly available after payment, requires confirmation only | |
| Draft → Pending → Delivered | Customer describes need, admin approves before work starts | |

**User's choice:** Pending → InProgress → Delivered
**Notes:** Clear lifecycle that customer can track

---

## Marketplace Downloads (MKT-01, MKT-02)

| Option | Description | Selected |
|--------|-------------|----------|
| Entitlement check at download | Protected endpoint returning file blob, entitlement check before download | |
| Signed URL | Generate time-limited signed URL to cloud storage, check entitlement at generation only | ✓ |
| One-time token | Check entitlement and issue one-time token | |

**User's choice:** Signed URL (Recommended)
**Notes:** 15 min expiration, check at generation not at download

---

| Option | Description | Selected |
|--------|-------------|----------|
| Zero-price products | Products with price $0 that follow normal purchase flow | |
| Policy-based free access | Special endpoint for free downloads, controlled by a policy/allowlist | ✓ |
| Always require checkout | Need explicit purchase checkbox even for free items | |

**User's choice:** Policy-based free access (Recommended)
**Notes:** Free assets controlled by policy, not pricing

---

## Admin Operations (ADM-01, ADM-02, ADM-03)

| Option | Description | Selected |
|--------|-------------|----------|
| Extend existing | Extend existing admin endpoints, add new CRUD under same controllers | ✓ |
| Separate admin route group | Separate admin API prefix with dedicated endpoints and versioning | |
| Dashboard-only surface | Minimal: only what's needed for existing dashboard integration | |

**User's choice:** Extend existing (Recommended)
**Notes:** Extend what already exists

---

| Option | Description | Selected |
|--------|-------------|----------|
| Full audit trail | Separate AuditLog entity with actor, action, entity, before/after values | ✓ |
| Minimal event logging | Log only that action occurred, no before/after snapshot | |
| Reuse timeline events | Reuse existing timeline events table in each domain | |

**User's choice:** Full audit trail (Recommended)
**Notes:** Dedicated entity for admin audit

---

## Agent Discretion

Areas where user said "you decide":
- Exact signed URL storage backend
- Exact file hosting approach
- Exact audit log retention policy
- Exact DTO structures for admin endpoints
- Free access policy exact field structure