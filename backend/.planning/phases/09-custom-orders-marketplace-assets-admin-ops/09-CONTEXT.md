# Phase 9: Custom Orders, Marketplace Assets & Admin Ops - Context

**Gathered:** 2026-04-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Specialized product flows and operational governance:
1. Custom script/macro requests with lifecycle tracking (Pending → InProgress → Delivered)
2. Downloadable marketplace assets (paid + free) with entitlement-based access
3. Admin CRUD APIs + audit logging + webhook log inspection

</domain>

<decisions>
## Implementation Decisions

### Custom Orders (CUS-01, CUS-02)
- **D-01:** Custom requests modeled as product-like items at checkout
- **D-02:** Status lifecycle: Pending → InProgress → Delivered
- **D-03:** Linked to Order via OrderItemSnapshot (same as regular products)
- **D-04:** Customer can track status via order detail or dedicated custom order view
- **D-05:** Admin can update status through order item or dedicated admin interface

### Marketplace Downloads (MKT-01, MKT-02)
- **D-06:** Paid downloads: entitlement check at signed URL generation, not at download access
- **D-07:** Signed URL expiration: 15 minutes (configurable)
- **D-08:** Free assets: policy-based access without checkout/payment
- **D-09:** Policy controls which product categories are free-downloadable to which user roles
- **D-10:** Service stores/download serves file blob with proper content-type headers

### Admin Operations (ADM-01, ADM-02, ADM-03)
- **D-11:** Admin CRUD APIs extend existing admin endpoints under same controllers
- **D-12:** Full audit trail for critical write actions: actor, action, entity, before/after values
- **D-13:** Audit events stored in dedicated AuditLog entity
- **D-14:** Admin can inspect webhook processing logs (from Phase 6) via admin API
- **D-15:** Products, stock, users, orders CRUD via admin endpoints

### Agent Discretion
- Exact signed URL storage backend (local file, cloud storage abstraction)
- Exact file hosting approach (static files, blob storage, CDN)
- Exact audit log retention policy
- Exact DTO structures for admin endpoints
- Free access policy exact field structure

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Requirements
- `.planning/ROADMAP.md` § Phase 9 — CUS-01, CUS-02, MKT-01, MKT-02, ADM-01, ADM-02, ADM-03
- `.planning/REQUIREMENTS.md` — Custom Orders and Marketplace Assets requirements

### Upstream Context
- `.planning/phases/08-fulfillment-orchestration/08-CONTEXT.md` — Delivery status model (extends to custom)
- `.planning/phases/05-order-lifecycle-timeline-visibility/05-CONTEXT.md` — Timeline events pattern
- `.planning/phases/01-identity-security-foundation/01-CONTEXT.md` — Role-based access (Admin role)

### Existing Code Anchors
- `src/Domain/Checkout/OrderItemSnapshot.cs` — existing item snapshot model
- `src/Domain/Checkout/Order.cs` — existing order with items
- `src/API/Inventory/InventoryEndpoints.cs` — existing stock adjustment
- `src/API/Checkout/AdminOrderEndpoints.cs` — existing admin order management

</canonical_refs>

  [@code_context]
## Existing Code Insights

### Reusable Assets
- `OrderItemSnapshot` already captures product reference and delivery info
- `DeliveryStatus` enum (Pending, Completed, Failed) extends to custom orders
- Role-based authorization (User, Admin) from Phase 1
- Timeline events pattern from Phase 5 for status history

### Established Patterns
- Order lifecycle state machine for status transitions
- Admin-only policies for CRUD operations
- RFC7807 error responses
- Fulfillment routing from Phase 6/8

### Integration Points
- Add custom order status to existing delivery status model
- Extend admin order endpoints for status updates
- Add audit logging middleware/decorator for critical actions
- Add webhook log inspection endpoint

</code_context>

<specifics>
## Specific Ideas

- Keep it simple: custom requests are just a special product type at checkout
- Free downloads are policy-controlled, not $0 products
- Admin extends what already exists, not new patterns

</specifics>

<deferred>
## Deferred Ideas

- Custom request file upload by customer during request — v1 is description text only
- Automated file generation for custom scripts — manual fulfillment for v1
- Full download analytics — can add later

</deferred>

---

*Phase: 09-custom-orders-marketplace-assets-admin-ops*
*Context gathered: 2026-04-18*