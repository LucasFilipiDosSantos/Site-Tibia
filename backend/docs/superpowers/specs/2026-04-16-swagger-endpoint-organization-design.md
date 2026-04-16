# Swagger Endpoint Organization Design

## Context

Swagger UI currently lists endpoints in a flat, mixed order, which makes it harder to quickly find customer-facing vs admin operations.

Goal: reorganize Swagger endpoint presentation by audience without changing API behavior.

## Scope

In scope:
- Swagger tag organization for existing Minimal API endpoints.
- Consistent grouping in Swagger UI under four audience-based sections.

Out of scope:
- Route path changes.
- Auth policy or middleware changes.
- Request/response contract changes.
- New endpoints.

## Target Organization

Use these Swagger tags:
- `Public Catalog`
- `Auth`
- `Admin Catalog`
- `Health/Probes`

Endpoint-to-tag mapping:
- `GET /products` -> `Public Catalog`
- `GET /products/{slug}` -> `Public Catalog`
- `POST /auth/register` -> `Auth`
- `POST /auth/login` -> `Auth`
- `POST /auth/refresh` -> `Auth`
- `POST /auth/verify-email/request` -> `Auth`
- `POST /auth/verify-email/confirm` -> `Auth`
- `POST /auth/password-reset/request` -> `Auth`
- `POST /auth/password-reset/confirm` -> `Auth`
- `GET /auth/admin/probe` -> `Health/Probes`
- `GET /auth/verified/probe` -> `Health/Probes`
- `POST /admin/catalog/categories` -> `Admin Catalog`
- `DELETE /admin/catalog/categories/{slug}` -> `Admin Catalog`
- `POST /admin/catalog/products` -> `Admin Catalog`
- `PUT /admin/catalog/products/{slug}` -> `Admin Catalog`

## Design Approach

Recommended approach: explicit per-endpoint tagging with `.WithTags(...)` in existing endpoint mapping files.

Rationale:
- Clear and explicit in Minimal API definitions.
- Easy to maintain as endpoints evolve.
- Avoids brittle route-inference or custom Swagger operation filters.

Implementation detail:
- Update `src/API/Auth/AuthEndpoints.cs` to apply `Auth` and `Health/Probes` tags.
- Update `src/API/Catalog/CatalogEndpoints.cs` to apply `Public Catalog` and `Admin Catalog` tags.
- Keep `src/API/Program.cs` Swagger setup simple; no custom grouping filters required.

## Data Flow and Runtime Impact

No domain/application/infrastructure flow changes.

This change only affects OpenAPI metadata consumed by Swagger UI.
- HTTP pipeline remains unchanged.
- Endpoint handlers remain unchanged.
- Authorization behavior remains unchanged.

## Error Handling

Potential issue:
- Endpoint assigned to wrong tag.

Mitigation:
- Verify each endpoint appears under the expected section in Swagger UI.
- Keep a direct endpoint-to-tag checklist during review.

## Testing and Verification

Verification steps:
1. `dotnet build`
2. Run API in development and open `/swagger`.
3. Confirm all endpoints are grouped under:
   - `Public Catalog`
   - `Auth`
   - `Admin Catalog`
   - `Health/Probes`
4. Confirm no endpoint path, request schema, or response schema changed.

## Alternatives Considered

1. Swagger convention-based route inference (`TagActionsBy`/operation filters)
   - Rejected: adds hidden logic and fragility.

2. Mixed group-level tagging with selective overrides
   - Rejected: lower clarity than explicit tagging for this small endpoint set.

## Success Criteria

- Swagger UI displays endpoints in the four agreed audience-based groups.
- Developers can identify public/auth/admin/probe operations at a glance.
- No functional API behavior changes.
