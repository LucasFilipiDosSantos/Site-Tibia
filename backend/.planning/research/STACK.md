# Stack Research

**Domain:** Tibia virtual goods commerce backend  
**Updated:** 2026-04-14  
**Intent:** Stack guidance that fits `src/API`, `src/Application`, `src/Domain`, `src/Infrastructure`, `tests/UnitTests`

## Baseline Stack

| Area | Choice | Notes |
|---|---|---|
| Runtime | .NET 10 + ASP.NET Core Web API | Aligns with required backend stack and clean layering |
| Domain/App modeling | C# class libraries (`Domain`, `Application`) | Keep business core framework-light |
| Persistence | PostgreSQL 17 + EF Core 10 + Npgsql | Transactional consistency for orders/payments |
| Background work | Hangfire + Hangfire.PostgreSql | Retries, reconciliation, fulfillment automation |
| Logging/Tracing | Serilog + OpenTelemetry | Operational visibility and auditability |
| Integrations | Mercado Pago SDK, WhatsApp provider client | Through Infrastructure adapters only |
| Tests | xUnit in `tests/UnitTests` | Focus on Domain/Application behavior |

## Package Placement by Project

### `src/API`
- `Microsoft.AspNetCore.*` (web host/controllers/auth)
- `Serilog.AspNetCore` (host logging)
- `Asp.Versioning.Http` (if versioning enabled)
- Optional health check endpoint wiring

### `src/Application`
- `MediatR` (optional pattern choice)
- Validation library (optional)
- Contracts/ports only (no EF/provider packages)

### `src/Domain`
- Prefer no external dependencies
- If needed, only small, stable utility libs

### `src/Infrastructure`
- `Microsoft.EntityFrameworkCore`
- `Npgsql.EntityFrameworkCore.PostgreSQL`, `Npgsql`
- `Hangfire.Core`, `Hangfire.PostgreSql`
- Mercado Pago SDK and WhatsApp client SDK/HTTP implementation
- `OpenTelemetry.Extensions.Hosting` and exporter packages as needed
- `AspNetCore.HealthChecks.NpgSql` (if health checks configured here)

### `tests/UnitTests`
- `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`
- `FluentAssertions` (optional)
- Mocking library (optional)

## Must-Follow Separation Rules

1. `Domain` and `Application` must not depend on EF Core, ASP.NET Core, or provider SDKs.
2. `Infrastructure` depends on `Application` (implements ports), not the other way around.
3. Keep all migrations and DbContext configuration in `src/Infrastructure`.
4. Keep API contracts (request/response models) out of `Domain`.
5. Keep payment/notification provider DTOs out of `Application` and `Domain`.

## Minimal Dependency Matrix

```text
API:            Application, Infrastructure (composition only)
Application:    Domain
Domain:         (none)
Infrastructure: Application, Domain
UnitTests:      Application, Domain
```

## Actionable Setup Sequence

1. Add core web host/auth/logging packages to `src/API`.
2. Add EF Core + Npgsql + Hangfire + provider SDKs to `src/Infrastructure`.
3. Keep `src/Application` focused on interfaces/use cases; avoid infra packages.
4. Keep `src/Domain` dependency-light and invariant-focused.
5. Add/keep unit test packages in `tests/UnitTests`.

## Avoid

- Adding `DbContext` references in `src/API` controllers.
- Installing Mercado Pago/WhatsApp SDK directly in `src/Application` or `src/Domain`.
- Letting entity classes depend on serialization/web framework attributes unless unavoidable.

---
*Concise stack baseline aligned to the current solution layout.*
