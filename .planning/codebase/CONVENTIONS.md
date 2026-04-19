# Coding Conventions

**Analysis Date:** 2026-04-18

## Naming Patterns

**Files:**
- Use `PascalCase.tsx` for page and component files in `FronEnd/src/pages/**` and `FronEnd/src/components/**` (examples: `FronEnd/src/pages/shop/Home.tsx`, `FronEnd/src/components/lootera/PublicLayout.tsx`).
- Use `kebab-case.ts` for utility/hook/service helper files and generated-style UI modules (examples: `FronEnd/src/features/auth/utils/auth.validators.ts`, `FronEnd/src/hooks/use-toast.ts`, `FronEnd/src/components/ui/alert-dialog.tsx`).
- Use `*.types.ts` suffix for domain type definitions (examples: `FronEnd/src/features/products/types/product.types.ts`, `FronEnd/src/features/auth/types/auth.types.ts`).
- Use `*.service.ts` suffix for data/service modules (examples: `FronEnd/src/features/cart/services/cart.service.ts`, `FronEnd/src/features/orders/services/order.service.ts`).

**Functions:**
- Use `camelCase` for functions and methods (`normalizeEmail`, `validateLoginInput`, `getProductsByCategory` in `FronEnd/src/features/auth/utils/auth.validators.ts` and `FronEnd/src/features/products/services/product.service.ts`).
- Use hook naming with `use` prefix (`useCart` in `FronEnd/src/contexts/CartContext.tsx`, `useAuth` in `FronEnd/src/features/auth/context/AuthContext.tsx`, `useToast` in `FronEnd/src/hooks/use-toast.ts`).

**Variables:**
- Use `camelCase` for local variables and constants scoped to a module (`queryClient` in `FronEnd/src/App.tsx`, `defaultSettings` in `FronEnd/src/features/settings/services/settings.service.ts`).
- Use `UPPER_SNAKE_CASE` for global/static constants (`AUTH_MODE` in `FronEnd/src/features/auth/types/auth.types.ts`, `CART_STORAGE_KEY` in `FronEnd/src/features/cart/services/cart.service.ts`, `STORAGE_KEYS` in `FronEnd/src/features/auth/utils/auth.storage.ts`).

**Types:**
- Use `PascalCase` for `interface` and `type` names (`Product`, `AuthUser`, `AuthResult`, `CartContextType` in `FronEnd/src/features/products/types/product.types.ts`, `FronEnd/src/features/auth/types/auth.types.ts`, `FronEnd/src/contexts/CartContext.tsx`).
- Use `*Props` suffix for component prop contracts (`ProtectedRouteProps` in `FronEnd/src/routes/ProtectedRoute.tsx`, `ButtonProps` in `FronEnd/src/components/ui/button.tsx`).

## Code Style

**Formatting:**
- Tool detected: no dedicated Prettier config file (`.prettierrc*`) is present.
- Formatting style is consistent with 2-space indentation, semicolons, double quotes, and trailing commas in multiline objects/arrays (examples in `FronEnd/src/App.tsx`, `FronEnd/src/features/auth/context/AuthContext.tsx`, `FronEnd/src/components/ui/button.tsx`).
- Prefer concise arrow functions for components and service methods (`const Home = () => {}` in `FronEnd/src/pages/shop/Home.tsx`, methods in `FronEnd/src/features/products/services/product.service.ts`).

**Linting:**
- Tool: ESLint flat config with TypeScript integration in `FronEnd/eslint.config.js`.
- Use `@eslint/js` + `typescript-eslint` recommended presets (`FronEnd/eslint.config.js`).
- Enforce React hooks rules via `eslint-plugin-react-hooks` (`FronEnd/eslint.config.js`).
- Keep `react-refresh/only-export-components` warning enabled (`FronEnd/eslint.config.js`).
- Current rule override disables `@typescript-eslint/no-unused-vars`; keep this consistent unless lint policy is intentionally tightened (`FronEnd/eslint.config.js`).

## Import Organization

**Order:**
1. External libraries first (`react`, `react-router-dom`, `@tanstack/react-query`, `lucide-react`) as in `FronEnd/src/App.tsx` and `FronEnd/src/pages/shop/Home.tsx`.
2. Alias-based internal imports (`@/components/**`, `@/features/**`, `@/contexts/**`, `@/lib/**`) as in `FronEnd/src/App.tsx` and `FronEnd/src/components/ui/button.tsx`.
3. Relative imports last (`./pages/shop/Home`, `../types/auth.types`) as in `FronEnd/src/App.tsx` and `FronEnd/src/features/auth/services/mock-auth.service.ts`.

**Path Aliases:**
- Use `@/*` alias mapped to `./src/*` in `FronEnd/tsconfig.json`, `FronEnd/tsconfig.app.json`, and resolver aliases in `FronEnd/vite.config.ts` and `FronEnd/vitest.config.ts`.

## Error Handling

**Patterns:**
- Use guard throws for invalid provider/hook usage (`throw new Error(...)` in `FronEnd/src/contexts/CartContext.tsx` and `FronEnd/src/features/auth/context/AuthContext.tsx`).
- Use safe JSON parsing helpers with fallback return values instead of propagating parse exceptions (`safeParseJSON` in `FronEnd/src/features/cart/services/cart.service.ts`, `FronEnd/src/features/settings/services/settings.service.ts`, and `FronEnd/src/features/auth/utils/auth.storage.ts`).
- Use result-object pattern for expected auth failures rather than throwing (`AuthResult` in `FronEnd/src/features/auth/types/auth.types.ts`, returned by `FronEnd/src/features/auth/services/mock-auth.service.ts`).

## Logging

**Framework:** console

**Patterns:**
- Avoid routine console logging in feature code.
- Existing explicit log is for missing route telemetry in `FronEnd/src/pages/NotFound.tsx` (`console.error` in `useEffect`).

## Comments

**When to Comment:**
- Use section headers to group route declarations or page sections (examples: `// Shop pages` in `FronEnd/src/App.tsx`, `/* Hero */` in `FronEnd/src/pages/shop/Home.tsx`).
- Use short inline comments for mock/prototype caveats (`// MOCK ONLY - Never use in production` in `FronEnd/src/features/auth/types/auth.types.ts`, mock backend note in `FronEnd/src/features/orders/services/order.service.ts`).

**JSDoc/TSDoc:**
- Not used in analyzed source. Prefer typed signatures and clear names over doc blocks.

## Function Design

**Size:**
- Keep service functions compact and single-purpose (filter/find/transform methods in `FronEnd/src/features/products/services/product.service.ts` and `FronEnd/src/features/orders/services/order.service.ts`).
- Context providers can be larger but should centralize state transitions and side effects (`FronEnd/src/contexts/CartContext.tsx`, `FronEnd/src/features/auth/context/AuthContext.tsx`).

**Parameters:**
- Use typed object parameters for richer inputs (`login(input: LoginInput)` in `FronEnd/src/features/auth/services/mock-auth.service.ts`).
- Use primitive parameters for direct lookups (`getProductById(id: string)` in `FronEnd/src/features/products/services/product.service.ts`).

**Return Values:**
- Use explicit return types on public service methods (`(): Product[]`, `(): Cart`, `Promise<AuthResult<AuthUser>>`).
- Use union-based result contracts for recoverable domain errors (`AuthResult` in `FronEnd/src/features/auth/types/auth.types.ts`).

## Module Design

**Exports:**
- Use default exports for page/layout components (`export default` in `FronEnd/src/pages/**` and `FronEnd/src/components/lootera/**`).
- Use named exports for services, hooks, contexts, and reusable utilities (`productService`, `useCart`, `AuthProvider`, `cn` in `FronEnd/src/features/**`, `FronEnd/src/contexts/CartContext.tsx`, `FronEnd/src/lib/utils.ts`).

**Barrel Files:**
- Not detected. Import directly from concrete module paths (examples in `FronEnd/src/App.tsx` and `FronEnd/src/pages/shop/Home.tsx`).

---

*Convention analysis: 2026-04-18*
