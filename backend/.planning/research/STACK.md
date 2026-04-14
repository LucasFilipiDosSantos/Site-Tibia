# Stack Research

**Domain:** Tibia virtual goods webstore backend (digital commerce + manual/automated fulfillment)
**Researched:** 2026-04-14
**Confidence:** MEDIUM-HIGH

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended | Confidence |
|------------|---------|---------|-----------------|------------|
| .NET + ASP.NET Core Web API | 10.0.x (LTS) | API platform, auth, middleware, background hosting | Current LTS line with support through Nov/2028; strongest ecosystem fit for clean architecture + high-throughput APIs. | HIGH |
| EF Core | 10.0.x | ORM, migrations, transactional domain persistence | Aligns with .NET 10 lifecycle; mature tooling for DDD aggregate persistence and migration workflows. | HIGH |
| PostgreSQL | 17.x (current minor: 17.9) | Primary transactional database | Best fit for order/payment consistency, JSONB for flexible metadata, robust indexing/locking, 5-year support model. | HIGH |
| Npgsql + Npgsql EF Provider | Npgsql 10.0.2 + EF provider 10.0.1 | PostgreSQL connectivity and EF provider | Canonical PostgreSQL provider stack for .NET; tracks EF major versions and supports provider-level tuning. | HIGH |
| Mercado Pago SDK (.NET) | 2.11.0 | Payment creation + API integration | Official SDK, active releases in 2025, supports retries/idempotency headers and reduces custom API plumbing risk. | MEDIUM |

### Supporting Libraries

| Library | Version | Purpose | When to Use | Confidence |
|---------|---------|---------|-------------|------------|
| Hangfire.Core | 1.8.23 | Background jobs for webhook retries, fulfillment orchestration, notifications | Use for retryable async workflows (payment confirmation reconciliation, delivery retries, delayed/manual follow-ups). | HIGH |
| Hangfire.PostgreSql | 1.21.1 | PostgreSQL storage backend for Hangfire | Use when you want one operational DB stack (Postgres only) and durable at-least-once job execution. | MEDIUM |
| Serilog.AspNetCore | 10.0.0 | Structured logging | Use for JSON logs with correlation IDs, payment/order traceability, and incident debugging. | HIGH |
| OpenTelemetry.Extensions.Hosting | 1.15.2 | Traces/metrics/logs instrumentation pipeline | Use from day 1 for observability of webhook latency, order lifecycle spans, and downstream provider failures. | HIGH |
| StackExchange.Redis | 2.8.x (stable line) | Cache + distributed coordination | Use for short-lived session/cache, idempotency key cache, and rate limiting state. Avoid early overuse. | MEDIUM |
| Asp.Versioning.Http | 8.1.1 | API versioning | Use once public API contracts are consumed by multiple clients (admin panel, customer frontend, partner tools). | MEDIUM |
| AspNetCore.HealthChecks.NpgSql | 9.0.0 | Health probes | Use for readiness/liveness and dependency health checks in container/orchestrated deployment. | MEDIUM |

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| .NET SDK | Build/test/runtime toolchain | Pin via `global.json` to avoid accidental major drift across environments. |
| EF Core CLI (`dotnet ef`) | Migration generation and DB updates | Keep migrations in Infrastructure layer; enforce migration review in PR. |
| Docker + Docker Compose | Local infra (Postgres, Redis, observability stack) | Standardize local onboarding and CI parity. |
| Testcontainers for .NET | Integration tests with real Postgres/Redis | Prefer over in-memory DB fakes for order/payment invariants. |

## Installation

```bash
# Core
dotnet add package Microsoft.EntityFrameworkCore --version 10.0.0
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.1
dotnet add package Npgsql --version 10.0.2
dotnet add package mercadopago-sdk --version 2.11.0

# Supporting
dotnet add package Hangfire.Core --version 1.8.23
dotnet add package Hangfire.PostgreSql --version 1.21.1
dotnet add package Serilog.AspNetCore --version 10.0.0
dotnet add package OpenTelemetry.Extensions.Hosting --version 1.15.2
dotnet add package StackExchange.Redis --version 2.8.41
dotnet add package Asp.Versioning.Http --version 8.1.1
dotnet add package AspNetCore.HealthChecks.NpgSql --version 9.0.0

# Dev dependencies / tooling packages (optional by project structure)
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.0
dotnet add package Testcontainers.PostgreSql --version 4.5.0
```

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| ASP.NET Core Web API | Node.js (NestJS/Fastify) | Only if team is significantly stronger in TS/Node and C# constraint is removed (it is currently required). |
| EF Core 10 + Npgsql | Dapper-first data layer | Use only for a narrow, performance-critical read path; keep EF as the transactional write model backbone. |
| Hangfire + Postgres | Quartz.NET or cloud-native queue workers | Use if you already run a managed queue ecosystem (e.g., SQS + workers) and want strict infra separation. |
| Redis cache | DB-only caching strategy | Use only for very low scale MVPs; expect rising DB load and poorer idempotency/rate-limit ergonomics. |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| Starting greenfield on out-of-support .NET/EF versions (6/7) | Security/update exposure and shorter/no support windows | .NET 10 + EF Core 10 (or .NET 8/EF8 only if forced by org constraints) |
| Hand-rolled webhook retry/queue framework | Hidden reliability bugs, duplicate execution, poor observability | Hangfire durable jobs + explicit idempotency keys |
| DB polling as primary payment confirmation mechanism | Slower confirmation, higher API cost, race conditions | Webhook-first flow + reconciliation jobs |
| In-memory fake DB for core order/payment tests | Misses transaction/isolation and SQL behavior bugs | Testcontainers + real Postgres integration tests |

## Stack Patterns by Variant

**If traffic is low (<1k orders/day) and team is small:**
- Use modular monolith + Postgres + Hangfire in same deployable
- Because it minimizes ops burden while preserving clean boundaries for later extraction

**If traffic grows rapidly (>10k orders/day) or fulfillment complexity increases:**
- Keep API monolith, split async workers into separate process, add Redis-backed rate limiting and stronger outbox/eventing discipline
- Because order capture and fulfillment orchestration scale differently and need independent throughput controls

## Version Compatibility

| Package A | Compatible With | Notes |
|-----------|-----------------|-------|
| `Microsoft.EntityFrameworkCore@10.0.x` | `Npgsql.EntityFrameworkCore.PostgreSQL@10.0.x` | Keep EF major and provider major aligned. |
| `ASP.NET Core@10.0.x` | `Serilog.AspNetCore@10.0.x` | Align major versions to avoid hosting API mismatch. |
| `Hangfire.Core@1.8.x` | `Hangfire.PostgreSql@1.21.x` | Validate storage package release notes before upgrades. |

## Prescriptive Recommendation for This Project

Use this baseline:
1. **.NET 10 + ASP.NET Core Web API** for service runtime
2. **EF Core 10 + Npgsql + PostgreSQL 17** for transactional domain
3. **MercadoPago SDK + webhook-first confirmation** for payments
4. **Hangfire + PostgreSQL storage** for retries/automation workflows
5. **Serilog + OpenTelemetry** for auditability and operations

This is the standard 2025+ pragmatic stack for a C# digital-goods commerce backend: stable, well-supported, and optimized for payment-webhook reliability more than novelty.

## Sources

- .NET support policy (updated 2026-03-12): https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core — verified LTS/STS lifecycles (**HIGH**)
- ASP.NET Core 10 release notes (updated 2026-01-29): https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0 — current platform track (**HIGH**)
- EF Core releases/support (updated 2025-11-20): https://learn.microsoft.com/en-us/ef/core/what-is-new/ — EF 10 support window (**HIGH**)
- PostgreSQL versioning policy (site update 2026-02-26): https://www.postgresql.org/support/versioning/ — PG major/minor support cadence (**HIGH**)
- Npgsql EF provider docs: https://www.npgsql.org/efcore/ — provider usage and EF 9+ config guidance (**HIGH**)
- NuGet package feeds for current versions:
  - Npgsql EF provider: https://api.nuget.org/v3-flatcontainer/npgsql.entityframeworkcore.postgresql/index.json (**HIGH**)
  - Npgsql: https://api.nuget.org/v3-flatcontainer/npgsql/index.json (**HIGH**)
  - Hangfire.Core: https://api.nuget.org/v3-flatcontainer/hangfire.core/index.json (**HIGH**)
  - Hangfire.PostgreSql: https://api.nuget.org/v3-flatcontainer/hangfire.postgresql/index.json (**HIGH**)
  - Serilog.AspNetCore: https://api.nuget.org/v3-flatcontainer/serilog.aspnetcore/index.json (**HIGH**)
  - OpenTelemetry.Extensions.Hosting: https://api.nuget.org/v3-flatcontainer/opentelemetry.extensions.hosting/index.json (**HIGH**)
  - MercadoPago SDK: https://api.nuget.org/v3-flatcontainer/mercadopago-sdk/index.json (**HIGH**)
- Mercado Pago .NET SDK repository + release evidence:
  - https://github.com/mercadopago/sdk-dotnet
  - https://www.nuget.org/packages/mercadopago-sdk
  (**MEDIUM-HIGH**, official but webhook docs page fetch was too large to parse fully in this run)

---
*Stack research for: Tibia virtual goods webstore backend*
*Researched: 2026-04-14*
