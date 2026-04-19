# Codebase Concerns

**Analysis Date:** 2026-04-18

## Tech Debt

**Download fulfillment pipeline is partially stubbed:**
- Issue: Download URL generation exists, but file delivery remains placeholder behavior with explicit TODO and 501 response.
- Files: `backend/src/API/Downloads/DownloadEndpoints.cs`, `backend/src/Application/Products/Services/DownloadEntitlementService.cs`
- Impact: Purchase flow can generate signed links but cannot deliver purchased artifacts.
- Fix approach: Implement storage-backed file streaming in `DownloadFile` and wire policy repository for `DownloadAccessPolicy` in entitlement checks.

**Frontend remains mock-data driven instead of API-backed:**
- Issue: Product/order/auth/cart services read from local in-memory constants and `localStorage` instead of backend endpoints.
- Files: `FronEnd/src/features/products/services/product.service.ts`, `FronEnd/src/features/orders/services/order.service.ts`, `FronEnd/src/features/auth/services/mock-auth.service.ts`, `FronEnd/src/features/cart/services/cart.service.ts`, `FronEnd/src/data/mockData.ts`
- Impact: UI behavior diverges from backend domain rules, causing integration drift and late discovery of contract mismatches.
- Fix approach: Replace mock services with HTTP adapters and align DTO/state mapping with backend API contracts.

**Payment/checkout integration tests contain placeholder implementations:**
- Issue: Multiple integration tests are present but implemented as `Task.CompletedTask` placeholders.
- Files: `backend/tests/IntegrationTests/Payments/PaymentWebhookEndpointsTests.cs`, `backend/tests/IntegrationTests/Payments/PaymentConfirmationFlowTests.cs`
- Impact: Critical webhook and payment-lifecycle behavior has low regression protection despite test file presence.
- Fix approach: Convert placeholders into executable integration tests with seeded data and end-to-end assertions against real service wiring.

## Known Bugs

**Webhook signature parser/validator contract mismatch:**
- Symptoms: Valid Mercado Pago webhook requests can be rejected as malformed signature.
- Files: `backend/src/API/Payments/PaymentWebhookEndpoints.cs`, `backend/src/Infrastructure/Payments/MercadoPago/MercadoPagoWebhookSignatureValidator.cs`
- Trigger: Endpoint parses `x-signature` header `v1=<hash>` and forwards only `<hash>`, while validator requires input beginning with `v1:`.
- Workaround: None in code; requires normalization in endpoint or validator to accept the same signature format.

**Entitlement check truncates purchase history:**
- Symptoms: Users with qualifying paid orders outside first page can be denied download entitlement.
- Files: `backend/src/Application/Products/Services/DownloadEntitlementService.cs`
- Trigger: `GetCustomerOrdersAsync(userId, 1, 100, ct)` is hard-limited to 100 orders before entitlement scan.
- Workaround: None in code; requires direct entitlement query by `(userId, productId, paidStatus)`.

## Security Considerations

**Insecure fallback secrets and connection defaults are embedded in DI wiring:**
- Risk: Service can boot with predictable JWT signing key and local DB password when configuration is missing.
- Files: `backend/src/Infrastructure/DependencyInjection.cs`
- Current mitigation: `backend/src/API/Program.cs` enforces required JWT config and minimum signing key length for API startup path.
- Recommendations: Remove fallback credentials entirely from `DependencyInjection`; fail startup if secure config is absent in all hosting paths.

**Frontend mock auth persists plaintext credentials in browser storage:**
- Risk: Passwords are written to `localStorage`, exposing credentials to XSS and local device inspection.
- Files: `FronEnd/src/features/auth/utils/auth.storage.ts`, `FronEnd/src/features/auth/services/mock-auth.service.ts`
- Current mitigation: None in code; data is directly serialized and read back.
- Recommendations: Remove password persistence from client storage and move authentication/credential verification fully server-side.

## Performance Bottlenecks

**Webhook processing incurs repeated DB round-trips per event:**
- Problem: Processor loads webhook log, dedupe record, latest status, persists event, and may call confirmation service with additional repository lookups.
- Files: `backend/src/Application/Payments/Services/PaymentWebhookProcessor.cs`, `backend/src/Application/Payments/Services/PaymentConfirmationService.cs`
- Cause: Sequential repository calls without batched query strategy.
- Improvement path: Consolidate read path (single query projection where possible) and ensure indexed lookups on provider payment identifiers.

**Download entitlement uses broad order-list scan:**
- Problem: Entitlement check pulls a page of orders and scans nested line items in memory.
- Files: `backend/src/Application/Products/Services/DownloadEntitlementService.cs`
- Cause: Query shape is order-history retrieval rather than entitlement-specific predicate query.
- Improvement path: Add repository method for direct paid-entitlement existence checks by user/product.

## Fragile Areas

**Client-side navigation during render in auth-protected pages:**
- Files: `FronEnd/src/pages/auth/Login.tsx`, `FronEnd/src/pages/auth/Register.tsx`, `FronEnd/src/pages/shop/Checkout.tsx`, `FronEnd/src/pages/user/Profile.tsx`
- Why fragile: Calling `navigate(...)` during render can cause render-loop warnings and timing-dependent behavior.
- Safe modification: Move redirects into route guards (`ProtectedRoute`/`AdminRoute`) or `useEffect` side effects.
- Test coverage: Frontend tests do not cover route/redirect behavior (`FronEnd/src/test/example.test.ts` only).

**In-memory rate limiter state is process-local and unbounded:**
- Files: `backend/src/API/Auth/AuthRateLimitMiddleware.cs`
- Why fragile: Static dictionary grows per unique identifier/IP and does not coordinate across replicas.
- Safe modification: Replace with distributed store-backed limiter and bounded eviction policy.
- Test coverage: No dedicated integration tests validating limiter behavior under load/distributed deployment.

## Scaling Limits

**Auth throttling capacity tied to single process memory:**
- Current capacity: Limited to one API process (`ConcurrentDictionary` in-memory counters).
- Limit: Horizontal scaling allows bypassing limits across instances; long-lived processes accumulate keys.
- Scaling path: Move counters to distributed cache (e.g., Redis) with TTL and global keying strategy.

**Mock frontend data model does not scale with real order/catalog volume:**
- Current capacity: Static arrays and local filters in memory.
- Limit: No server pagination/filtering, no persistence guarantees, and no multi-user consistency.
- Scaling path: Replace mock data access with backend API pagination and server-side querying.

## Dependencies at Risk

**Not detected:**
- Risk: No direct package deprecation/security risk is evidenced in scanned source files.
- Impact: Not applicable from current source-level concern scan.
- Migration plan: Maintain dependency audit in CI and track advisories as part of release checks.

## Missing Critical Features

**Download storage delivery integration is missing:**
- Problem: Download endpoint returns metadata + 501 instead of file stream.
- Blocks: Fulfillment of digital product delivery through signed URL flow.

**Executable integration coverage for payment webhook lifecycle is missing:**
- Problem: Test suites exist but core payment-webhook tests are placeholders.
- Blocks: Reliable change validation for payment confirmation, dedupe, and lifecycle transitions.

## Test Coverage Gaps

**Webhook ingress and confirmation end-to-end behavior:**
- What's not tested: Real signature acceptance/rejection, duplicate handling, status transition side-effects with persistent storage.
- Files: `backend/tests/IntegrationTests/Payments/PaymentWebhookEndpointsTests.cs`, `backend/tests/IntegrationTests/Payments/PaymentConfirmationFlowTests.cs`
- Risk: Payment regressions can ship without failing tests.
- Priority: High

**Frontend auth/checkout route behavior and persistence edge cases:**
- What's not tested: Redirect flow, localStorage corruption handling under realistic UI interactions, protected/admin route outcomes.
- Files: `FronEnd/src/routes/ProtectedRoute.tsx`, `FronEnd/src/routes/AdminRoute.tsx`, `FronEnd/src/features/auth/utils/auth.storage.ts`, `FronEnd/src/pages/shop/Checkout.tsx`, `FronEnd/src/test/example.test.ts`
- Risk: Navigation/auth state defects surface only in manual testing.
- Priority: Medium

---

*Concerns audit: 2026-04-18*
