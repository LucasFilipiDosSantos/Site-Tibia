# Pitfalls Research

**Domain:** Tibia virtual goods commerce backend (Aurera/Eternia)
**Researched:** 2026-04-14
**Confidence:** MEDIUM

## Critical Pitfalls

### Pitfall 1: Non-idempotent payment webhook handling (double-fulfillment)

**What goes wrong:**
Mercado Pago notification retries or duplicated events are processed as new payments, creating duplicate order transitions and duplicate deliveries (e.g., same gold/package sent twice).

**Why it happens:**
Teams model webhook consumption as “fire-and-forget” and skip a durable dedup key + state transition guard.

**How to avoid:**
- Store every inbound notification with provider event ID/external reference and processing status.
- Enforce unique constraint for dedup identity.
- Make order state transitions explicit and monotonic (`Pending -> Paid -> Delivering -> Delivered`), rejecting illegal transitions.
- Move fulfillment trigger to an outbox/queue worker, not synchronous in webhook HTTP handler.

**Warning signs:**
- Same order has multiple successful payment logs within minutes.
- Support reports “customer received twice.”
- Retries in logs produce side effects instead of “already processed.”

**Phase to address:**
Phase: Payments + order state machine foundation (before any automated delivery).

---

### Pitfall 2: No compensating design for chargebacks/refunds after digital delivery

**What goes wrong:**
Order is fully delivered, then payment reverses (chargeback/refund), causing direct revenue loss and operator confusion.

**Why it happens:**
Digital goods projects often treat “payment approved” as terminal success, without post-delivery dispute lifecycle.

**How to avoid:**
- Add post-delivery payment states (`DeliveredAtRisk`, `Disputed`, `Reversed`, `Recovered`).
- Keep immutable evidence bundle per order (payment timeline, delivery proof, operator actions).
- Route risky products (high-value gold/character) to manual review thresholds.
- Add hold windows for products where immediate irreversible delivery is unsafe.

**Warning signs:**
- Negative gross margin despite high “successful deliveries.”
- Refund handling happens in spreadsheets/WhatsApp chats only.
- No query can answer “which delivered orders are disputed?”

**Phase to address:**
Phase: Risk controls + payment reconciliation (immediately after core payment integration).

---

### Pitfall 3: Policy/TOS mismatch in catalog (selling sanction-prone offerings)

**What goes wrong:**
Store lists offerings that are likely to trigger account sanctions (especially automation-adjacent offers such as macros/scripts) or create high enforcement risk.

**Why it happens:**
Product ingestion is treated as pure commerce data entry, without policy gates tied to Tibia rule boundaries.

**How to avoid:**
- Add catalog policy flags (`allowed`, `restricted`, `manual-only`, `blocked`).
- Require compliance review before publishing categories with elevated risk.
- Keep policy version + reviewer in audit log for each listing.
- Prefer service categories with clear operational workflow and lower sanction exposure.

**Warning signs:**
- Frequent “is this safe?” support tickets.
- Products published without reviewer/audit metadata.
- Sudden rise in post-delivery complaints tied to punishments.

**Phase to address:**
Phase: Catalog governance + compliance policy before broad catalog launch.

---

### Pitfall 4: Character/world identity mismatches during fulfillment

**What goes wrong:**
Delivery is executed to wrong character, wrong world, or wrong vocation context, especially with manual flows and similarly named characters.

**Why it happens:**
Order capture lacks normalized in-game identity fields and verified handoff checklists for operators.

**How to avoid:**
- Treat in-game identity as structured data: `character_name`, `world`, optional `guild`, `delivery_window`.
- Add pre-fulfillment verification checklist requiring two-field match at minimum (character + world).
- Require operator confirmation step with immutable delivery receipt payload.
- For high-value items, enforce dual-control approval.

**Warning signs:**
- Rising “wrong character/world” support cases.
- Manual corrections are frequent and undocumented.
- Operators rely on free-text notes instead of validated fields.

**Phase to address:**
Phase: Order schema + fulfillment ops tooling (before scaling manual delivery volume).

---

### Pitfall 5: Overselling hybrid inventory (fungible + unique goods in one model)

**What goes wrong:**
Gold/items (fungible) and characters/services (unique or capacity-constrained) share simplistic stock logic, causing oversell or dead stock.

**Why it happens:**
Single “quantity” abstraction is applied to fundamentally different inventory semantics.

**How to avoid:**
- Split inventory types: `FUNGIBLE`, `UNIQUE`, `SLOT_BASED`.
- Use reservation records with TTL for checkout.
- Lock/serialize stock mutation paths for scarce inventory.
- Reconciliation job detects drift between reserved, committed, and available stock.

**Warning signs:**
- Orders paid but blocked by “unexpectedly unavailable” stock.
- Negative available quantities.
- Same unique asset linked to multiple orders.

**Phase to address:**
Phase: Inventory domain modeling + reservation engine.

---

### Pitfall 6: Weak concurrency control on order/stock updates

**What goes wrong:**
Concurrent admin/customer actions overwrite each other (lost updates), causing incorrect status, inventory, or duplicate processing.

**Why it happens:**
Read-committed defaults are used without row versioning/optimistic concurrency handling in EF Core.

**How to avoid:**
- Add concurrency tokens (`row version`/equivalent) on mutable aggregates.
- Handle `DbUpdateConcurrencyException` with retry or merge strategy.
- Use transaction boundaries around reservation + order creation.
- Use stricter isolation for high-contention workflows where needed.

**Warning signs:**
- “Impossible” state jumps in audit timeline.
- Intermittent support-only bugs under load.
- Frequent manual corrections after simultaneous actions.

**Phase to address:**
Phase: Persistence reliability hardening (same phase as order/inventory core).

---

### Pitfall 7: Background job design that drops or duplicates fulfillment tasks

**What goes wrong:**
Retries, restarts, or shutdowns lose in-flight work or re-execute tasks without idempotency, breaking delivery reliability.

**Why it happens:**
Hosted services are used without durable queue semantics, bounded backpressure, and graceful shutdown behavior.

**How to avoid:**
- Persist work items (DB queue/outbox) before acknowledging webhook.
- Use bounded worker concurrency and retry policy with poison/dead-letter handling.
- Implement graceful stop with checkpointing and resume.
- Emit correlation IDs across webhook -> order -> job -> delivery notifications.

**Warning signs:**
- Orders stuck in `Delivering` after deploy/restart.
- Sudden spike in duplicate notification messages.
- No deterministic replay path for failed jobs.

**Phase to address:**
Phase: Background processing and observability backbone.

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Fulfill directly inside webhook request | Fast MVP path | Timeouts, duplicates, no replay | Never for production |
| Single `quantity` model for all products | Less modeling upfront | Oversell/undersell of unique assets | Only throwaway prototype |
| Free-text delivery instructions only | Quick operator flow | High misdelivery rate, no validation | Never |
| Manual “paid” toggles in admin | Operational flexibility | Audit gaps and fraud surface | Emergency-only with mandatory audit log |

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| Mercado Pago webhooks | Trusting payload blindly and processing every retry as new | Validate authenticity, deduplicate by event identity, process asynchronously |
| WhatsApp notifications | Treating message send as guaranteed delivery | Track send status + retries, expose delivery status in ops panel |
| Email fallback | Assuming email is immediate/reliable for critical actions | Use as secondary channel; never as sole proof of delivery |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Polling-heavy admin dashboards on live order tables | DB CPU spikes during support peak | Read models/materialized views + indexed status timelines | ~100+ concurrent operator/customer sessions |
| Synchronous third-party calls on hot order path | P95/P99 latency spikes, checkout failures | Queue outbound integrations and decouple confirmation path | Moderate traffic + provider latency incidents |
| No partitioning strategy for logs/audit/payments | Queries degrade month over month | Retention policy + partitioning/index maintenance | Usually after first few million events |

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|------------|
| Admin and customer APIs share weakly separated auth scopes | Privilege escalation and data exposure | Strict RBAC with role-specific policies and endpoint segregation |
| Storing sensitive operational proofs in mutable records | Tampering and dispute loss | Immutable append-only audit events with actor/timestamp/signature hashes |
| No anti-automation controls on checkout/login | Credential stuffing, inventory locking abuse | Rate limits, IP/device heuristics, step-up verification for risky actions |

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Vague order states (“processing”) for long manual deliveries | Ticket volume and trust loss | Explicit state timeline with ETA bands and next expected event |
| Missing world/character validation at checkout | Delivery errors and delays | Validate world + character fields and show confirmation summary before payment |
| Silent failures on notification channels | Customer thinks no action happened | Show notification status and fallback channel used |

## "Looks Done But Isn't" Checklist

- [ ] **Webhook integration:** Often missing replay-safe idempotency — verify duplicate event reprocessing is side-effect free.
- [ ] **Order state machine:** Often missing illegal transition guards — verify invalid transitions are rejected and logged.
- [ ] **Manual delivery flow:** Often missing proof schema — verify immutable delivery evidence is stored per fulfillment.
- [ ] **Inventory:** Often missing reservations with expiry — verify concurrent carts cannot oversell scarce goods.
- [ ] **Ops notifications:** Often missing failure visibility — verify alerting for stuck states and retry exhaustion.

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Duplicate fulfillment | HIGH | Freeze affected SKU/workflow, reconcile payment vs delivery, create compensating ledger entries, add dedup guard before reopening |
| Wrong-character delivery | HIGH | Incident workflow: confirm evidence, attempt negotiated remediation, mark account for stricter verification path |
| Oversold unique asset | MEDIUM/HIGH | Priority queue for replacements/refunds, block SKU, run stock reconciliation and rebuild reservations |
| Lost background jobs after restart | MEDIUM | Rebuild pending queue from durable outbox, replay with idempotency keys, add shutdown/restart runbook |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Non-idempotent webhook handling | Phase: Payments + state machine | Replaying same webhook N times yields exactly one fulfillment |
| Chargeback/refund blind spot | Phase: Reconciliation + risk lifecycle | Delivered orders can be queried by dispute/reversal status |
| Catalog policy mismatch | Phase: Catalog governance | No product can publish without policy flag + reviewer audit |
| Character/world mismatch | Phase: Order capture + fulfillment ops | Delivery cannot start without validated character+world checkpoint |
| Hybrid inventory oversell | Phase: Inventory modeling | Concurrency test proves no double-allocation of unique assets |
| Concurrency/lost updates | Phase: Persistence hardening | Conflict tests trigger safe retry/merge, not silent overwrite |
| Job loss/duplication in background workers | Phase: Async processing backbone | Restart chaos test completes pending jobs exactly once semantics at business layer |

## Sources

- Tibia Rules (official): https://www.tibia.com/support/?subtopic=tibiarules  
  - Used for policy-risk pitfalls around forbidden advertising/RMT language and unofficial software references.
- PostgreSQL transaction isolation (official): https://www.postgresql.org/docs/current/transaction-iso.html
- EF Core concurrency handling (official): https://learn.microsoft.com/en-us/ef/core/saving/concurrency
- ASP.NET Core hosted/background services (official): https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services

### Source confidence notes

- **HIGH:** PostgreSQL, EF Core, ASP.NET Core docs (official, current).
- **MEDIUM:** Tibia rules interpretation for commerce-policy gating (official text available, enforcement interpretation is contextual).
- **LOW/MEDIUM:** Some Mercado Pago webhook specifics could not be directly extracted in this environment due fetch size/format limitations; recommendations here follow widely used webhook reliability patterns and should be validated against your exact Mercado Pago doc version during implementation.

---
*Pitfalls research for: Tibia virtual goods backend*
*Researched: 2026-04-14*
