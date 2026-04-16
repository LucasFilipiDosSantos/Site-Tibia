<!-- GSD:project-start source:PROJECT.md -->
## Project

**Tibia Webstore Backend**

This project is a backend platform for a Tibia-focused webstore that sells virtual goods and services for the Aurera and Eternia servers. It supports the full commerce lifecycle: product catalog, inventory control, ordering, payment confirmation, and delivery orchestration. It is built to serve both customers (self-service purchasing and tracking) and operators (admin management and fulfillment workflows).

**Core Value:** Customers can reliably buy Tibia goods and receive confirmed, trackable delivery with minimal manual intervention.

### Constraints

- **Tech stack**: C# with .NET and ASP.NET Core Web API — required to align with existing implementation direction
- **Persistence**: PostgreSQL — required for transactional consistency and relational domain modeling
- **Architecture**: Clean Architecture + DDD layering — required for maintainability and bounded-context clarity
- **Integrations**: Mercado Pago and WhatsApp API — required to support payment processing and operational alerts
- **Security**: HTTPS, password hashing, email verification, password reset — required to protect accounts and transactions
- **Reliability**: Background jobs for retries, payment confirmation, and delivery automation — required to reduce fulfillment failures
<!-- GSD:project-end -->

<!-- GSD:stack-start source:research/STACK.md -->
## Technology Stack

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
# Core
# Supporting
# Dev dependencies / tooling packages (optional by project structure)
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
- Use modular monolith + Postgres + Hangfire in same deployable
- Because it minimizes ops burden while preserving clean boundaries for later extraction
- Keep API monolith, split async workers into separate process, add Redis-backed rate limiting and stronger outbox/eventing discipline
- Because order capture and fulfillment orchestration scale differently and need independent throughput controls
## Version Compatibility
| Package A | Compatible With | Notes |
|-----------|-----------------|-------|
| `Microsoft.EntityFrameworkCore@10.0.x` | `Npgsql.EntityFrameworkCore.PostgreSQL@10.0.x` | Keep EF major and provider major aligned. |
| `ASP.NET Core@10.0.x` | `Serilog.AspNetCore@10.0.x` | Align major versions to avoid hosting API mismatch. |
| `Hangfire.Core@1.8.x` | `Hangfire.PostgreSql@1.21.x` | Validate storage package release notes before upgrades. |
## Prescriptive Recommendation for This Project
## Sources
- .NET support policy (updated 2026-03-12): https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core — verified LTS/STS lifecycles (**HIGH**)
- ASP.NET Core 10 release notes (updated 2026-01-29): https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0 — current platform track (**HIGH**)
- EF Core releases/support (updated 2025-11-20): https://learn.microsoft.com/en-us/ef/core/what-is-new/ — EF 10 support window (**HIGH**)
- PostgreSQL versioning policy (site update 2026-02-26): https://www.postgresql.org/support/versioning/ — PG major/minor support cadence (**HIGH**)
- Npgsql EF provider docs: https://www.npgsql.org/efcore/ — provider usage and EF 9+ config guidance (**HIGH**)
- NuGet package feeds for current versions:
- Mercado Pago .NET SDK repository + release evidence:
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->
## Conventions

### Enforceable Architecture Boundary Guardrails (PR Checks)

- [ ] `src/Domain` stays framework-free (no ASP.NET Core, EF Core, Npgsql, Hangfire, HTTP, or external SDK references).
- [ ] `src/Application` depends only on `src/Domain` abstractions and shared contracts (never on `src/API` or `src/Infrastructure`).
- [ ] `src/API` contains transport/composition concerns only (controllers, DTO mapping, auth/middleware, DI wiring), not domain/business rules.
- [ ] `src/Infrastructure` implements interfaces from `src/Application`/`src/Domain` and contains persistence/integration details; it never becomes an orchestration layer.
- [ ] Dependency direction always points inward: `API -> Application -> Domain`; `Infrastructure -> Application/Domain`; no reverse or cross-layer shortcuts.
- [ ] New abstractions are owned by inner layers (`Domain` or `Application`), while outer layers only implement or consume them.
- [ ] `tests/UnitTests` test `Domain`/`Application` behavior in isolation (mocks/fakes), without real database, network, or filesystem dependencies.
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->
## Architecture

Architecture not yet mapped. Follow existing patterns found in the codebase.
<!-- GSD:architecture-end -->

<!-- GSD:skills-start source:skills/ -->
## Project Skills

No project skills found. Add skills to any of: `.claude/skills/`, `.agents/skills/`, `.cursor/skills/`, or `.github/skills/` with a `SKILL.md` index file.
<!-- GSD:skills-end -->

<!-- GSD:workflow-start source:GSD defaults -->
## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd-quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd-debug` for investigation and bug fixing
- `/gsd-execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->



<!-- GSD:profile-start -->
## Developer Profile

> Profile not yet configured. Run `/gsd-profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
