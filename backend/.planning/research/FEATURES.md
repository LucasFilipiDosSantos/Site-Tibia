# Feature Research

**Domain:** Tibia virtual-goods webstore backend (single-store, Aurera/Eternia focused)
**Researched:** 2026-04-14
**Confidence:** MEDIUM

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist. Missing these = product feels incomplete.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Server/world-specific catalog and pricing | Tibia buyers must buy for the correct world; competitors expose server selection prominently | MEDIUM | Must enforce `product x server` compatibility (Aurera/Eternia now, extensible later) |
| Product type coverage (gold, Tibia Coins, items, characters/services request flow) | Buyers expect one store to handle common Tibia purchase intents, not only one SKU type | HIGH | Needs per-type fulfillment schema (instant-ish digital vs manual workflows) |
| Checkout + payment confirmation via webhook | “Paid = processing” is baseline in game-goods stores | HIGH | Mercado Pago webhook idempotency and signature validation are mandatory for reliability |
| Order status tracking (Pending → Paid → In Delivery → Delivered/Failed) | Competitors emphasize “fast delivery” and order visibility/disputeability | MEDIUM | Customer and admin views must share the same order timeline model |
| Delivery instructions capture at purchase time | Virtual delivery depends on character name/server/availability | MEDIUM | Capture and validate character/server fields + optional delivery window |
| Stock visibility and reservation | Buyers expect immediate “in stock/out of stock” signal | HIGH | Reserve stock on order creation, release on expiration/failure |
| Notification pipeline (WhatsApp first, optional email) | Fast confirmations and delivery updates are expected in this category | MEDIUM | Event-driven templates: payment approved, delivery started, completed, failed |
| Refund/dispute handling workflow | Trust is core in grey/fragile virtual-goods commerce; buyers expect recourse | HIGH | Minimum: dispute states, operator notes, evidence trail, refund outcome logging |

### Differentiators (Competitive Advantage)

Features that set the product apart. Not required, but valuable.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| SLA-driven fulfillment orchestration | Converts “fast delivery” marketing into enforceable ops (timers, escalation, retries) | HIGH | Auto-escalate stuck orders; move from reactive support to proactive operations |
| Unified mixed-fulfillment engine | Handles both automated assets and manual trades in one consistent flow | HIGH | Strong fit for Tibia where character/gold handling differs by product and risk |
| Proactive exception notifications to admins | Reduces missed webhooks, stalled deliveries, and silent failures | MEDIUM | Trigger alerts on timeout, payment mismatch, and delivery retry exhaustion |
| Repeat-order shortcuts (customer profile defaults) | Speeds repeat purchases; repeat buyers are common in currency stores | MEDIUM | Save preferred server/character safely; reduce checkout friction |
| Trust transparency panel (per-order audit timeline) | Increases buyer confidence without building full third-party escrow | MEDIUM | Show payment event, fulfillment start, operator action, and completion proof |

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem good but create problems.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Multi-game marketplace in v1 | “More TAM fast” argument | Explodes catalog/fulfillment complexity before Tibia flow is stable | Stay Tibia-first; add games only after stable unit economics and support load |
| C2C seller marketplace + escrow at launch | Looks like big platforms (G2G/Eldorado) | Requires moderation, KYC/risk, dispute ops, and legal overhead | Start as single-operator store with strong internal audit trail |
| Real-time in-app support chat system | Perceived as modern UX | Duplicates support channel; high operational overhead | Use WhatsApp integration + structured order-status events |
| “Instant delivery for everything” promise | Marketing pressure | Some Tibia goods are inherently manual/risky; false SLA harms trust | Expose per-product realistic SLA windows and escalation paths |
| Full anti-fraud ML in v1 | Feels enterprise-grade | High data + tuning cost; weak ROI at early scale | Begin with rule-based controls + manual review queues |

## Feature Dependencies

```
[Server/world catalog]
    └──requires──> [Product type modeling]
                        └──requires──> [Delivery instructions schema]

[Checkout creation]
    └──requires──> [Payment webhook confirmation]
                        └──requires──> [Order state machine]
                                              └──requires──> [Notification pipeline]

[Stock reservation]
    └──requires──> [Order state machine]

[Dispute/refund workflow]
    └──requires──> [Order timeline + audit logs]

[SLA-driven orchestration] ──enhances──> [Order state machine]
[Trust transparency panel] ──enhances──> [Dispute/refund workflow]

[C2C marketplace] ──conflicts──> [Lean single-store v1 scope]
```

### Dependency Notes

- **Server/world catalog requires product type modeling:** gold/items/coins/services each need different validation and fulfillment rules.
- **Checkout requires webhook confirmation:** payment status cannot depend on frontend redirect only.
- **Webhook confirmation requires order state machine:** idempotent transitions are needed to prevent duplicate fulfillment.
- **Dispute workflow requires audit timeline:** without immutable event history, disputes become subjective and expensive.
- **C2C marketplace conflicts with lean v1:** adds a second business model and doubles operational/legal complexity.

## MVP Definition

### Launch With (v1)

Minimum viable product — what's needed to validate the concept.

- [ ] Server-scoped Tibia catalog + inventory availability — core buying intent is server-bound.
- [ ] Checkout + Mercado Pago webhook-confirmed order lifecycle — required for payment-driven automation.
- [ ] Manual+automated delivery orchestration with customer/admin status tracking — core value is reliable, trackable delivery.
- [ ] WhatsApp notification events (payment approved, in delivery, delivered, failed) — critical communication loop.
- [ ] Basic dispute/refund workflow + audit log — trust baseline for virtual-goods transactions.

### Add After Validation (v1.x)

Features to add once core is working.

- [ ] SLA timer automation + escalation queues — add when manual operations start missing deadlines.
- [ ] Repeat-order shortcuts and saved delivery preferences — add when repeat-customer cohort is visible.
- [ ] Coupon and campaign rules engine — add after fulfillment reliability is stable.

### Future Consideration (v2+)

Features to defer until product-market fit is established.

- [ ] Multi-game expansion — defer until Tibia operations are consistently profitable and supportable.
- [ ] Seller-side marketplace/escrow model — defer until legal, moderation, and risk ops are funded.
- [ ] Advanced fraud scoring/ML — defer until sufficient transaction volume and labeled fraud outcomes exist.

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Webhook-confirmed payment + order state machine | HIGH | HIGH | P1 |
| Server-scoped catalog + inventory reservation | HIGH | MEDIUM/HIGH | P1 |
| Delivery instructions + fulfillment tracking | HIGH | MEDIUM | P1 |
| WhatsApp notifications | HIGH | MEDIUM | P1 |
| Dispute/refund + audit timeline | HIGH | HIGH | P1 |
| SLA-driven orchestration | MEDIUM/HIGH | HIGH | P2 |
| Repeat-order shortcuts | MEDIUM | MEDIUM | P2 |
| Coupon engine | MEDIUM | MEDIUM | P2 |
| Marketplace/escrow model | MEDIUM | VERY HIGH | P3 |

**Priority key:**
- P1: Must have for launch
- P2: Should have, add when possible
- P3: Nice to have, future consideration

## Competitor Feature Analysis

| Feature | Competitor A (Eldorado) | Competitor B (MMOPixel/MMOShops) | Our Approach |
|---------|--------------------------|-----------------------------------|--------------|
| Server/world targeting | Explicit region/server filtering in offer selection | Explicit world selection lists on product pages | First-class server scoping in product and order models |
| Delivery promise | Emphasizes guaranteed/average delivery time + order chat | Emphasizes “24-hour guarantee” and fast delivery | SLA-aware states and escalation instead of marketing-only claims |
| Buyer trust mechanism | TradeShield with dispute/refund process | Refund policy and customer service messaging | Native dispute states + immutable order timeline |
| Order communication | Per-order buyer/seller chat flow | Support-led communication model | WhatsApp-integrated notification + operator workflow |

## Sources

- Project context and scope constraints: `.planning/PROJECT.md` (HIGH)
- Eldorado Tibia listing page (delivery-time, support, trust positioning): https://www.eldorado.gg/tibia-gold/g/371 (MEDIUM)
- Eldorado TradeShield help article (dispute/refund expectations): https://support.eldorado.gg/en/articles/8408994-tradeshield-buying (MEDIUM)
- MMOPixel Tibia Gold page (feature/expectation signals: fast delivery, refund, payment options): https://www.mmopixel.com/tibia-gold (MEDIUM)
- MMOShops Tibia Gold page (server selection, cart/checkout baseline): https://mmoshops.com/Tibia-Gold%20-1 (MEDIUM)
- Tibia official site (world/community/account/char-bazaar ecosystem signals): https://www.tibia.com/community/?subtopic=characters and https://www.tibia.com/mmorpg/free-multiplayer-online-role-playing-game.php (HIGH)
- Mercado Pago docs pages were partially inaccessible via fetch (heavy JS/size); payment-webhook recommendations above are inferred from integration requirements + standard payment integration patterns (LOW/MEDIUM, needs in-phase validation against live docs)

---
*Feature research for: Tibia virtual goods webstore backend*
*Researched: 2026-04-14*
