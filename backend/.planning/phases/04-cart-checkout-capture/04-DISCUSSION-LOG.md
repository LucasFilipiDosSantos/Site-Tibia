# Phase 4: Cart & Checkout Capture - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-17T00:00:00Z
**Phase:** 04-cart-checkout-capture
**Areas discussed:** Cart line behavior, Delivery instructions contract, Order snapshot payload, Reservation/checkout handshake

---

## Cart line behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Merge quantities | Keep one cart line per product and increment quantity. | ✓ |
| Separate lines | Keep each add as a distinct line. | |
| Merge by default + note | Merge quantities and keep add history note. | |

**User's choice:** Merge quantities
**Notes:** Keep line model simple and deterministic.

| Option | Description | Selected |
|--------|-------------|----------|
| Set absolute quantity | Client sends desired final quantity. | ✓ |
| Increment/decrement | Client sends delta per action. | |
| Support both | Absolute set plus convenience delta endpoints. | |

**User's choice:** Set absolute quantity
**Notes:** Idempotent updates preferred.

| Option | Description | Selected |
|--------|-------------|----------|
| Reject with conflict | Return 409 with available quantity details. | ✓ |
| Clamp to available | Auto-reduce requested quantity. | |
| Accept and defer failure | Allow cart and fail later at checkout. | |

**User's choice:** Reject with conflict
**Notes:** Keep consistent with inventory conflict semantics.

| Option | Description | Selected |
|--------|-------------|----------|
| Explicit remove endpoint | Dedicated delete action for line removal. | ✓ |
| Quantity zero means remove | Set quantity to zero to remove. | |
| Both remove + zero | Support both conventions. | |

**User's choice:** Explicit remove endpoint
**Notes:** Clear intent and API behavior.

---

## Delivery instructions contract

| Option | Description | Selected |
|--------|-------------|----------|
| Structured by fulfillment type | Structured fields for automated/manual paths. | ✓ |
| Single free-text field | One text blob for all products. | |
| Hybrid | Structured fields plus free-text note. | |

**User's choice:** Structured by fulfillment type
**Notes:** Stronger validation and routing clarity.

| Option | Description | Selected |
|--------|-------------|----------|
| Conditionally required | Required fields depend on fulfillment path. | ✓ |
| Always required | Require for all lines regardless of path. | |
| Always optional | Allow missing instructions everywhere. | |

**User's choice:** Conditionally required
**Notes:** Enforce only where needed.

| Option | Description | Selected |
|--------|-------------|----------|
| Character + server + channel | Require deterministic routing fields plus contact channel. | ✓ |
| Character + server | Require minimal deterministic fields only. | |
| Product-specific schema | Define unique required fields per product type. | |

**User's choice:** Character + server + channel
**Notes:** Include channel/contact in automated path input.

| Option | Description | Selected |
|--------|-------------|----------|
| Free-text brief + contact handle | Require brief request details and contact handle. | ✓ |
| Free-text brief only | Require details only. | |
| Detailed template | Require many structured manual-service fields. | |

**User's choice:** Free-text brief + contact handle
**Notes:** Keep manual input concise but actionable.

---

## Order snapshot payload

| Option | Description | Selected |
|--------|-------------|----------|
| Unit price only | Freeze only price. | |
| Price + display basics | Freeze price, currency, product display identity fields. | ✓ |
| Full product copy | Freeze broad product metadata. | |

**User's choice:** Price + display basics
**Notes:** Preserve both pricing and human-readable item identity.

| Option | Description | Selected |
|--------|-------------|----------|
| Decimal + currency code | Persist decimal amount and explicit currency code. | ✓ |
| Decimal only | Persist amount without currency field. | |
| Integer cents + currency | Persist minor units integer plus currency code. | |

**User's choice:** Decimal + currency code
**Notes:** Explicit currency is required in snapshot.

| Option | Description | Selected |
|--------|-------------|----------|
| Fully immutable | Snapshots are write-once after checkout. | ✓ |
| Admin editable | Allow edits to snapshot fields later. | |
| Partially mutable | Lock price but allow some display edits. | |

**User's choice:** Fully immutable
**Notes:** Corrections should use compensating records, not mutation.

| Option | Description | Selected |
|--------|-------------|----------|
| Always snapshot values | Historic orders show stored snapshot values only. | ✓ |
| Live catalog values | Historic orders read current catalog data. | |
| Both snapshot and current | Return both representations. | |

**User's choice:** Always snapshot values
**Notes:** Historic reads must stay stable over catalog changes.

---

## Reservation/checkout handshake

| Option | Description | Selected |
|--------|-------------|----------|
| Keep unchanged | Reserve stock only at checkout submit. | ✓ |
| Reserve during cart updates | Add early reservation in cart flow. | |
| Hybrid pre-hold | Add soft holds before checkout reservation. | |

**User's choice:** Keep unchanged
**Notes:** Carries forward locked Phase 3 timing.

| Option | Description | Selected |
|--------|-------------|----------|
| Fail whole checkout | Any reservation conflict fails whole checkout. | ✓ |
| Create partial order | Keep only successful lines. | |
| Create pending with failures | Create order and mark failed lines. | |

**User's choice:** Fail whole checkout
**Notes:** Atomic checkout behavior.

| Option | Description | Selected |
|--------|-------------|----------|
| Per-line available quantity | Return conflict details for each failing line. | ✓ |
| Generic out-of-stock message | Return generic error text. | |
| Single first-conflict detail | Return only first conflict details. | |

**User's choice:** Per-line available quantity
**Notes:** Client needs actionable line-level correction info.

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, clear on success | Empty cart after successful checkout. | ✓ |
| Keep cart until payment confirmed | Preserve cart during pending payment. | |
| Keep but mark checked-out | Retain consumed lines with marker. | |

**User's choice:** Yes, clear on success
**Notes:** Prevent duplicate submissions from stale cart state.

---

## the agent's Discretion

- Exact DTO naming and endpoint URI shape.
- Exact persistence model for cart/order write path.
- Exact ProblemDetails extension key naming.

## Deferred Ideas

None.
