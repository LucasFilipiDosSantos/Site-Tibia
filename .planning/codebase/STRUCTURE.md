# Codebase Structure

**Analysis Date:** 2026-04-18

## Directory Layout

```text
Site-Tibia/
├── backend/                 # .NET backend (Clean Architecture + DDD)
│   ├── src/                 # API, Application, Domain, Infrastructure projects
│   ├── tests/               # Unit and integration test projects
│   ├── docs/                # Backend documentation artifacts
│   ├── docker-compose.yml   # Local service orchestration
│   └── backend.slnx         # Solution file linking backend projects
├── FronEnd/                 # React + Vite frontend SPA
│   ├── src/                 # App code: routes, pages, features, contexts, UI
│   ├── public/              # Static public assets
│   ├── package.json         # Frontend scripts/dependencies
│   ├── vite.config.ts       # Vite build/dev config
│   └── vitest.config.ts     # Frontend test runner config
└── .planning/codebase/      # Generated codebase mapping documents
```

## Directory Purposes

**`backend/src/API`:**
- Purpose: HTTP transport/composition and runtime bootstrapping.
- Contains: `Program.cs`, endpoint modules by domain, auth middleware/policies, global exception handler, request/response DTOs.
- Key files: `/backend/src/API/Program.cs`, `/backend/src/API/Auth/AuthEndpoints.cs`, `/backend/src/API/Checkout/CheckoutEndpoints.cs`, `/backend/src/API/ErrorHandling/GlobalExceptionHandler.cs`.

**`backend/src/Application`:**
- Purpose: Use-case orchestration and business services/contracts.
- Contains: `Services/` and `Contracts/` grouped by domain area (`Catalog`, `Checkout`, `Identity`, `Inventory`, `Payments`, `Audit`, `Products`).
- Key files: `/backend/src/Application/Catalog/Services/CatalogService.cs`, `/backend/src/Application/Checkout/Services/CheckoutService.cs`, `/backend/src/Application/Payments/Services/PaymentWebhookProcessor.cs`.

**`backend/src/Domain`:**
- Purpose: Domain entities and business primitives without infrastructure coupling.
- Contains: domain modules (`Catalog`, `Checkout`, `Identity`, `Inventory`, `Payments`, `Audit`, `Products`).
- Key files: `/backend/src/Domain/Audit/AuditLog.cs`, `/backend/src/Domain/Checkout/Order.cs`, `/backend/src/Domain/Catalog/Product.cs`.

**`backend/src/Infrastructure`:**
- Purpose: External adapters/implementations and persistence.
- Contains: repository implementations, options/validators, integrations (Mercado Pago, WhatsApp), Hangfire setup, EF Core context/configurations/migrations.
- Key files: `/backend/src/Infrastructure/DependencyInjection.cs`, `/backend/src/Infrastructure/Persistence/AppDbContext.cs`, `/backend/src/Infrastructure/Payments/MercadoPago/MercadoPagoPreferenceGateway.cs`.

**`backend/tests/IntegrationTests`:**
- Purpose: HTTP and integration-level verification of backend behavior.
- Contains: endpoint test suites by feature area.
- Key files: `/backend/tests/IntegrationTests/Catalog/CatalogCustomerEndpointsTests.cs`, `/backend/tests/IntegrationTests/Payments/PaymentWebhookEndpointsTests.cs`.

**`backend/tests/UnitTests`:**
- Purpose: unit-level test project for isolated verification.
- Contains: unit test suites (project exists and references all backend layers).
- Key files: `/backend/tests/UnitTests/UnitTests.csproj`.

**`FronEnd/src/pages`:**
- Purpose: route-level page components grouped by app area (`shop`, `auth`, `user`, `admin`).
- Contains: top-level screen components used directly by router.
- Key files: `/FronEnd/src/pages/shop/Home.tsx`, `/FronEnd/src/pages/admin/Dashboard.tsx`, `/FronEnd/src/pages/auth/Login.tsx`.

**`FronEnd/src/features`:**
- Purpose: feature-oriented client domain modules.
- Contains: per-feature `services/`, `types/`, and auth `context/` + `utils/`.
- Key files: `/FronEnd/src/features/auth/context/AuthContext.tsx`, `/FronEnd/src/features/products/services/product.service.ts`, `/FronEnd/src/features/orders/services/order.service.ts`.

**`FronEnd/src/contexts`:**
- Purpose: shared cross-feature state containers.
- Contains: React contexts/providers for global app state.
- Key files: `/FronEnd/src/contexts/CartContext.tsx`.

**`FronEnd/src/components`:**
- Purpose: reusable UI and layout building blocks.
- Contains: domain layouts in `lootera/` and extensive UI primitives in `ui/`.
- Key files: `/FronEnd/src/components/lootera/PublicLayout.tsx`, `/FronEnd/src/components/ui/button.tsx`.

## Key File Locations

**Entry Points:**
- `/backend/src/API/Program.cs`: backend process startup, DI/middleware composition, endpoint mapping.
- `/FronEnd/src/main.tsx`: frontend browser bootstrap.
- `/FronEnd/src/App.tsx`: frontend provider composition and route table.

**Configuration:**
- `/backend/src/API/appsettings.json`: backend runtime configuration.
- `/backend/src/API/API.csproj`: backend API package/dependency config.
- `/backend/src/Infrastructure/Infrastructure.csproj`: infrastructure integrations/dependencies.
- `/backend/backend.slnx`: backend project graph.
- `/FronEnd/package.json`: frontend scripts + dependency manifest.
- `/FronEnd/vite.config.ts`: frontend dev/build settings and `@` alias.
- `/FronEnd/tsconfig.json`: TypeScript compiler options and path mapping.
- `/FronEnd/vitest.config.ts`: frontend test configuration.

**Core Logic:**
- `/backend/src/Application/**/Services/*.cs`: backend use-case logic.
- `/backend/src/Domain/**/*.cs`: backend core domain models.
- `/backend/src/Infrastructure/**/Repositories/*.cs`: backend persistence adapters.
- `/FronEnd/src/features/**/services/*.ts`: frontend feature data/business helpers.
- `/FronEnd/src/routes/*.tsx`: frontend access-control and route wrappers.

**Testing:**
- `/backend/tests/IntegrationTests/**/*.cs`: backend integration tests.
- `/backend/tests/UnitTests/**/*.cs`: backend unit tests.
- `/FronEnd/src/test/setup.ts`: frontend test setup.
- `/FronEnd/src/test/*.test.ts`: frontend test specs.
- `/FronEnd/playwright.config.ts`: E2E tooling configuration.

## Naming Conventions

**Files:**
- Backend C# source uses `PascalCase` file names matching class names: `CatalogService.cs`, `PaymentWebhookEndpoints.cs`, `AppDbContext.cs`.
- Backend contracts use `I` prefix for interfaces: `IProductRepository.cs`, `ICheckoutRepository.cs`.
- Backend endpoint modules consistently end with `Endpoints.cs`.
- Frontend React component/page files use `PascalCase.tsx`: `AdminRoute.tsx`, `PublicLayout.tsx`, `ProductDetail.tsx`.
- Frontend service/type utility files use `kebab-case` with role suffixes: `product.service.ts`, `auth.validators.ts`, `settings.types.ts`.

**Directories:**
- Backend top-level bounded layers are `PascalCase`: `API`, `Application`, `Domain`, `Infrastructure`.
- Backend inside layers uses domain-centric folder names: `Catalog`, `Checkout`, `Payments`, `Identity`.
- Frontend uses lowercase domain folders for routes/pages/features (`pages/shop`, `features/auth`) and named UI groups (`components/ui`).

## Where to Add New Code

**New Feature:**
- Backend primary code: add endpoint module under `/backend/src/API/<Feature>/`, service/contracts under `/backend/src/Application/<Feature>/`, and domain model updates in `/backend/src/Domain/<Feature>/` if needed.
- Backend persistence/integration implementations: `/backend/src/Infrastructure/<Feature>/` and `Persistence/Configurations` + `Persistence/Migrations` for schema changes.
- Backend tests: `/backend/tests/IntegrationTests/<Feature>/` and `/backend/tests/UnitTests/<Feature>/`.
- Frontend feature implementation: `/FronEnd/src/features/<feature>/` plus route/page in `/FronEnd/src/pages/<area>/` and route registration in `/FronEnd/src/App.tsx`.

**New Component/Module:**
- Shared UI primitive: `/FronEnd/src/components/ui/`.
- Marketplace-specific layout/presentational component: `/FronEnd/src/components/lootera/`.
- New route guard: `/FronEnd/src/routes/`.

**Utilities:**
- Backend cross-cutting runtime helpers: `/backend/src/API/Auth/` or `/backend/src/Infrastructure/<Concern>/` depending on concern ownership.
- Frontend shared helpers: `/FronEnd/src/lib/`.
- Frontend hooks: `/FronEnd/src/hooks/`.

## Special Directories

**`/backend/src/*/obj` and `/backend/src/*/bin`:**
- Purpose: .NET build artifacts.
- Generated: Yes.
- Committed: No (should be ignored).

**`/backend/tests/*/obj` and `/backend/tests/*/bin`:**
- Purpose: test build artifacts.
- Generated: Yes.
- Committed: No (should be ignored).

**`/backend/src/Infrastructure/Persistence/Migrations`:**
- Purpose: EF Core schema migration history and model snapshots.
- Generated: Yes (via EF tooling), then curated.
- Committed: Yes.

**`/FronEnd/public`:**
- Purpose: static assets served as-is by Vite.
- Generated: No.
- Committed: Yes.

**`/.planning/codebase`:**
- Purpose: architecture/stack/convention/concern mapping docs consumed by GSD planning and execution workflows.
- Generated: Yes (agent-authored mapping artifacts).
- Committed: Yes (project process artifacts).

---

*Structure analysis: 2026-04-18*
