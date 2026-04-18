---
phase: 07-async-processing-notifications-monitoring
plan: 03
status: complete
completed: 2026-04-18
---

## What was built

Notification jobs with manual trigger endpoints:
- OrderNotificationJob with AutomaticRetry (5 attempts: 1min, 5min, 15min, 1hr, 24hr)
- PaymentNotificationJob for payment events
- Admin-only trigger endpoints:
  - POST /jobs/notifications/trigger
  - POST /jobs/notifications/retry/{orderId}

## Files created/modified

| File | Change |
|------|--------|
| src/Infrastructure/Notifications/NotificationJobs.cs | Created |
| src/API/Jobs/NotificationJobEndpoints.cs | Created |
| src/Application/Checkout/Services/OrderLifecycleService.cs | Modified |
| src/Application/Application.csproj | Modified |
| src/API/Program.cs | Modified |

## Key decisions

- Manual trigger pattern only (no automatic lifecycle wiring)
- Phone number not on Order entity - requires manual pass
- Admin authorization required for endpoints
- Exponential backoff: 60s → 300s → 900s → 3600s → 86400s

## Known issues

- LifecycleService doesn't automatically enqueue - Order missing CustomerPhone field. Manual triggers available for operators.