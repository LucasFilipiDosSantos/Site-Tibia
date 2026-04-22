# Structure

```
src/
|-- pages/
|   |-- shop/        # Home, Products, ProductDetail, Cart, Checkout
|   |-- auth/        # Login, Register
|   |-- user/        # Profile, OrderHistory
|   `-- admin/       # Dashboard, products, orders, users, inventory, settings
|-- components/
|   |-- ui/          # shadcn/ui components
|   `-- lootera/     # App-specific layout/navigation components
|-- features/
|   |-- auth/        # context, services, utils, tests
|   |-- products/    # hooks, services, types, utils, tests
|   |-- orders/
|   |-- users/
|   |-- cart/
|   `-- settings/
|-- contexts/        # CartContext
|-- hooks/
|-- lib/             # shared API client
|-- data/            # legacy mock data still used by some admin/order screens
|-- routes/          # ProtectedRoute, AdminRoute
`-- test/            # Vitest setup
```

## Key Files
- `src/App.tsx` - route definitions and QueryClient wiring
- `src/lib/api.ts` - shared fetch client with auth retry
- `src/features/auth/context/AuthContext.tsx` - auth session orchestration
- `src/features/products/hooks/useProducts.ts` - catalog query hooks
- `src/contexts/CartContext.tsx` - local cart state
