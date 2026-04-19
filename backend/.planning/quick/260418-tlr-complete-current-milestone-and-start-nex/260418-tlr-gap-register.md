# Backend P0/P1 Gap Register (Milestone Carryover)

**Scope lock:** Backend only (API/Application/Domain/Infrastructure, background jobs, operational reliability).  
**Explicitly excluded:** Frontend/UI implementation scope.

## Coverage Matrix

This register consolidates only unresolved backend concerns into P0/P1 with traceability fields required by the plan threat model.

| Priority | Category | Concern Class Covered |
|---|---|---|
| P0 | Bug | Broken/fragile runtime behavior impacting core flow |
| P0 | Security | Trust boundary and transport/account protection risks |
| P0 | Reliability | Background processing and delivery/notification continuity |
| P0 | Test Gap | Missing executable proof for critical safety requirements |
| P1 | Performance | Throughput/latency and operational efficiency risks |
| P1 | Technical Debt | Structural debt increasing defect risk and maintenance cost |
| P1 | Fragile/Scaling | Current design limits that can fail under load/growth |
| P1 | Missing Feature | Required backend capability not yet wired for full v1 operations |

---

## P0

### P0-01 — Notification flow not automatically wired from lifecycle events
- **Priority:** P0
- **Category:** Reliability / Missing Feature
- **Source:** summary (07-03-SUMMARY.md)
- **Affected files/systems:** `src/Application/Checkout/Services/OrderLifecycleService.cs`, `src/Infrastructure/Notifications/NotificationJobs.cs`, Hangfire job enqueue path
- **Impact:** Order/payment/delivery notifications depend on manual operator trigger; event loss risk and inconsistent customer/operator awareness.
- **Concrete next action:** Implement automatic event-to-job enqueue for key lifecycle transitions (order paid, delivery completed/failed) with idempotent enqueue keys and retry-safe job payloads.

### P0-02 — Customer phone not wired in order flow for notification automation
- **Priority:** P0
- **Category:** Bug / Missing Feature
- **Source:** summary (07-03-SUMMARY.md)
- **Affected files/systems:** Checkout/order aggregate and persistence mapping; notification payload composition
- **Impact:** Automated WhatsApp notifications cannot be deterministically dispatched without operator-provided phone input.
- **Concrete next action:** Add canonical customer phone source for order notification context (derived from account profile or validated checkout capture), persist as required notification target metadata, and wire into notification job payload generation.

### P0-03 — HTTPS/HSTS enforcement still pending deployed-environment proof
- **Priority:** P0
- **Category:** Security / Test Gap
- **Source:** uat (01-HUMAN-UAT.md), verification (01-VERIFICATION.md)
- **Affected files/systems:** API hosting pipeline, ingress/proxy deployment configuration
- **Impact:** SEC-01 remains operationally unproven; transport downgrade/misconfiguration risk in non-development environments.
- **Concrete next action:** Add deployment verification runbook + executable smoke probe for HTTP→HTTPS redirect and HSTS presence in staging/prod-like topology; capture and store evidence artifacts.

### P0-04 — ADM-02 audit requirement marked pending despite phase 9 delivery intent
- **Priority:** P0
- **Category:** Reliability / Technical Debt
- **Source:** verification + requirements (REQUIREMENTS.md, roadmap/phase completion inconsistency)
- **Affected files/systems:** Admin critical write paths, audit persistence and retrieval endpoints
- **Impact:** Compliance/forensics gap for critical state mutations; impossible to confidently reconstruct operational incidents.
- **Concrete next action:** Reconcile requirement traceability against shipped code, then close missing audit capture paths (actor/action/entity/before-after) and add integration tests asserting persistence for representative admin mutations.

### P0-05 — REL-01/REL-02 still pending while async stack is partially delivered
- **Priority:** P0
- **Category:** Reliability / Test Gap
- **Source:** requirements (REQUIREMENTS.md), summary (07-01/07-02/07-03)
- **Affected files/systems:** Background jobs, structured logging/monitoring instrumentation, operational health checks
- **Impact:** Core reliability requirements remain unclosed at artifact level; milestone closure can drift from true runtime readiness.
- **Concrete next action:** Define and execute closure criteria for REL-01/REL-02 (automated enqueue/retry verification, structured telemetry assertions, health/degradation scenarios) and update traceability only after proof passes.

---

## P1

### P1-01 — Milestone planning metadata drift across ROADMAP/STATE/REQUIREMENTS
- **Priority:** P1
- **Category:** Technical Debt
- **Source:** verification (cross-artifact review)
- **Affected files/systems:** `.planning/ROADMAP.md`, `.planning/STATE.md`, `.planning/REQUIREMENTS.md`
- **Impact:** Automation routing and execution confidence degrade when completion markers contradict one another.
- **Concrete next action:** Introduce milestone-transition consistency check script (phase-plan counts, requirement status coherence, current position alignment) and run it on every milestone close.

### P1-02 — Notification routing currently operator-centric, not policy-driven per event class
- **Priority:** P1
- **Category:** Fragile/Scaling
- **Source:** summary (07-03-SUMMARY.md)
- **Affected files/systems:** Notification trigger endpoints and job selection logic
- **Impact:** Increased manual toil and inconsistent behavior under volume; high chance of missed or delayed outbound notifications.
- **Concrete next action:** Add server-side routing policy matrix by event type/channel with deterministic defaults and override audit trail.

### P1-03 — Payment/fulfillment/notification observability coverage not validated end-to-end
- **Priority:** P1
- **Category:** Performance / Reliability
- **Source:** requirements (REL-02), verification summaries
- **Affected files/systems:** Serilog/OpenTelemetry pipeline, correlation IDs across payment→order→fulfillment→notification path
- **Impact:** Slower incident diagnosis and reduced ability to detect degraded flow before customer impact.
- **Concrete next action:** Add integration-level telemetry assertions for critical spans/log fields and establish minimum dashboard/alert contract for the core commerce pipeline.

### P1-04 — Security/reliability requirement closure process depends on manual interpretation
- **Priority:** P1
- **Category:** Test Gap / Technical Debt
- **Source:** verification + uat process observations
- **Affected files/systems:** Verification artifacts and requirement status update workflow
- **Impact:** Regressions can pass unnoticed when requirement status is updated without reproducible evidence artifacts.
- **Concrete next action:** Require per-requirement evidence pointer (test name, log artifact, or UAT record) before marking status complete; enforce via checklist in verifier output template.

### P1-05 — Carryover risk for unresolved Phase 7 concerns if not explicitly phase-bound
- **Priority:** P1
- **Category:** Fragile Area
- **Source:** summary (07-03), roadmap sequencing
- **Affected files/systems:** Next-phase scope planning and execution boundary
- **Impact:** Known reliability gaps can be delayed again by competing feature work.
- **Concrete next action:** Lock unresolved P0/P1 carryover as non-negotiable scope in Phase 10 context with explicit “no frontend/no priority downgrade” rule.

---

## Traceability Back to User-Listed Concern Classes

| Concern Class | Covered By |
|---|---|
| technical debt | P0-04, P1-01, P1-04 |
| bugs | P0-02 |
| security | P0-03 |
| performance | P1-03 |
| fragile areas | P1-02, P1-05 |
| scaling limits | P1-02, P1-03 |
| missing features | P0-01, P0-02 |
| test gaps | P0-03, P0-05, P1-04 |

## Notes

- Priority labels (P0/P1) are intentionally preserved and will be copied into Phase 10 scope decisions.
- No v2 analytics/fraud expansion is introduced here; only carryover risks needed for backend v1 correctness/reliability closure are included.
