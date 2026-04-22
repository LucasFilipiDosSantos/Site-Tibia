# Lootera Marketplace

React + Vite frontend for the Tibia marketplace.

## Current status

- Auth is wired to the backend `/auth/*` endpoints
- Public catalog is wired to `/products` and `/products/{slug}`
- Cart, orders, and admin flows still need additional backend contract work

## Environment

Create a local `.env` with:

```bash
VITE_API_BASE_URL=http://localhost:8080
```

For Hostinger/static production deploy, edit `.env.production` before building:

```bash
VITE_API_BASE_URL=https://api.seudominio.com.br
VITE_APP_BASE_PATH=/
```

If the frontend is deployed inside a folder instead of the domain root, set `VITE_APP_BASE_PATH=/nome-da-pasta/` and adjust `public/.htaccess` `RewriteBase` to the same folder.

## Hostinger deploy

```bash
npm install
npm run build
```

Upload the contents of `dist/` to `public_html`. The SPA rewrite file is already in `public/.htaccess`, so Vite copies it into `dist/.htaccess` during build.

If the backend runs on another host, also set the frontend domain in `backend/src/API/appsettings.json` under `Cors:AllowedOrigins`, or override it in production with:

```bash
Cors__AllowedOrigins__0=https://seudominio.com.br
Cors__AllowedOrigins__1=https://www.seudominio.com.br
```
