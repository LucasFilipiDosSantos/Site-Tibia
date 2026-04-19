---
phase: 10-backend-hardening-reliability
plan: 01
status: complete
completed: 2026-04-19
---

## What was built

Automatic WhatsApp notification enqueue on order/delivery lifecycle events with deterministic contact sourcing:
- Domain events: OrderPaidEvent, DeliveryCompletedEvent, DeliveryFailedEvent
- Order notification metadata: NotificationPhone (E.164), NotificationAvailable flag, NotificationFailedReason
- ICustomerRepository for phone fetch at checkout
- INotificationPublisher interface with auto-enqueue on lifecycle transitions
- NotificationOutboxRepository for idempotency checks and retry persistence
- Idempotency key = OrderId+EventType+StatusAtUtc
- Enqueue failures persist to outbox without rollback

## Files created/modified

| File | Change |
|------|--------|
| src/Domain/Checkout/OrderPaidEvent.cs | Created |
| src/Domain/Checkout/DeliveryCompletedEvent.cs | Created |
| src/Domain/Checkout/DeliveryFailedEvent.cs | Created |
| src/Domain/Checkout/Order.cs | Modified - add NotificationPhone/Available/FailedReason |
| src/Application/Checkout/Contracts/ICustomerRepository.cs | Created |
| src/Application/Checkout/Services/CheckoutService.cs | Modified - phone snapshot on order |
| src/Application/Notifications/INotificationPublisher.cs | Created |
| src/Application/Notifications/NotificationOutboxContracts.cs | Created |
| src/Application/Checkout/Services/OrderLifecycleService.cs | Modified - auto-enqueue on Paid |
| src/Application/Checkout/Services/FulfillmentService.cs | Modified - auto-enqueue on completion/fail |
| src/Infrastructure/Notifications/NotificationPublisher.cs | Created |
| src/Infrastructure/Notifications/NotificationOutboxRepository.cs | Created |

## Must-haves verification

| Must-have | Status |
|-----------|--------|
| Order status change to Paid triggers automatic WhatsApp notification enqueue | ✓ |
| Delivery status change to Completed triggers automatic WhatsApp notification enqueue | ✓ |
| Delivery status change to Failed triggers automatic WhatsApp notification enqueue | ✓ |
| Notification uses snapshotted phone from order metadata with E.164 validation | ✓ |
| Enqueue failure does not roll back the business transition | ✓ |
| Enqueue idempotency uses OrderId+EventType+statusAtUtc key | ✓ |

## Key decisions

- D-01: Auto-triggers on Order Paid, Delivery Completed, Delivery Failed
- D-02: Idempotency key = OrderId+EventType+statusAtUtc
- D-03: Auto-enqueue in Application lifecycle services
- D-04: If enqueue fails, persist outbox/retry; don't rollback business transition
- D-07: Phone source is customer profile
- D-08: Phone snapshotted on order at creation time
- D-10: Phone persisted in E.164 format

## Requirements covered

- NTF-01: Automatic WhatsApp notification enqueue on Order Paid, Delivery Completed, Delivery Failed
- NTF-03: Notification failures persist to outbox for retry

## Commit

09117b6

## Self-Check: PASSED