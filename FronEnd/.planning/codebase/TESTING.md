# Testing

## Framework
- Vitest for unit tests
- Playwright for e2e tests
- React Testing Library

## Test Structure
- `src/test/` - baseline test setup
- `src/features/auth/utils/*.test.ts` - auth/session utility tests
- `src/features/products/utils/*.test.ts` - catalog utility tests

## Coverage
- Utility coverage exists for JWT decoding and category mapping
- No component/integration tests yet for API-backed catalog and auth pages

## Validation Notes
- Local verification requires `vite`/`vitest` execution outside the current sandbox restrictions
