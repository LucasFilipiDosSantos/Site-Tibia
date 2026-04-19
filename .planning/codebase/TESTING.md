# Testing Patterns

**Analysis Date:** 2026-04-18

## Test Framework

**Runner:**
- Vitest `^3.2.4` (declared in `FronEnd/package.json`).
- Config: `FronEnd/vitest.config.ts`.

**Assertion Library:**
- Vitest built-in `expect` API (`FronEnd/src/test/example.test.ts`).
- DOM matchers from `@testing-library/jest-dom` loaded in `FronEnd/src/test/setup.ts`.

**Run Commands:**
```bash
npm run test              # Run all tests (single run)
npm run test:watch        # Watch mode
Not configured            # Coverage command not present in scripts
```

## Test File Organization

**Location:**
- Co-located inside `src` under `src/test` currently (`FronEnd/src/test/example.test.ts`, `FronEnd/src/test/setup.ts`).
- Discovery pattern allows co-located tests anywhere in `src`: `src/**/*.{test,spec}.{ts,tsx}` (`FronEnd/vitest.config.ts`).

**Naming:**
- Use `*.test.ts` and/or `*.spec.ts(x)` naming; both are included by config (`FronEnd/vitest.config.ts`).

**Structure:**
```
FronEnd/src/
└── **/*.{test,spec}.{ts,tsx}
```

## Test Structure

**Suite Organization:**
```typescript
import { describe, it, expect } from "vitest";

describe("example", () => {
  it("should pass", () => {
    expect(true).toBe(true);
  });
});
```
Pattern sourced from `FronEnd/src/test/example.test.ts`.

**Patterns:**
- Setup pattern: global setup file configures browser API shims before tests (`FronEnd/src/test/setup.ts`).
- Teardown pattern: Not detected in current test suite.
- Assertion pattern: direct matcher assertions (`expect(...).toBe(...)`) in `FronEnd/src/test/example.test.ts`.

## Mocking

**Framework:**
- Vitest mocking APIs are available but not currently used in committed tests (no `vi.mock`, `vi.spyOn` matches under `FronEnd/src/**/*.{test,spec}.{ts,tsx}`).

**Patterns:**
```typescript
Object.defineProperty(window, "matchMedia", {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => {},
  }),
});
```
Pattern sourced from `FronEnd/src/test/setup.ts`.

**What to Mock:**
- Browser APIs not provided by jsdom (example already implemented: `window.matchMedia` in `FronEnd/src/test/setup.ts`).
- External integration boundaries when introduced (network/storage adapters in service modules such as `FronEnd/src/features/**/services/*.service.ts`).

**What NOT to Mock:**
- Pure utility functions and deterministic validators (`FronEnd/src/features/auth/utils/auth.validators.ts`, `FronEnd/src/lib/utils.ts`).
- Type-only modules (`FronEnd/src/features/**/types/*.types.ts`).

## Fixtures and Factories

**Test Data:**
```typescript
const featured = productService.getFeaturedProducts();
```
Current codebase relies on static mock datasets from `FronEnd/src/data/mockData.ts` used by services (for example `FronEnd/src/features/products/services/product.service.ts` and `FronEnd/src/features/orders/services/order.service.ts`).

**Location:**
- Shared mock data lives in `FronEnd/src/data/mockData.ts`.
- Dedicated test fixture/factory directories are not detected.

## Coverage

**Requirements:** None enforced (no thresholds/config detected).

**View Coverage:**
```bash
Not configured in current npm scripts
```

## Test Types

**Unit Tests:**
- Framework configured, but only a minimal smoke-style unit test exists in `FronEnd/src/test/example.test.ts`.

**Integration Tests:**
- Not detected in Vitest suite (`FronEnd/src/**/*.{test,spec}.{ts,tsx}`).

**E2E Tests:**
- Playwright is installed and configured in `FronEnd/playwright.config.ts` with fixture re-export at `FronEnd/playwright-fixture.ts`.
- No Playwright spec files detected (`**/*.spec.ts` outside Vitest test file set not present).

## Common Patterns

**Async Testing:**
```typescript
// Preferred with existing service contracts in `FronEnd/src/features/auth/services/mock-auth.service.ts`
const result = await mockAuthService.login({ email, password });
expect(result.success).toBe(true);
```

**Error Testing:**
```typescript
// Preferred for provider guard hooks in `FronEnd/src/contexts/CartContext.tsx`
expect(() => useCart()).toThrow("useCart must be used within CartProvider");
```

---

*Testing analysis: 2026-04-18*
