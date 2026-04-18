# Phase 07: Async Processing, Notifications & Monitoring - Context

**Gathered:** 2026-04-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Background jobs execute payment confirmation, fulfillment automation hooks, notification dispatch, and retry workloads. Key order/payment/delivery events enqueue WhatsApp notifications and optional email notifications. Transient failures retry automatically. Structured logs expose critical flow outcomes for operations.

</domain>

<decisions>
## Implementation Decisions

### Notification Channel
- **D-01:** Use Meta Cloud API directly for WhatsApp notifications — fewer layers, more control than Twilio

### Retry Strategy
- **D-02:** Exponential backoff for transient notification failures — 1min, 5min, 15min, 1hr, 24hr max

### Monitoring & Observability
- **D-03:** Hangfire dashboard + health checks for job monitoring
- **D-04:** Structured Serilog logs for critical flow outcomes

### Job Scheduling
- **D-05:** Fire-and-forget — enqueue immediately on lifecycle event for fastest processing

### Agent Discretion
- Email notification channel implementation can use default patterns
- Exact notification templates can follow existing brand tone
- Health check endpoints can use standard ASP.NET health check patterns

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Requirements
- `.planning/ROADMAP.md` § Phase 7 Success Criteria — NTF-01, NTF-02, NTF-03, REL-01, REL-02
- `.planning/REQUIREMENTS.md` — Notification and reliability requirements

### Integration Patterns
- `src/API/Payments/PaymentWebhookEndpoints.cs` — Existing IBackgroundJobClient pattern to follow for job enqueue
- `.planning/phases/06-mercado-pago-payment-confirmation/06-02-SUMMARY.md` — Phase 6 webhook async pattern

### Security
- `.planning/PROJECT.md` — WhatsApp API requirement from Constraints section

[If no external specs: "No external specs — requirements fully captured in decisions above"]

</canonical_refs>

  [@code_context]
## Existing Code Insights

### Reusable Assets
- `IHangfireJobClient` / `IBackgroundJobClient` — Job enqueue interface from Phase 6 webhook implementation
- Hangfire.PostgreSql storage — configured in Phase 6 infrastructure

### Established Patterns
- Async webhook processing: fast 200 ack + async job dispatch
- Payment event dedup pattern from Phase 6

### Integration Points
- Order lifecycle transition events → notification triggers
- Payment status changes → notification triggers
- Delivery status changes → notification triggers

</code_context>

<specifics>
## Specific Ideas

- Meta Cloud API requires phone number verification for business account
- WhatsApp message templates must be pre-approved by Meta for outbound notifications
- Exponential backoff 24hr max prevents zombie jobs

[If none: "No specific requirements — open to standard approaches"]

</specifics>

<deferred>
## Deferred Ideas

- Email notification implementation — Phase 7 scope focuses on WhatsApp, email can be added in Phase 9 or later
- Custom notification preferences per user — can be added in future phases

[If none: "Discussion stayed within phase scope"]

</deferred>

---

*Phase: 07-async-processing-notifications-monitoring*
*Context gathered: 2026-04-18*