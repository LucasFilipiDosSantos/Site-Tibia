<!-- GSD:project-start source:PROJECT.md -->
## Project

Project not yet initialized. Run /gsd-new-project to set up.
<!-- GSD:project-end -->

<!-- GSD:stack-start source:codebase/STACK.md -->
## Technology Stack

## Languages
- C# (.NET 10 target) - backend API and business layers in `backend/src/API`, `backend/src/Application`, `backend/src/Domain`, `backend/src/Infrastructure` (`*.csproj` target `net10.0`)
- TypeScript - frontend app and UI in `FronEnd/src/**/*.ts` and `FronEnd/src/**/*.tsx`
- CSS (Tailwind + custom CSS) - styling in `FronEnd/src/index.css` and Tailwind config in `FronEnd/tailwind.config.ts`
- JSON/XML/YAML - app/build configuration in `backend/src/API/appsettings*.json`, `FronEnd/package.json`, `backend/docker-compose.yml`, and `backend/*.csproj`
## Runtime
- .NET runtime 10.0 for backend (`backend/src/API/API.csproj`, `backend/Dockerfile`)
- Node.js runtime for frontend tooling via Vite/Vitest/ESLint (`FronEnd/package.json` scripts)
- Frontend: pnpm lockfile present in `FronEnd/pnpm-lock.yaml` (npm-compatible `package.json` scripts in `FronEnd/package.json`)
- Backend: NuGet package references in `backend/src/*/*.csproj`
- Lockfile: present for frontend (`FronEnd/pnpm-lock.yaml`, also `FronEnd/bun.lock`); missing for backend NuGet central lockfile
## Frameworks
- ASP.NET Core Web API 10.0 (`Microsoft.NET.Sdk.Web`) - HTTP API host and endpoint mapping in `backend/src/API/Program.cs`
- Entity Framework Core 10.0 + Npgsql provider - ORM/data access in `backend/src/Infrastructure/DependencyInjection.cs` and `backend/src/Infrastructure/Persistence/AppDbContext.cs`
- React 18 + React Router - SPA frontend in `FronEnd/src/main.tsx`, `FronEnd/src/App.tsx`, and `FronEnd/src/routes/*.tsx`
- Vite 5 + React SWC plugin - frontend dev/build pipeline in `FronEnd/vite.config.ts`
- xUnit + Microsoft.NET.Test.Sdk - backend unit/integration tests in `backend/tests/UnitTests/UnitTests.csproj` and `backend/tests/IntegrationTests/IntegrationTests.csproj`
- Vitest + Testing Library + jsdom - frontend test runner/config in `FronEnd/vitest.config.ts` and `FronEnd/package.json`
- Playwright - frontend E2E configuration in `FronEnd/playwright.config.ts`
- Docker + Docker Compose - backend containerization/local stack in `backend/Dockerfile` and `backend/docker-compose.yml`
- Tailwind CSS + PostCSS - frontend styling pipeline in `FronEnd/tailwind.config.ts` and `FronEnd/postcss.config.js`
- ESLint (flat config) + TypeScript ESLint - frontend linting in `FronEnd/eslint.config.js`
## Key Dependencies
- `mercadopago-sdk` (2.11.0) - checkout preference creation and payment integration in `backend/src/Infrastructure/Payments/MercadoPago/MercadoPagoPreferenceGateway.cs`
- `Npgsql.EntityFrameworkCore.PostgreSQL` (10.0.0) - PostgreSQL EF provider in `backend/src/Infrastructure/Infrastructure.csproj` and `backend/src/Infrastructure/DependencyInjection.cs`
- `Hangfire.Core`/`Hangfire.PostgreSql`/`Hangfire.AspNetCore` - durable background jobs and webhook processing in `backend/src/Infrastructure/Jobs/HangfireConfiguration.cs` and `backend/src/API/Payments/PaymentWebhookEndpoints.cs`
- `Microsoft.AspNetCore.Authentication.JwtBearer` - JWT bearer auth in `backend/src/API/Program.cs`
- `Serilog.AspNetCore` + `Serilog.Extensions.Hosting` - structured logging in `backend/src/API/API.csproj` and `backend/src/Infrastructure/Jobs/HangfireConfiguration.cs`
- `Microsoft.Extensions.Http` - typed HTTP clients for external APIs in `backend/src/Infrastructure/DependencyInjection.cs`
- `@tanstack/react-query` - frontend data-fetch state management dependency declared in `FronEnd/package.json`
- `@radix-ui/*` + shadcn UI config - reusable UI primitives in `FronEnd/package.json` and `FronEnd/components.json`
## Configuration
- Backend configuration uses ASP.NET configuration binding from `appsettings*.json` and environment variables in `backend/src/API/appsettings.json`, `backend/src/API/appsettings.Development.json`, and `backend/docker-compose.yml`
- Required configuration sections include `ConnectionStrings:DefaultConnection`, `Jwt`, `MercadoPago`, `IdentityTokenDelivery`, `WhatsApp`, and `Hangfire` (bound in `backend/src/Infrastructure/DependencyInjection.cs` and `backend/src/Infrastructure/Jobs/HangfireConfiguration.cs`)
- Frontend runtime settings are mostly compile-time/build-time through Vite config and local storage usage in `FronEnd/vite.config.ts` and `FronEnd/src/features/settings/services/settings.service.ts`
- Backend build/publish: multi-stage Docker build in `backend/Dockerfile`
- Backend solution/project composition: `backend/backend.slnx` and `backend/src/*/*.csproj`
- Frontend build config: `FronEnd/vite.config.ts`, `FronEnd/tsconfig.json`, `FronEnd/tsconfig.app.json`
## Platform Requirements
- .NET SDK/runtime 10.0 to build/run backend projects in `backend/src/*/*.csproj`
- Node.js + pnpm (or compatible npm client) to run frontend scripts from `FronEnd/package.json`
- Docker for local dependency stack (`postgres`, `mailpit`, API container) in `backend/docker-compose.yml`
- Containerized ASP.NET API deployment on Linux base images (`mcr.microsoft.com/dotnet/aspnet:10.0`) defined in `backend/Dockerfile`
- PostgreSQL database required by EF Core and Hangfire storage (`backend/src/Infrastructure/DependencyInjection.cs`, `backend/src/Infrastructure/Jobs/HangfireConfiguration.cs`)
- External network access required for Mercado Pago and WhatsApp API calls from backend services in `backend/src/Infrastructure/Payments/MercadoPago/MercadoPagoPreferenceGateway.cs` and `backend/src/Infrastructure/Notifications/WhatsAppNotificationService.cs`
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->
## Conventions

## Naming Patterns
- Use `PascalCase.tsx` for page and component files in `FronEnd/src/pages/**` and `FronEnd/src/components/**` (examples: `FronEnd/src/pages/shop/Home.tsx`, `FronEnd/src/components/lootera/PublicLayout.tsx`).
- Use `kebab-case.ts` for utility/hook/service helper files and generated-style UI modules (examples: `FronEnd/src/features/auth/utils/auth.validators.ts`, `FronEnd/src/hooks/use-toast.ts`, `FronEnd/src/components/ui/alert-dialog.tsx`).
- Use `*.types.ts` suffix for domain type definitions (examples: `FronEnd/src/features/products/types/product.types.ts`, `FronEnd/src/features/auth/types/auth.types.ts`).
- Use `*.service.ts` suffix for data/service modules (examples: `FronEnd/src/features/cart/services/cart.service.ts`, `FronEnd/src/features/orders/services/order.service.ts`).
- Use `camelCase` for functions and methods (`normalizeEmail`, `validateLoginInput`, `getProductsByCategory` in `FronEnd/src/features/auth/utils/auth.validators.ts` and `FronEnd/src/features/products/services/product.service.ts`).
- Use hook naming with `use` prefix (`useCart` in `FronEnd/src/contexts/CartContext.tsx`, `useAuth` in `FronEnd/src/features/auth/context/AuthContext.tsx`, `useToast` in `FronEnd/src/hooks/use-toast.ts`).
- Use `camelCase` for local variables and constants scoped to a module (`queryClient` in `FronEnd/src/App.tsx`, `defaultSettings` in `FronEnd/src/features/settings/services/settings.service.ts`).
- Use `UPPER_SNAKE_CASE` for global/static constants (`AUTH_MODE` in `FronEnd/src/features/auth/types/auth.types.ts`, `CART_STORAGE_KEY` in `FronEnd/src/features/cart/services/cart.service.ts`, `STORAGE_KEYS` in `FronEnd/src/features/auth/utils/auth.storage.ts`).
- Use `PascalCase` for `interface` and `type` names (`Product`, `AuthUser`, `AuthResult`, `CartContextType` in `FronEnd/src/features/products/types/product.types.ts`, `FronEnd/src/features/auth/types/auth.types.ts`, `FronEnd/src/contexts/CartContext.tsx`).
- Use `*Props` suffix for component prop contracts (`ProtectedRouteProps` in `FronEnd/src/routes/ProtectedRoute.tsx`, `ButtonProps` in `FronEnd/src/components/ui/button.tsx`).
## Code Style
- Tool detected: no dedicated Prettier config file (`.prettierrc*`) is present.
- Formatting style is consistent with 2-space indentation, semicolons, double quotes, and trailing commas in multiline objects/arrays (examples in `FronEnd/src/App.tsx`, `FronEnd/src/features/auth/context/AuthContext.tsx`, `FronEnd/src/components/ui/button.tsx`).
- Prefer concise arrow functions for components and service methods (`const Home = () => {}` in `FronEnd/src/pages/shop/Home.tsx`, methods in `FronEnd/src/features/products/services/product.service.ts`).
- Tool: ESLint flat config with TypeScript integration in `FronEnd/eslint.config.js`.
- Use `@eslint/js` + `typescript-eslint` recommended presets (`FronEnd/eslint.config.js`).
- Enforce React hooks rules via `eslint-plugin-react-hooks` (`FronEnd/eslint.config.js`).
- Keep `react-refresh/only-export-components` warning enabled (`FronEnd/eslint.config.js`).
- Current rule override disables `@typescript-eslint/no-unused-vars`; keep this consistent unless lint policy is intentionally tightened (`FronEnd/eslint.config.js`).
## Import Organization
- Use `@/*` alias mapped to `./src/*` in `FronEnd/tsconfig.json`, `FronEnd/tsconfig.app.json`, and resolver aliases in `FronEnd/vite.config.ts` and `FronEnd/vitest.config.ts`.
## Error Handling
- Use guard throws for invalid provider/hook usage (`throw new Error(...)` in `FronEnd/src/contexts/CartContext.tsx` and `FronEnd/src/features/auth/context/AuthContext.tsx`).
- Use safe JSON parsing helpers with fallback return values instead of propagating parse exceptions (`safeParseJSON` in `FronEnd/src/features/cart/services/cart.service.ts`, `FronEnd/src/features/settings/services/settings.service.ts`, and `FronEnd/src/features/auth/utils/auth.storage.ts`).
- Use result-object pattern for expected auth failures rather than throwing (`AuthResult` in `FronEnd/src/features/auth/types/auth.types.ts`, returned by `FronEnd/src/features/auth/services/mock-auth.service.ts`).
## Logging
- Avoid routine console logging in feature code.
- Existing explicit log is for missing route telemetry in `FronEnd/src/pages/NotFound.tsx` (`console.error` in `useEffect`).
## Comments
- Use section headers to group route declarations or page sections (examples: `// Shop pages` in `FronEnd/src/App.tsx`, `/* Hero */` in `FronEnd/src/pages/shop/Home.tsx`).
- Use short inline comments for mock/prototype caveats (`// MOCK ONLY - Never use in production` in `FronEnd/src/features/auth/types/auth.types.ts`, mock backend note in `FronEnd/src/features/orders/services/order.service.ts`).
- Not used in analyzed source. Prefer typed signatures and clear names over doc blocks.
## Function Design
- Keep service functions compact and single-purpose (filter/find/transform methods in `FronEnd/src/features/products/services/product.service.ts` and `FronEnd/src/features/orders/services/order.service.ts`).
- Context providers can be larger but should centralize state transitions and side effects (`FronEnd/src/contexts/CartContext.tsx`, `FronEnd/src/features/auth/context/AuthContext.tsx`).
- Use typed object parameters for richer inputs (`login(input: LoginInput)` in `FronEnd/src/features/auth/services/mock-auth.service.ts`).
- Use primitive parameters for direct lookups (`getProductById(id: string)` in `FronEnd/src/features/products/services/product.service.ts`).
- Use explicit return types on public service methods (`(): Product[]`, `(): Cart`, `Promise<AuthResult<AuthUser>>`).
- Use union-based result contracts for recoverable domain errors (`AuthResult` in `FronEnd/src/features/auth/types/auth.types.ts`).
## Module Design
- Use default exports for page/layout components (`export default` in `FronEnd/src/pages/**` and `FronEnd/src/components/lootera/**`).
- Use named exports for services, hooks, contexts, and reusable utilities (`productService`, `useCart`, `AuthProvider`, `cn` in `FronEnd/src/features/**`, `FronEnd/src/contexts/CartContext.tsx`, `FronEnd/src/lib/utils.ts`).
- Not detected. Import directly from concrete module paths (examples in `FronEnd/src/App.tsx` and `FronEnd/src/pages/shop/Home.tsx`).
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->
## Architecture

## Pattern Overview
- Backend enforces inward dependency flow across explicit projects: `API -> Application -> Domain`, with `Infrastructure` implementing `Application` contracts (`/backend/src/API/API.csproj`, `/backend/src/Application/Application.csproj`, `/backend/src/Infrastructure/Infrastructure.csproj`).
- API surface is Minimal API endpoint modules grouped by business area (`/backend/src/API/*Endpoints.cs`) and composed centrally in `/backend/src/API/Program.cs`.
- Frontend composes global providers at app root and routes by page domain (`/FronEnd/src/App.tsx`), while domain logic is organized under `features/*` service/type folders.
## Layers
- Purpose: HTTP transport, auth policies, middleware, endpoint mapping, and DTO translation.
- Location: `/backend/src/API`
- Contains: `Program`, endpoint modules, auth/rate-limit middleware, error handler, route DTOs.
- Depends on: `Application` services/contracts, `Infrastructure` DI extension, ASP.NET Core packages.
- Used by: External clients (frontend, admin tools, webhook providers), integration tests.
- Purpose: Use-case orchestration and business workflows.
- Location: `/backend/src/Application`
- Contains: Services (`CatalogService`, `CheckoutService`, `PaymentWebhookProcessor`, `IdentityService`), contracts, use-case DTOs/exceptions.
- Depends on: `Domain` entities/value concepts and its own contracts.
- Used by: API endpoints and Infrastructure implementations.
- Purpose: Core domain entities and invariants independent of frameworks.
- Location: `/backend/src/Domain`
- Contains: Aggregates/entities for catalog, checkout, identity, inventory, payments, audit/products.
- Depends on: .NET BCL only (`/backend/src/Domain/Domain.csproj`).
- Used by: Application services/contracts and Infrastructure persistence mappings.
- Purpose: Externalized concerns: persistence, repository implementations, external gateways, jobs, notifications.
- Location: `/backend/src/Infrastructure`
- Contains: EF Core `AppDbContext`, repository classes, Mercado Pago gateway/signature validator, Hangfire setup, WhatsApp notifier, migrations, DI wiring.
- Depends on: `Application` and `Domain`, plus EF Core/Npgsql/Hangfire/MercadoPago SDK.
- Used by: API startup composition (`AddInfrastructure`) and runtime workflows.
- Purpose: Client-side rendering, routing, access guards, page composition.
- Location: `/FronEnd/src` (entry: `/FronEnd/src/main.tsx`, app shell: `/FronEnd/src/App.tsx`).
- Contains: Routes, page components, layout components, UI primitives.
- Depends on: React Router, React Query, auth/cart contexts, feature services.
- Used by: Browser users.
- Purpose: Encapsulate domain-specific client logic and local persistence stubs.
- Location: `/FronEnd/src/features`, `/FronEnd/src/contexts`
- Contains: `services/`, `types/`, auth/cart contexts, validators/storage helpers.
- Depends on: Shared mock data and browser storage patterns.
- Used by: Pages/components and route guards.
## Data Flow
- Backend: request-scoped service orchestration with persisted state in PostgreSQL via EF Core (`/backend/src/Infrastructure/Persistence/AppDbContext.cs`).
- Frontend: React context (`AuthContext`, `CartContext`) + local service state, with React Query available at root for server-state expansion (`/FronEnd/src/App.tsx`).
## Key Abstractions
- Purpose: Keep Minimal API registration segmented by bounded context.
- Examples: `/backend/src/API/Catalog/CatalogEndpoints.cs`, `/backend/src/API/Checkout/CheckoutEndpoints.cs`, `/backend/src/API/Payments/PaymentWebhookEndpoints.cs`.
- Pattern: `public static IEndpointRouteBuilder MapXEndpoints(this IEndpointRouteBuilder app)`.
- Purpose: Invert dependencies so application logic is storage/provider-agnostic.
- Examples: `/backend/src/Application/Catalog/Contracts/IProductRepository.cs`, `/backend/src/Application/Payments/Contracts/IMercadoPagoPreferenceGateway.cs`, `/backend/src/Application/Checkout/Contracts/ICheckoutInventoryGateway.cs`.
- Pattern: interfaces in `Application`, concrete implementations in `Infrastructure`.
- Purpose: Model core behavior and data integrity.
- Examples: `/backend/src/Domain/Checkout/Order.cs`, `/backend/src/Domain/Catalog/Product.cs`, `/backend/src/Domain/Audit/AuditLog.cs`.
- Pattern: constructors/factory methods and controlled mutation methods.
- Purpose: Central registration of repositories, gateways, options, and integrations.
- Examples: `/backend/src/Infrastructure/DependencyInjection.cs`.
- Pattern: `AddInfrastructure(IServiceCollection, IConfiguration)` with typed options and service lifetimes.
- Purpose: Keep UI components thin and move state/data logic into reusable modules.
- Examples: `/FronEnd/src/features/auth/context/AuthContext.tsx`, `/FronEnd/src/contexts/CartContext.tsx`, `/FronEnd/src/features/products/services/product.service.ts`.
- Pattern: context exposes action API; service encapsulates data source details.
## Entry Points
- Location: `/backend/src/API/Program.cs`
- Triggers: `dotnet run` / deployed ASP.NET process.
- Responsibilities: configure middleware/auth/DI/health/openapi and map all endpoint groups.
- Location: `/backend/src/API/*/*Endpoints.cs`
- Triggers: matching HTTP routes.
- Responsibilities: request validation/mapping, invoking services, returning contract responses.
- Location: `/FronEnd/src/main.tsx`
- Triggers: Vite app initialization in browser.
- Responsibilities: mount React root and start SPA render pipeline.
- Location: `/FronEnd/src/App.tsx`
- Triggers: loaded by main entry.
- Responsibilities: global provider composition, routing tree, not-found fallback.
## Error Handling
- Backend global exception handler maps domain/app exceptions to HTTP semantics in `/backend/src/API/ErrorHandling/GlobalExceptionHandler.cs`.
- Endpoint-level targeted handling for known provider not-found cases (example in checkout payment preference endpoint in `/backend/src/API/Checkout/CheckoutEndpoints.cs`).
- Frontend uses route guards + toast notifications for auth/interaction feedback (`/FronEnd/src/routes/*.tsx`, `/FronEnd/src/features/auth/context/AuthContext.tsx`, `/FronEnd/src/contexts/CartContext.tsx`).
## Cross-Cutting Concerns
- Backend validates at multiple layers: endpoint input mapping, service guards, options validators (`/backend/src/Infrastructure/Payments/MercadoPago/MercadoPagoOptionsValidator.cs`, `/backend/src/Infrastructure/Identity/Options/IdentityTokenDeliveryOptionsValidator.cs`).
- Frontend validates form and auth inputs in page handlers and utility validators (`/FronEnd/src/pages/shop/Checkout.tsx`, `/FronEnd/src/features/auth/utils/auth.validators.ts`).
- Backend JWT bearer auth + policy-based claims in `/backend/src/API/Program.cs` and `/backend/src/API/Auth/AuthPolicies.cs`.
- Frontend client-side auth state and role checks via context + guarded routes (`/FronEnd/src/features/auth/context/AuthContext.tsx`, `/FronEnd/src/routes/AdminRoute.tsx`).
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
