# Architecture

## Pattern
React SPA with client-side routing.

## Layers
- **Pages** (`src/pages/`) - Route components
- **Components** (`src/components/`) - UI components (ui/*, lootera/*)
- **Features** (`src/features/`) - Business logic (services, types, context)
- **Contexts** (`src/contexts/`) - Global state

## Data Flow
1. Pages import from features/services
2. Features use mock data (no API)
3. TanStack Query ready for caching

## State Management
- Auth: React Context (`AuthContext`)
- Cart: React Context (`CartContext`)
- Products/Orders: TanStack Query (not yet wired to API)