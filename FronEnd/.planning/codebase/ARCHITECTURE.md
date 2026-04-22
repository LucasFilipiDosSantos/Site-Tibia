# Architecture

## Pattern
React SPA with client-side routing and a hybrid state model:
- TanStack Query for backend-backed catalog reads
- React Context for auth session and local cart

## Layers
- **Pages** (`src/pages/`) - Route components
- **Components** (`src/components/`) - UI components (ui/*, lootera/*)
- **Features** (`src/features/`) - Business logic, services, hooks, types
- **Contexts** (`src/contexts/`) - Global state wrappers
- **Lib** (`src/lib/`) - Shared API client utilities

## Data Flow
1. Pages call feature hooks (`useProducts`, `useProduct`)
2. Hooks call services
3. Services use shared API client to reach backend endpoints
4. Auth context restores session and injects bearer tokens for protected requests

## State Management
- Auth: React Context backed by JWT session storage + refresh flow
- Cart: React Context with local persistence
- Products: TanStack Query

## Current Boundary Mismatch
- Public catalog is slug-based
- Checkout/cart endpoints are product-ID based
- This mismatch blocks full cart/checkout backend integration
