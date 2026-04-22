# Stack

## Technologies
- **Runtime**: React 18.3.1 + TypeScript 5.8.3
- **Build**: Vite 5.4.x
- **Routing**: React Router DOM 6.30.1
- **State/Query**: TanStack Query 5.83.0
- **UI Components**: Radix UI (shadcn/ui pattern)
- **Styling**: Tailwind CSS 3.4.17 + tailwind-merge + clsx
- **Forms**: React Hook Form 7.61.1 + Zod 3.25.76
- **Icons**: Lucide React 0.462.0

## Project Structure
- `src/pages/` - Route pages (shop, auth, user, admin)
- `src/components/ui/` - shadcn/ui components
- `src/components/lootera/` - Custom components
- `src/features/` - Feature folders (auth, products, orders, users, cart, settings)
- `src/contexts/` - React contexts
- `src/hooks/` - Custom hooks
- `src/lib/` - Shared utilities and API client

## Integrations
- Real backend integration for auth and catalog
- Remaining admin, order, and payment flows still use local/mock assumptions

## Testing
- Vitest for unit tests
- Playwright for e2e tests
