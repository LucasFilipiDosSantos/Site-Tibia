# Structure

```
src/
├── pages/
│   ├── shop/        # Home, Products, ProductDetail, Cart, Checkout
│   ├── auth/       # Login, Register
│   ├── user/       # Profile, OrderHistory
│   ├── admin/      # Dashboard, AdminProducts, AdminOrders, AdminUsers, AdminInventory, AdminSettings
│   └── NotFound.tsx
├── components/
│   ├── ui/         # shadcn/ui components (30+ files)
│   └── lootera/     # Custom components (PublicLayout, ProductCard, Footer, etc.)
├── features/
│   ├── auth/       # context, services, types, utils
│   ├── products/
│   ├── orders/
│   ├── users/
│   ├── cart/
│   └── settings/
├── contexts/       # CartContext
├── hooks/          # use-mobile
├── lib/           # utils (cn helper)
├── data/          # mockData
└── routes/        # ProtectedRoute, AdminRoute
```

## Key Files
- `src/App.tsx` - Route definitions
- `src/main.tsx` - Entry point
- `src/contexts/CartContext.tsx` - Cart state
- `src/features/auth/context/AuthContext.tsx` - Auth state