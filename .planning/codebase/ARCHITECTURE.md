# Architecture

**Analysis Date:** 2026-04-18

## Pattern Overview

**Overall:** Polyrepo-style monorepo with two primary applications: a Clean Architecture + DDD backend (`/backend`) and a feature-organized React SPA frontend (`/FronEnd`).

**Key Characteristics:**
- Backend enforces inward dependency flow across explicit projects: `API -> Application -> Domain`, with `Infrastructure` implementing `Application` contracts (`/backend/src/API/API.csproj`, `/backend/src/Application/Application.csproj`, `/backend/src/Infrastructure/Infrastructure.csproj`).
- API surface is Minimal API endpoint modules grouped by business area (`/backend/src/API/*Endpoints.cs`) and composed centrally in `/backend/src/API/Program.cs`.
- Frontend composes global providers at app root and routes by page domain (`/FronEnd/src/App.tsx`), while domain logic is organized under `features/*` service/type folders.

## Layers

**Backend Transport/Composition Layer (API):**
- Purpose: HTTP transport, auth policies, middleware, endpoint mapping, and DTO translation.
- Location: `/backend/src/API`
- Contains: `Program`, endpoint modules, auth/rate-limit middleware, error handler, route DTOs.
- Depends on: `Application` services/contracts, `Infrastructure` DI extension, ASP.NET Core packages.
- Used by: External clients (frontend, admin tools, webhook providers), integration tests.

**Backend Application Layer:**
- Purpose: Use-case orchestration and business workflows.
- Location: `/backend/src/Application`
- Contains: Services (`CatalogService`, `CheckoutService`, `PaymentWebhookProcessor`, `IdentityService`), contracts, use-case DTOs/exceptions.
- Depends on: `Domain` entities/value concepts and its own contracts.
- Used by: API endpoints and Infrastructure implementations.

**Backend Domain Layer:**
- Purpose: Core domain entities and invariants independent of frameworks.
- Location: `/backend/src/Domain`
- Contains: Aggregates/entities for catalog, checkout, identity, inventory, payments, audit/products.
- Depends on: .NET BCL only (`/backend/src/Domain/Domain.csproj`).
- Used by: Application services/contracts and Infrastructure persistence mappings.

**Backend Infrastructure Layer:**
- Purpose: Externalized concerns: persistence, repository implementations, external gateways, jobs, notifications.
- Location: `/backend/src/Infrastructure`
- Contains: EF Core `AppDbContext`, repository classes, Mercado Pago gateway/signature validator, Hangfire setup, WhatsApp notifier, migrations, DI wiring.
- Depends on: `Application` and `Domain`, plus EF Core/Npgsql/Hangfire/MercadoPago SDK.
- Used by: API startup composition (`AddInfrastructure`) and runtime workflows.

**Frontend UI & Routing Layer:**
- Purpose: Client-side rendering, routing, access guards, page composition.
- Location: `/FronEnd/src` (entry: `/FronEnd/src/main.tsx`, app shell: `/FronEnd/src/App.tsx`).
- Contains: Routes, page components, layout components, UI primitives.
- Depends on: React Router, React Query, auth/cart contexts, feature services.
- Used by: Browser users.

**Frontend Feature/State Layer:**
- Purpose: Encapsulate domain-specific client logic and local persistence stubs.
- Location: `/FronEnd/src/features`, `/FronEnd/src/contexts`
- Contains: `services/`, `types/`, auth/cart contexts, validators/storage helpers.
- Depends on: Shared mock data and browser storage patterns.
- Used by: Pages/components and route guards.

## Data Flow

**Backend request-to-domain flow (catalog/checkout/auth):**

1. HTTP request hits mapped endpoint module in `/backend/src/API/*/*Endpoints.cs`.
2. Endpoint resolves app service/repository from DI configured in `/backend/src/API/Program.cs` and `/backend/src/Infrastructure/DependencyInjection.cs`.
3. Application service applies validation + use-case rules (`/backend/src/Application/**/Services/*.cs`).
4. Service reads/writes domain entities through repository/gateway contracts in `/backend/src/Application/**/Contracts/*.cs`.
5. Infrastructure implementation persists through EF Core `AppDbContext` and returns data (`/backend/src/Infrastructure/**/Repositories/*.cs`).
6. Endpoint maps result to transport DTO and returns `Results.*` response.

**Payment webhook flow (Mercado Pago):**

1. Provider posts to `/payments/mercadopago/webhook` in `/backend/src/API/Payments/PaymentWebhookEndpoints.cs`.
2. Ingress validates signature via `PaymentWebhookIngressService` + validator contract.
3. Endpoint persists inbound webhook log via `IPaymentWebhookLogRepository`.
4. Endpoint enqueues async processor through Hangfire-style background client.
5. `PaymentWebhookProcessor` applies dedupe + monotonic status checks and triggers confirmation transitions when applicable.

**Frontend route and state flow:**

1. `/FronEnd/src/main.tsx` mounts `<App/>`.
2. `/FronEnd/src/App.tsx` wraps app in `QueryClientProvider`, `AuthProvider`, and `CartProvider`, then defines route table.
3. Route guards (`/FronEnd/src/routes/ProtectedRoute.tsx`, `/FronEnd/src/routes/AdminRoute.tsx`) gate access using `useAuth()` context state.
4. Pages call feature services (`/FronEnd/src/features/*/services/*.ts`) and context actions.
5. Cart/auth contexts persist to local storage through service/util adapters.

**State Management:**
- Backend: request-scoped service orchestration with persisted state in PostgreSQL via EF Core (`/backend/src/Infrastructure/Persistence/AppDbContext.cs`).
- Frontend: React context (`AuthContext`, `CartContext`) + local service state, with React Query available at root for server-state expansion (`/FronEnd/src/App.tsx`).

## Key Abstractions

**Endpoint module extension methods:**
- Purpose: Keep Minimal API registration segmented by bounded context.
- Examples: `/backend/src/API/Catalog/CatalogEndpoints.cs`, `/backend/src/API/Checkout/CheckoutEndpoints.cs`, `/backend/src/API/Payments/PaymentWebhookEndpoints.cs`.
- Pattern: `public static IEndpointRouteBuilder MapXEndpoints(this IEndpointRouteBuilder app)`.

**Application contracts as ports:**
- Purpose: Invert dependencies so application logic is storage/provider-agnostic.
- Examples: `/backend/src/Application/Catalog/Contracts/IProductRepository.cs`, `/backend/src/Application/Payments/Contracts/IMercadoPagoPreferenceGateway.cs`, `/backend/src/Application/Checkout/Contracts/ICheckoutInventoryGateway.cs`.
- Pattern: interfaces in `Application`, concrete implementations in `Infrastructure`.

**Domain-first entities:**
- Purpose: Model core behavior and data integrity.
- Examples: `/backend/src/Domain/Checkout/Order.cs`, `/backend/src/Domain/Catalog/Product.cs`, `/backend/src/Domain/Audit/AuditLog.cs`.
- Pattern: constructors/factory methods and controlled mutation methods.

**Infrastructure composition root:**
- Purpose: Central registration of repositories, gateways, options, and integrations.
- Examples: `/backend/src/Infrastructure/DependencyInjection.cs`.
- Pattern: `AddInfrastructure(IServiceCollection, IConfiguration)` with typed options and service lifetimes.

**Frontend provider + feature-service split:**
- Purpose: Keep UI components thin and move state/data logic into reusable modules.
- Examples: `/FronEnd/src/features/auth/context/AuthContext.tsx`, `/FronEnd/src/contexts/CartContext.tsx`, `/FronEnd/src/features/products/services/product.service.ts`.
- Pattern: context exposes action API; service encapsulates data source details.

## Entry Points

**Backend host startup:**
- Location: `/backend/src/API/Program.cs`
- Triggers: `dotnet run` / deployed ASP.NET process.
- Responsibilities: configure middleware/auth/DI/health/openapi and map all endpoint groups.

**Backend endpoint groups:**
- Location: `/backend/src/API/*/*Endpoints.cs`
- Triggers: matching HTTP routes.
- Responsibilities: request validation/mapping, invoking services, returning contract responses.

**Frontend browser bootstrap:**
- Location: `/FronEnd/src/main.tsx`
- Triggers: Vite app initialization in browser.
- Responsibilities: mount React root and start SPA render pipeline.

**Frontend app shell/router:**
- Location: `/FronEnd/src/App.tsx`
- Triggers: loaded by main entry.
- Responsibilities: global provider composition, routing tree, not-found fallback.

## Error Handling

**Strategy:** Centralized exception-to-ProblemDetails mapping on backend, local UI feedback toasts and redirect guards on frontend.

**Patterns:**
- Backend global exception handler maps domain/app exceptions to HTTP semantics in `/backend/src/API/ErrorHandling/GlobalExceptionHandler.cs`.
- Endpoint-level targeted handling for known provider not-found cases (example in checkout payment preference endpoint in `/backend/src/API/Checkout/CheckoutEndpoints.cs`).
- Frontend uses route guards + toast notifications for auth/interaction feedback (`/FronEnd/src/routes/*.tsx`, `/FronEnd/src/features/auth/context/AuthContext.tsx`, `/FronEnd/src/contexts/CartContext.tsx`).

## Cross-Cutting Concerns

**Logging:** Structured logging with Serilog/Hangfire ecosystem in backend package setup (`/backend/src/API/API.csproj`, `/backend/src/Infrastructure/Jobs/HangfireConfiguration.cs`); app-service logging via `ILogger<T>` (`/backend/src/Application/Payments/Services/PaymentWebhookProcessor.cs`).

**Validation:**
- Backend validates at multiple layers: endpoint input mapping, service guards, options validators (`/backend/src/Infrastructure/Payments/MercadoPago/MercadoPagoOptionsValidator.cs`, `/backend/src/Infrastructure/Identity/Options/IdentityTokenDeliveryOptionsValidator.cs`).
- Frontend validates form and auth inputs in page handlers and utility validators (`/FronEnd/src/pages/shop/Checkout.tsx`, `/FronEnd/src/features/auth/utils/auth.validators.ts`).

**Authentication:**
- Backend JWT bearer auth + policy-based claims in `/backend/src/API/Program.cs` and `/backend/src/API/Auth/AuthPolicies.cs`.
- Frontend client-side auth state and role checks via context + guarded routes (`/FronEnd/src/features/auth/context/AuthContext.tsx`, `/FronEnd/src/routes/AdminRoute.tsx`).

---

*Architecture analysis: 2026-04-18*
