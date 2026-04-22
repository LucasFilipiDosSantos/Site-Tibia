# Concerns

## Critical Gaps
1. **Public catalog contract is insufficient for checkout** - it lacks product GUIDs required by `/checkout/cart/*`
2. **Server filtering cannot be implemented honestly yet** - catalog responses do not expose server data
3. **Stock cannot be displayed in real time** - public catalog responses do not expose stock data
4. **Password reset and email verification UI are still missing** - backend endpoints exist, frontend screens do not

## Quality Issues
- Several admin and order pages still depend on mock data
- Cart is still local-only and not synchronized with backend
- Loading/error UX was improved on catalog routes, but not audited app-wide

## Next Steps
- Extend backend catalog contract with `productId`, `server`, and `stock`
- Wire checkout/cart pages to backend once product identity is available
- Add auth recovery flows (verify email, reset password)
- Replace remaining admin/order mocks with API-backed services
