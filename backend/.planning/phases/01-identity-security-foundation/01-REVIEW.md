---
phase: 01-identity-security-foundation
reviewed: 2026-04-16T14:23:11Z
depth: standard
files_reviewed: 8
files_reviewed_list:
  - src/Infrastructure/Identity/Options/IdentityTokenDeliveryOptionsValidator.cs
  - src/Infrastructure/DependencyInjection.cs
  - src/Application/Identity/Exceptions/TokenDeliveryUnavailableException.cs
  - src/Application/Identity/Services/IdentityService.cs
  - src/Application/Identity/Services/SecurityAuditService.cs
  - src/API/ErrorHandling/GlobalExceptionHandler.cs
  - tests/IntegrationTests/Identity/ExternalTokenDeliveryRoundTripTests.cs
  - tests/UnitTests/Identity/VerificationAndPasswordResetRoundTripTests.cs
findings:
  critical: 0
  warning: 4
  info: 0
  total: 4
status: issues_found
---

# Phase 01: Code Review Report

**Reviewed:** 2026-04-16T14:23:11Z  
**Depth:** standard  
**Files Reviewed:** 8  
**Status:** issues_found

## Summary

Reviewed phase 01 plan 01-08 changes around SMTP token delivery guardrails, failure mapping, and regression coverage. The overall direction is strong (fail-fast config + anti-enumeration-safe request contracts), but there are correctness and security hardening gaps: placeholder validation can be bypassed by casing, cancellation can be swallowed into false success responses, startup DB defaults include hardcoded credentials, and one integration assertion is brittle.

## Warnings

### WR-01: Placeholder SMTP checks are case-sensitive and can be bypassed

**File:** `src/Infrastructure/Identity/Options/IdentityTokenDeliveryOptionsValidator.cs:46-47,83-87`  
**Issue:** `PlaceholderHosts.Contains(...)` and `PlaceholderValues.Contains(...)` use default case-sensitive `HashSet<string>`. Values like `SMTP.EXAMPLE.COM` or `Change-Me` bypass validation even though they are still placeholders.

**Fix:** Make placeholder sets case-insensitive (or normalize input to lower-invariant before matching).

```csharp
private static readonly HashSet<string> PlaceholderHosts =
[
    "smtp.dev.local",
    "smtp.example.com",
];

private static readonly HashSet<string> PlaceholderValues =
[
    "change-me",
    "dev-user",
    "dev-password",
    "example",
];

// safer:
private static readonly HashSet<string> PlaceholderHosts = new(StringComparer.OrdinalIgnoreCase)
{
    "smtp.dev.local",
    "smtp.example.com",
};

private static readonly HashSet<string> PlaceholderValues = new(StringComparer.OrdinalIgnoreCase)
{
    "change-me",
    "dev-user",
    "dev-password",
    "example",
};
```

### WR-02: Request cancellation is swallowed and converted into generic success

**File:** `src/Application/Identity/Services/IdentityService.cs:221-231,320-330`  
**Issue:** Broad `catch (Exception ex)` wraps all exceptions into `TokenDeliveryUnavailableException`, including `OperationCanceledException`. A canceled request can therefore be turned into a handled 200 generic success response, which is incorrect behavior and obscures cancellation semantics.

**Fix:** Allow cancellation exceptions to propagate; only wrap real transport failures.

```csharp
try
{
    await _tokenDelivery.DeliverEmailVerificationTokenAsync(payload, cancellationToken);
}
catch (OperationCanceledException)
{
    throw;
}
catch (Exception ex)
{
    _audit?.Record(SecurityAuditService.EmailVerificationDispatchFailed, user.Id, user.Email, null);
    throw new TokenDeliveryUnavailableException("email_verification_request", user.Email, ex);
}
```

### WR-03: Infrastructure startup falls back to hardcoded DB credentials

**File:** `src/Infrastructure/DependencyInjection.cs:18-20`  
**Issue:** Missing `DefaultConnection` silently falls back to `Username=postgres;Password=postgres`. This can boot with insecure defaults in misconfigured environments and masks configuration failures that should fail fast.

**Fix:** Remove credential fallback and require an explicit connection string (optionally allow a dev-only fallback guarded by environment).

```csharp
var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");
}
```

### WR-04: Integration assertions rely on exact raw JSON string payload

**File:** `tests/IntegrationTests/Identity/ExternalTokenDeliveryRoundTripTests.cs:199,211`  
**Issue:** Assertions compare full raw JSON text (`"{\"message\":...}"`). This is brittle to harmless serializer/output changes (property ordering/formatting/options) and can cause flaky contract tests.

**Fix:** Deserialize and assert the semantic payload field.

```csharp
var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
Assert.Equal("If the account exists, a verification link was sent.", body?["message"]);
```

---

_Reviewed: 2026-04-16T14:23:11Z_  
_Reviewer: the agent (gsd-code-reviewer)_  
_Depth: standard_
