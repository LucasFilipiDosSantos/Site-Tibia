# Phase 5: Order Lifecycle & Timeline Visibility - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-17
**Phase:** 05-order-lifecycle-timeline-visibility
**Areas discussed:** Lifecycle Rules, Timeline Events, Customer History API, Admin Search & Actions

---

## Lifecycle Rules

| Option | Description | Selected |
|--------|-------------|----------|
| Domain state machine | Enforce allowed transitions in Domain/Application service with explicit transition methods and invariants. | ✓ |
| API-only validation | Validate transitions in endpoint handlers. | |
| DB constraint-heavy | Use persistence constraints/triggers as primary guard. | |

**User's choice:** Domain state machine
**Notes:** Legal transition policy locked for centralized invariant enforcement.

| Option | Description | Selected |
|--------|-------------|----------|
| Pending-only cancel | Cancel allowed only from Pending; Paid terminal in this phase. | ✓ |
| Pending and Paid | Allow cancel after payment too. | |
| Admin-only cancel window | Allow cancel with additional admin/time policy. | |

**User's choice:** Pending-only cancel
**Notes:** Keeps refund semantics out of Phase 5.

| Option | Description | Selected |
|--------|-------------|----------|
| System+Admin split | System/webhook marks Paid; customer/admin cancel while Pending. | ✓ |
| Admin owns all transitions | Only admins can change status. | |
| System-only transitions | No manual correction path in this phase. | |

**User's choice:** System+Admin split
**Notes:** Preserves payment authority boundary for upcoming payment phase.

| Option | Description | Selected |
|--------|-------------|----------|
| Idempotent no-op | Duplicate request to current status succeeds without duplicate event. | ✓ |
| Return 409 conflict | Reject duplicate intent explicitly. | |
| Always append event | Record duplicate requests as separate events. | |

**User's choice:** Idempotent no-op
**Notes:** Retry-safe behavior required; avoid timeline noise.

---

## Timeline Events

| Option | Description | Selected |
|--------|-------------|----------|
| State changes only | Append event only when status changes. | ✓ |
| State + user actions | Include operational actions beyond transitions. | |
| Every order mutation | Include all order-field changes. | |

**User's choice:** State changes only
**Notes:** Timeline remains status-centric for ORD-02.

| Option | Description | Selected |
|--------|-------------|----------|
| Status+timestamp+source | Store fromStatus, toStatus, occurredAtUtc, sourceType. | ✓ |
| Plus actor id always | Require actor identity on every event. | |
| Minimal status+timestamp | Store only statuses and timestamp. | |

**User's choice:** Status+timestamp+source
**Notes:** Balanced traceability without overfitting system-generated transitions.

| Option | Description | Selected |
|--------|-------------|----------|
| Backend UTC clock | Server-generated occurredAtUtc at transition commit. | ✓ |
| Client-provided timestamp | Accept caller-provided time. | |
| Database-generated timestamp | Use DB default/trigger timestamp. | |

**User's choice:** Backend UTC clock
**Notes:** Consistent with existing CreatedAtUtc usage and testability.

| Option | Description | Selected |
|--------|-------------|----------|
| Append-only immutable | No edits/deletes; corrections via new event. | ✓ |
| Admin editable notes | Allow metadata edits. | |
| Soft-delete allowed | Permit hidden/removable events. | |

**User's choice:** Append-only immutable
**Notes:** Audit trust prioritized.

---

## Customer History API

| Option | Description | Selected |
|--------|-------------|----------|
| List + detail timeline | Paged order list plus timeline in order detail. | ✓ |
| List only | Summary list without timeline detail now. | |
| Detail only | Keep only order-by-id endpoint. | |

**User's choice:** List + detail timeline
**Notes:** ORD-03 coverage requires both discovery and traceability.

| Option | Description | Selected |
|--------|-------------|----------|
| Newest by created date | Default sort by CreatedAtUtc desc. | ✓ |
| Newest by last status change | Sort by last transition time. | |
| Explicit sort only | No default ordering. | |

**User's choice:** Newest by created date
**Notes:** Stable ordering behavior for user history.

| Option | Description | Selected |
|--------|-------------|----------|
| Raw enum + display label | Return stable status code and display text. | ✓ |
| Display label only | Human-readable only contract. | |
| Raw enum only | Machine code only contract. | |

**User's choice:** Raw enum + display label
**Notes:** Supports both client logic and UI rendering.

| Option | Description | Selected |
|--------|-------------|----------|
| Offset page/pageSize | Reuse existing pagination pattern. | ✓ |
| Cursor-based | Introduce new cursor pattern. | |
| No pagination | Return full history set. | |

**User's choice:** Offset page/pageSize
**Notes:** Contract consistency with current API list style.

---

## Admin Search & Actions

| Option | Description | Selected |
|--------|-------------|----------|
| Status+customer+date | Search by status, customer identifier, created date range. | ✓ |
| Status only | Minimal filter set. | |
| All possible filters now | Add broad filter scope in this phase. | |

**User's choice:** Status+customer+date
**Notes:** Meets ORD-04 operational baseline without scope creep.

| Option | Description | Selected |
|--------|-------------|----------|
| Explicit transition actions | Operation-specific commands for status changes. | ✓ |
| Generic set-status | Single endpoint with target status. | |
| Read-only admin in phase | No admin transition actions. | |

**User's choice:** Explicit transition actions
**Notes:** Encodes legal transitions directly in API/use-case design.

| Option | Description | Selected |
|--------|-------------|----------|
| 409 ProblemDetails | Conflict with currentStatus/allowedTransitions details. | ✓ |
| 400 validation | Treat illegal transition as bad request. | |
| Silent no-op | Ignore illegal transition attempts. | |

**User's choice:** 409 ProblemDetails
**Notes:** Aligns with existing conflict semantics and operator feedback quality.

| Option | Description | Selected |
|--------|-------------|----------|
| Record actor+reason | Persist admin identity and required reason. | ✓ |
| Actor only | Identity tracked, reason optional. | |
| No extra metadata | Only source type tracked. | |

**User's choice:** Record actor+reason
**Notes:** Incident/audit traceability prioritized.

---

## the agent's Discretion

- Endpoint naming/details for list/detail/admin action routes.
- Internal table/entity split for status-history persistence.
- Display label formatting/localization details.

## Deferred Ideas

None — discussion stayed within phase scope.
