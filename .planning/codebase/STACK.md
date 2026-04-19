# Technology Stack

**Analysis Date:** 2026-04-18

## Languages

**Primary:**
- C# (.NET 10 target) - backend API and business layers in `backend/src/API`, `backend/src/Application`, `backend/src/Domain`, `backend/src/Infrastructure` (`*.csproj` target `net10.0`)
- TypeScript - frontend app and UI in `FronEnd/src/**/*.ts` and `FronEnd/src/**/*.tsx`

**Secondary:**
- CSS (Tailwind + custom CSS) - styling in `FronEnd/src/index.css` and Tailwind config in `FronEnd/tailwind.config.ts`
- JSON/XML/YAML - app/build configuration in `backend/src/API/appsettings*.json`, `FronEnd/package.json`, `backend/docker-compose.yml`, and `backend/*.csproj`

## Runtime

**Environment:**
- .NET runtime 10.0 for backend (`backend/src/API/API.csproj`, `backend/Dockerfile`)
- Node.js runtime for frontend tooling via Vite/Vitest/ESLint (`FronEnd/package.json` scripts)

**Package Manager:**
- Frontend: pnpm lockfile present in `FronEnd/pnpm-lock.yaml` (npm-compatible `package.json` scripts in `FronEnd/package.json`)
- Backend: NuGet package references in `backend/src/*/*.csproj`
- Lockfile: present for frontend (`FronEnd/pnpm-lock.yaml`, also `FronEnd/bun.lock`); missing for backend NuGet central lockfile

## Frameworks

**Core:**
- ASP.NET Core Web API 10.0 (`Microsoft.NET.Sdk.Web`) - HTTP API host and endpoint mapping in `backend/src/API/Program.cs`
- Entity Framework Core 10.0 + Npgsql provider - ORM/data access in `backend/src/Infrastructure/DependencyInjection.cs` and `backend/src/Infrastructure/Persistence/AppDbContext.cs`
- React 18 + React Router - SPA frontend in `FronEnd/src/main.tsx`, `FronEnd/src/App.tsx`, and `FronEnd/src/routes/*.tsx`
- Vite 5 + React SWC plugin - frontend dev/build pipeline in `FronEnd/vite.config.ts`

**Testing:**
- xUnit + Microsoft.NET.Test.Sdk - backend unit/integration tests in `backend/tests/UnitTests/UnitTests.csproj` and `backend/tests/IntegrationTests/IntegrationTests.csproj`
- Vitest + Testing Library + jsdom - frontend test runner/config in `FronEnd/vitest.config.ts` and `FronEnd/package.json`
- Playwright - frontend E2E configuration in `FronEnd/playwright.config.ts`

**Build/Dev:**
- Docker + Docker Compose - backend containerization/local stack in `backend/Dockerfile` and `backend/docker-compose.yml`
- Tailwind CSS + PostCSS - frontend styling pipeline in `FronEnd/tailwind.config.ts` and `FronEnd/postcss.config.js`
- ESLint (flat config) + TypeScript ESLint - frontend linting in `FronEnd/eslint.config.js`

## Key Dependencies

**Critical:**
- `mercadopago-sdk` (2.11.0) - checkout preference creation and payment integration in `backend/src/Infrastructure/Payments/MercadoPago/MercadoPagoPreferenceGateway.cs`
- `Npgsql.EntityFrameworkCore.PostgreSQL` (10.0.0) - PostgreSQL EF provider in `backend/src/Infrastructure/Infrastructure.csproj` and `backend/src/Infrastructure/DependencyInjection.cs`
- `Hangfire.Core`/`Hangfire.PostgreSql`/`Hangfire.AspNetCore` - durable background jobs and webhook processing in `backend/src/Infrastructure/Jobs/HangfireConfiguration.cs` and `backend/src/API/Payments/PaymentWebhookEndpoints.cs`
- `Microsoft.AspNetCore.Authentication.JwtBearer` - JWT bearer auth in `backend/src/API/Program.cs`

**Infrastructure:**
- `Serilog.AspNetCore` + `Serilog.Extensions.Hosting` - structured logging in `backend/src/API/API.csproj` and `backend/src/Infrastructure/Jobs/HangfireConfiguration.cs`
- `Microsoft.Extensions.Http` - typed HTTP clients for external APIs in `backend/src/Infrastructure/DependencyInjection.cs`
- `@tanstack/react-query` - frontend data-fetch state management dependency declared in `FronEnd/package.json`
- `@radix-ui/*` + shadcn UI config - reusable UI primitives in `FronEnd/package.json` and `FronEnd/components.json`

## Configuration

**Environment:**
- Backend configuration uses ASP.NET configuration binding from `appsettings*.json` and environment variables in `backend/src/API/appsettings.json`, `backend/src/API/appsettings.Development.json`, and `backend/docker-compose.yml`
- Required configuration sections include `ConnectionStrings:DefaultConnection`, `Jwt`, `MercadoPago`, `IdentityTokenDelivery`, `WhatsApp`, and `Hangfire` (bound in `backend/src/Infrastructure/DependencyInjection.cs` and `backend/src/Infrastructure/Jobs/HangfireConfiguration.cs`)
- Frontend runtime settings are mostly compile-time/build-time through Vite config and local storage usage in `FronEnd/vite.config.ts` and `FronEnd/src/features/settings/services/settings.service.ts`

**Build:**
- Backend build/publish: multi-stage Docker build in `backend/Dockerfile`
- Backend solution/project composition: `backend/backend.slnx` and `backend/src/*/*.csproj`
- Frontend build config: `FronEnd/vite.config.ts`, `FronEnd/tsconfig.json`, `FronEnd/tsconfig.app.json`

## Platform Requirements

**Development:**
- .NET SDK/runtime 10.0 to build/run backend projects in `backend/src/*/*.csproj`
- Node.js + pnpm (or compatible npm client) to run frontend scripts from `FronEnd/package.json`
- Docker for local dependency stack (`postgres`, `mailpit`, API container) in `backend/docker-compose.yml`

**Production:**
- Containerized ASP.NET API deployment on Linux base images (`mcr.microsoft.com/dotnet/aspnet:10.0`) defined in `backend/Dockerfile`
- PostgreSQL database required by EF Core and Hangfire storage (`backend/src/Infrastructure/DependencyInjection.cs`, `backend/src/Infrastructure/Jobs/HangfireConfiguration.cs`)
- External network access required for Mercado Pago and WhatsApp API calls from backend services in `backend/src/Infrastructure/Payments/MercadoPago/MercadoPagoPreferenceGateway.cs` and `backend/src/Infrastructure/Notifications/WhatsAppNotificationService.cs`

---

*Stack analysis: 2026-04-18*
