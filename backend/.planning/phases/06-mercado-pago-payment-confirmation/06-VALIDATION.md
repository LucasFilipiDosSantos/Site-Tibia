---
phase: 06
slug: mercado-pago-payment-confirmation
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-17
---

# Phase 06 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET test projects) |
| **Config file** | tests/UnitTests/UnitTests.csproj, tests/IntegrationTests/IntegrationTests.csproj |
| **Quick run command** | `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~Payment|FullyQualifiedName~Webhook"` |
| **Full suite command** | `dotnet test backend.slnx -v minimal` |
| **Estimated runtime** | ~120 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~Payment|FullyQualifiedName~Webhook"`
- **After every plan wave:** Run `dotnet test backend.slnx -v minimal`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 180 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 06-01-01 | 01 | 1 | PAY-01 | T-06-01 | Preference creation binds exact `external_reference=orderId` | integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~PaymentPreference"` | ✅ | ⬜ pending |
| 06-01-02 | 01 | 1 | PAY-01 | T-06-02 | Payment-link snapshot persists expected amount/currency | integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~PaymentPreference"` | ✅ | ⬜ pending |
| 06-02-01 | 02 | 2 | PAY-02 | T-06-03 | Invalid signature never mutates state | unit+integration | `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~WebhookSignature" && dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~PaymentWebhook"` | ✅ | ⬜ pending |
| 06-02-02 | 02 | 2 | PAY-02, PAY-03 | T-06-04 | Dedupe key prevents duplicate transition/log inflation | integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~PaymentWebhook"` | ✅ | ⬜ pending |
| 06-03-01 | 03 | 3 | PAY-04 | T-06-05 | Only verified approved/processed marks order Paid | integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~PaymentConfirmation"` | ✅ | ⬜ pending |
| 06-03-02 | 03 | 3 | PAY-03, PAY-04 | T-06-06 | Already-paid and non-approved events remain lifecycle no-op with audit | integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~PaymentConfirmation"` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/UnitTests/Payments/WebhookSignatureValidatorTests.cs` — signature/digest behavior stubs
- [ ] `tests/IntegrationTests/Payments/PaymentPreferenceEndpointsTests.cs` — payment init endpoint contract scaffold
- [ ] `tests/IntegrationTests/Payments/PaymentWebhookEndpointsTests.cs` — webhook ack + idempotency scaffold
- [ ] `tests/IntegrationTests/Payments/PaymentConfirmationFlowTests.cs` — paid transition contract scaffold

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Mercado Pago panel webhook secret reset/rotation drills | PAY-02 | External dashboard operation | Rotate secret in Mercado Pago UI, update env, send simulation, confirm invalid-old/valid-new signature behavior |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 180s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
