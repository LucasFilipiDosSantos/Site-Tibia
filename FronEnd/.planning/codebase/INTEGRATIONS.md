# Integrations

## External APIs
- Backend auth API: `/auth/register`, `/auth/login`, `/auth/refresh`
- Backend catalog API: `/products`, `/products/{slug}`

## Auth
- JWT access token + refresh token
- Session persisted in localStorage
- Automatic refresh retry on protected API 401 responses

## API Readiness
- Shared `apiRequest` client added
- `VITE_API_BASE_URL` is now the frontend entry point for backend connectivity
- Protected checkout/order/admin integrations still require additional backend/frontend contract alignment
