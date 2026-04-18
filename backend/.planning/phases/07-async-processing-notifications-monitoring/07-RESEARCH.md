# Phase 7 Research: Async Processing, Notifications & Monitoring

**Phase:** 07-async-processing-notifications-monitoring
**Researched:** 2026-04-18
**Status:** Complete

---

## Domain Overview

This phase delivers:
- Background job infrastructure (Hangfire + PostgreSQL storage)
- WhatsApp notifications via Meta Cloud API
- Retry logic with exponential backoff
- Job monitoring via dashboard and health checks

---

## Research Findings

### 1. Hangfire with PostgreSQL Storage

**Architecture Pattern:**
- Hangfire.PostgreSql 1.21.x provides durable job storage using PostgreSQL
- Jobs are persisted in `hangfire` schema tables (`job`, `jobparameter`, `state`, `set`, `hash`, `list`, `counter`)
- Storage supports distributed locking for multi-server deployments

**Durable Job Execution:**
- Fire-and-forget jobs: Enqueued immediately, processed by available workers
- Delayed jobs: Scheduled for future execution using `BackgroundJob.Schedule()`
- Recurring jobs: Cron-based scheduling via `RecurringJob.AddOrUpdate()`

**Retry Policies (Exponential Backoff):**
```csharp
// Global automatic retry filter (default: 10 attempts)
config.UseFilter(new AutomaticRetryAttribute 
{ 
    Attempts = 5,
    DelaysInSeconds = new[] { 60, 300, 900, 3600, 86400 } // 1min, 5min, 15min, 1hr, 24hr
});

// Per-job custom retry
[AutomaticRetry(Attempts = 5, DelaysInSeconds = new[] { 60, 300, 900, 3600, 86400 })]
public class WhatsAppNotificationJob
{
    public async Task ExecuteAsync(NotificationDto dto, CancellationToken ct) { }
}
```

**Startup Resilience (v1.20.13+):**
- `PostgreSqlStorageOptions` has built-in startup resilience
- Default: 5 retries with exponential backoff (1s → 2s → 4s → 8s → 16s, max 1 min)
- `AllowDegradedModeWithoutStorage = true` allows app to start even if DB unavailable

**Idempotency Strategy:**
- Use job parameters to track deduplication keys
- Check for processed events before sending notifications:
  ```csharp
  public async Task ExecuteAsync(string notificationKey, CancellationToken ct)
  {
      // Check if already processed
      if (await _dedupRepository.ExistsAsync(notificationKey, ct))
          return;
      
      // Process and mark as done
      await _whatsAppService.SendAsync(...);
      await _dedupRepository.AddAsync(notificationKey, ct);
  }
  ```

---

### 2. Meta Cloud API for WhatsApp

**API Endpoint:**
```
POST https://graph.facebook.com/v25.0/{PHONE_NUMBER_ID}/messages
```

**Authentication:**
- Bearer token (long-lived access token from Meta Developer Portal)
- Token never expires for Cloud API (unless explicitly revoked)

**No Official SDK:**
- Meta provides Cloud API (REST) but no official C# SDK
- Community wrapper available: `WhatsappBusiness.CloudApi` (NuGet, ~439 stars)
- Alternative: Direct HTTP calls via `HttpClient`

**Message Types:**

| Type | Use Case | Template Required |
|------|----------|-------------------|
| Template | Initial contact, notifications | Yes (pre-approved by Meta) |
| Text | Direct replies within 24h window | No |

**Template Messages (Required for Notifications):**
```json
{
  "messaging_product": "whatsapp",
  "to": "+5511999999999",
  "type": "template",
  "template": {
    "name": "order_confirmed",
    "language": { "code": "en_US" },
    "components": [
      {
        "type": "body",
        "parameters": [
          { "type": "text", "parameter_name": "order_id", "value": "12345" }
        ]
      }
    ]
  }
}
```

**Webhook Handling:**
```
GET /webhooks/whatsapp → Verification challenge (hub.verify)
POST /webhooks/whatsapp → Message status updates
```

Payload includes:
- `messages[].status` → sent, delivered, read, failed
- `messages[].id` → WhatsApp message ID for tracking

**Rate Limits:**
- Default: 80 messages/second per phone number
- Can request capacity upgrades via Meta Business Manager

**Pre-requisites:**
1. Meta Business Account (verified)
2. WhatsApp Business Account (WABA)
3. Phone number verified
4. Message templates pre-approved by Meta

---

### 3. Background Job Monitoring

**Hangfire Dashboard:**
- Built-in dashboard at `/hangfire` (or custom route)
- Shows: enqueued, processing, scheduled, failed, succeeded jobs
- Metrics: server count, queue depth, retry count
- Auth: protect with authorization filter in production

**Health Checks (AspNetCore.Diagnostics.HealthChecks):**
```csharp
builder.Services.AddHealthChecks()
    .AddHangfire(options => 
    {
        options.MaximumJobsFailed = 10;
        options.MinimumAvailableServers = 1;
    });
```

**OpenTelemetry Instrumentation:**
- Track: job execution time, success/failure rates, retry counts
- Create custom metrics:
  - `hangfire.jobs.executed_total`
  - `hangfire.jobs.failed_total`
  - `hangfire.jobs.duration_p95`

**Alerting Patterns:**
- Failed jobs exceeding threshold → alert to operators
- Queue depth growing unbounded → capacity warning
- Jobs consistently exhausting retries → investigate upstream

---

## Implementation Recommendations

### Stack Selection

| Component | Recommendation | Version |
|-----------|----------------|---------|
| Background Jobs | Hangfire + PostgreSQL storage | 1.8.x core, 1.21.x Postgres |
| WhatsApp API | Direct HTTP via HttpClient (no wrapper) | - |
| Monitoring | Hangfire dashboard + HealthChecks | Latest stable |
| Structured Logs | Serilog (already in stack) | 10.x |

### Phase Breakdown

**Plan 1: Hangfire Infrastructure Setup**
- Install: Hangfire.Core, Hangfire.PostgreSql
- Configure: PostgreSqlStorageOptions with resilience defaults
- Add: Dashboard endpoint with auth
- Add: Health check integration
- Migrate: Create hangfire schema

**Plan 2: WhatsApp Notification Service**
- Create: IWhatsAppNotificationService + implementation
- Implement: Template message sending via Graph API
- Add: Deduplication for idempotent sends
- Configure: Meta credentials in configuration

**Plan 3: Event-to-Notification Wiring**
- Create: Notification job classes for order/payment/delivery events
- Wire: Lifecycle service → background job enqueue
- Implement: Exponential backoff retry (1min, 5min, 15min, 1hr, 24hr)
- Add: Structured logs for critical flow visibility

---

## Risks and Considerations

1. **WhatsApp Template Approval** → Requires business verification; lead time 1-3 days per template
2. **Meta API Rate Limits** → Monitor 80 msg/sec cap; queue if exceeded
3. **Notification Failures** → Exponential backoff prevents cascade; dead-letter after 24hr
4. **Hangfire Dashboard Security** → Must restrict to admin users in production

---

## Validation Architecture

For Nyquist validation coverage:

| Dimension | Validation Approach |
|-----------|---------------------|
| 1. Task correctness | Job executes and completes without exception |
| 2. State transitions | Notification sent → webhook confirms delivery |
| 3. Error handling | Failed sends trigger retry; exhausted retries log to dead letter |
| 4. Concurrency | Multiple concurrent jobs don't duplicate notifications |
| 5. Recovery | App restart re-processes pending jobs from PostgreSQL |
| 6. Observability | Dashboard shows all job states; health check returns OK |
| 7. Performance | Jobs complete within SLA (e.g., <30s for notification send) |
| 8. Security | WhatsApp API tokens stored securely; dashboard auth'd |

---

## References

- Hangfire PostgreSQL: https://github.com/hangfire-postgres/Hangfire.PostgreSql
- Meta Cloud API: https://developers.facebook.com/docs/whatsapp/cloud-api/
- WhatsApp C# Wrapper (optional): https://github.com/gabrieldwight/Whatsapp-Business-Cloud-Api-Net
- HealthChecks.Hangfire: https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks

---

## RESEARCH COMPLETE

The planner can now create detailed implementation plans based on this research.