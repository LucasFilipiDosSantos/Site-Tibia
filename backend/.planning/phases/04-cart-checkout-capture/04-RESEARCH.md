# Phase 04 Research — Cart & Checkout Capture

**Phase:** 04 — Cart & Checkout Capture  
**Date:** 2026-04-17  
**Requirements in scope:** CHK-01, CHK-02, CHK-03

## Standard Stack

- ASP.NET Core 10 Minimal APIs for cart/checkout endpoints and route groups
- Application-layer orchestration services (same pattern as `CatalogService` and `InventoryService`)
- EF Core 10 + Npgsql for cart state and checkout/order persistence
- Existing inventory reservation contracts (`Application.Inventory.Contracts`) for checkout-time reserve/release handshake
- Existing global RFC7807 exception mapping pipeline (`GlobalExceptionHandler`) for deterministic 409 semantics
- xUnit unit tests for domain/application invariants + integration tests for transport contracts

## Architecture Patterns

- Keep Clean Architecture boundaries strict:
  - `Domain`: cart, order, immutable snapshot, delivery instruction invariants
  - `Application`: cart and checkout services/contracts/repository ports
  - `Infrastructure`: EF mappings, repositories, migrations
  - `API`: DTOs + endpoint mapping only
- Preserve Phase 3 reservation timing by reserving only during checkout submit (not add-to-cart).
- Keep checkout atomic across all cart lines: reserve all lines and create order in one transaction boundary.
- Keep historical reads snapshot-first (no live catalog join for immutable order values).

## Locked Decision Translation (Context Fidelity)

- D-01..D-04: cart line merge on re-add, absolute quantity-set updates, stock conflict as 409, explicit remove endpoint.
- D-05..D-08: typed delivery-instruction payloads, conditional requirements by fulfillment path, automation payload requires character/server/channel, manual payload requires request brief + contact handle.
- D-09..D-12: checkout creates immutable order-item snapshots (`unitPrice`, `currency`, `productName`, `productSlug`, `categorySlug`); money persisted as decimal + currency; read models return stored snapshot values only.
- D-13..D-16: reserve only on checkout submit, checkout fully atomic, conflict returns per-line available quantity details, successful checkout clears cart.

## Don’t Hand-Roll

- Do not reserve stock at cart-add time (violates D-13).
- Do not represent delivery instructions as untyped/free-form JSON-only blobs without fulfillment-type validation (violates D-05/D-06).
- Do not mutate order snapshot fields after creation (violates D-11).
- Do not partially create checkout orders when one line fails reserve (violates D-14).
- Do not return generic 400 for stock conflicts; preserve 409 with per-line available quantity detail (D-15).

## Common Pitfalls

1. **Duplicate cart lines for same product** after repeated add requests.  
   Mitigation: upsert/merge semantics keyed by product id (D-01).
2. **Quantity drift between client and server** when updates are delta-based.  
   Mitigation: absolute-set semantics for update endpoint (D-02).
3. **Mixed fulfillment payload validation gaps** (manual fields accepted for automated products and vice versa).  
   Mitigation: fulfillment-type discriminator + branch validation (D-06..D-08).
4. **Snapshot corruption** from later catalog edits changing historical order data.  
   Mitigation: write immutable snapshot fields and always project from order-item snapshot (D-09..D-12).
5. **Half-failed checkout** that reserves some lines and writes partial order.  
   Mitigation: single transaction for reserve-all + order create + cart clear (D-14/D-16).

## Validation Architecture

- Unit tests:
  - Cart merge, absolute-set quantity, explicit remove behavior (D-01, D-02, D-04)
  - Checkout snapshot immutability and money shape (D-09, D-10, D-11)
  - Delivery-instruction conditional validation by fulfillment type (D-05..D-08)
  - Checkout orchestration rollback/atomicity contract on reservation conflicts (D-14, D-15)
- Integration tests:
  - Authenticated cart CRUD contracts + 409 stock conflict semantics (CHK-01, D-03)
  - Checkout endpoint creates order snapshots and clears cart on success (CHK-02, D-16)
  - Checkout conflicts return RFC7807 with per-line available quantity details (D-15)
  - Historic order read path returns stored snapshot fields (D-12)
- Schema verification:
  - EF migration for cart/order/snapshot/instruction tables
  - Blocking schema push (`dotnet ef database update ...`) before final verification

## Research Outcome

No new external dependency is required. Phase 04 should be planned as four focused plans: (1) cart contracts + service invariants, (2) checkout snapshot + delivery-instruction orchestration, (3) persistence + migration/schema push, and (4) API contracts + integration verification.
