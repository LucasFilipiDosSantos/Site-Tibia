# Swagger Endpoint Organization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Organize Swagger UI endpoints by audience (`Public Catalog`, `Auth`, `Admin Catalog`, `Health/Probes`) without changing route behavior or API contracts.

**Architecture:** Keep endpoint organization explicit at endpoint declaration time using Minimal API `.WithTags(...)` metadata. Validate behavior through an integration test that reads `/swagger/v1/swagger.json` and asserts each route+verb has the expected single tag. This keeps Swagger behavior transparent and prevents future regressions when endpoints are added.

**Tech Stack:** ASP.NET Core Minimal APIs (.NET 10), Swashbuckle/OpenAPI JSON, xUnit integration tests with `WebApplicationFactory<Program>`.

---

## File Structure

- Create: `tests/IntegrationTests/API/SwaggerEndpointTagGroupingTests.cs`
  - Responsibility: OpenAPI contract regression test for endpoint tags by route+method.
- Modify: `src/API/Auth/AuthEndpoints.cs`
  - Responsibility: Assign `Auth` and `Health/Probes` tags to auth/probe endpoints.
- Modify: `src/API/Catalog/CatalogEndpoints.cs`
  - Responsibility: Assign `Public Catalog` and `Admin Catalog` tags to catalog endpoints.

### Task 1: Add failing Swagger tag contract test

**Files:**
- Create: `tests/IntegrationTests/API/SwaggerEndpointTagGroupingTests.cs`
- Test: `tests/IntegrationTests/API/SwaggerEndpointTagGroupingTests.cs`

- [ ] **Step 1: Create the failing integration test file**

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace IntegrationTests.API;

public sealed class SwaggerEndpointTagGroupingTests
{
    [Theory]
    [InlineData("/products", "get", "Public Catalog")]
    [InlineData("/products/{slug}", "get", "Public Catalog")]
    [InlineData("/auth/register", "post", "Auth")]
    [InlineData("/auth/login", "post", "Auth")]
    [InlineData("/auth/refresh", "post", "Auth")]
    [InlineData("/auth/verify-email/request", "post", "Auth")]
    [InlineData("/auth/verify-email/confirm", "post", "Auth")]
    [InlineData("/auth/password-reset/request", "post", "Auth")]
    [InlineData("/auth/password-reset/confirm", "post", "Auth")]
    [InlineData("/auth/admin/probe", "get", "Health/Probes")]
    [InlineData("/auth/verified/probe", "get", "Health/Probes")]
    [InlineData("/admin/catalog/categories", "post", "Admin Catalog")]
    [InlineData("/admin/catalog/categories/{slug}", "delete", "Admin Catalog")]
    [InlineData("/admin/catalog/products", "post", "Admin Catalog")]
    [InlineData("/admin/catalog/products/{slug}", "put", "Admin Catalog")]
    public async Task SwaggerV1_UsesAudienceTags(string route, string method, string expectedTag)
    {
        await using var factory = new SwaggerApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var tags = GetOperationTags(payload, route, method);

        var actualTag = Assert.Single(tags);
        Assert.Equal(expectedTag, actualTag);
    }

    private static IReadOnlyList<string> GetOperationTags(JsonElement swagger, string route, string method)
    {
        var paths = swagger.GetProperty("paths");
        var pathItem = paths.GetProperty(route);
        var operation = pathItem.GetProperty(method);
        var tags = operation.GetProperty("tags").EnumerateArray().Select(x => x.GetString()!).ToList();
        return tags;
    }

    private sealed class SwaggerApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(
                [
                    new KeyValuePair<string, string?>("Jwt:Issuer", "tibia-webstore"),
                    new KeyValuePair<string, string?>("Jwt:Audience", "tibia-webstore-client"),
                    new KeyValuePair<string, string?>("Jwt:SigningKey", "01234567890123456789012345678901"),
                    new KeyValuePair<string, string?>("IdentityTokenDelivery:Provider", "inmemory")
                ]);
            });
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
        }
    }
}
```

- [ ] **Step 2: Run targeted test and confirm it fails before implementation**

Run: `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter FullyQualifiedName~SwaggerEndpointTagGroupingTests.SwaggerV1_UsesAudienceTags`

Expected: FAIL because current Swagger tags are not yet the four audience tags.

- [ ] **Step 3: Commit test-first checkpoint**

```bash
git add tests/IntegrationTests/API/SwaggerEndpointTagGroupingTests.cs
git commit -m "test(api): add failing swagger tag grouping contract"
```

Expected: one commit containing only the new failing integration test.

### Task 2: Tag auth and probe endpoints explicitly

**Files:**
- Modify: `src/API/Auth/AuthEndpoints.cs`
- Test: `tests/IntegrationTests/API/SwaggerEndpointTagGroupingTests.cs`

- [ ] **Step 1: Add explicit tags to each auth endpoint**

Apply these exact endpoint chains in `MapAuthEndpoints`:

```csharp
group.MapPost("/register", async (RegisterRequest request, IIdentityService identityService, CancellationToken ct) =>
{
    await identityService.RegisterAsync(new RegisterCommand(request.Email, request.Password), ct);
    return Results.Ok(new { message = "Registration successful." });
})
.WithTags("Auth");

group.MapPost("/login", async (HttpContext context, LoginRequest request, IIdentityService identityService, CancellationToken ct) =>
{
    context.Request.Headers["X-Auth-Identifier"] = request.Email;
    var ip = context.Connection.RemoteIpAddress?.ToString();
    var result = await identityService.LoginAsync(new LoginCommand(request.Email, request.Password, ip), ct);
    return Results.Ok(new AuthResponse(result.AccessToken, result.RefreshToken, result.AccessTokenExpiresAtUtc, result.RefreshTokenExpiresAtUtc));
})
.WithTags("Auth");

group.MapPost("/refresh", async (HttpContext context, RefreshRequest request, TokenRotationService rotationService, CancellationToken ct) =>
{
    var ip = context.Connection.RemoteIpAddress?.ToString();
    var result = await rotationService.RotateAsync(request.RefreshToken, ip, ct);
    return Results.Ok(new AuthResponse(result.AccessToken, result.RefreshToken, result.AccessTokenExpiresAtUtc, result.RefreshTokenExpiresAtUtc));
})
.WithTags("Auth");

group.MapPost("/verify-email/request", async (VerificationRequest request, IIdentityService identityService, CancellationToken ct) =>
{
    await identityService.RequestEmailVerificationAsync(request.Email, ct);
    return Results.Ok(new { message = "If the account exists, a verification link was sent." });
})
.WithTags("Auth");

group.MapPost("/verify-email/confirm", async (VerificationConfirmRequest request, IIdentityService identityService, CancellationToken ct) =>
{
    var ok = await identityService.ConfirmEmailVerificationAsync(request.Token, ct);
    return ok ? Results.Ok(new { message = "Email verified." }) : Results.BadRequest(new { message = "Invalid or expired token." });
})
.WithTags("Auth");

group.MapPost("/password-reset/request", async (HttpContext context, PasswordResetRequest request, IIdentityService identityService, CancellationToken ct) =>
{
    context.Request.Headers["X-Auth-Identifier"] = request.Email;
    await identityService.RequestPasswordResetAsync(request.Email, ct);
    return Results.Ok(new { message = "If the account exists, a reset link was sent." });
})
.WithTags("Auth");

group.MapPost("/password-reset/confirm", async (PasswordResetConfirmRequest request, IIdentityService identityService, CancellationToken ct) =>
{
    var ok = await identityService.ConfirmPasswordResetAsync(request.Token, request.NewPassword, ct);
    return ok ? Results.Ok(new { message = "Password reset completed." }) : Results.BadRequest(new { message = "Invalid, consumed, or expired token." });
})
.WithTags("Auth");

group.MapGet("/admin/probe", () => Results.Ok(new { ok = true }))
    .RequireAuthorization(AuthPolicies.AdminOnly)
    .WithTags("Health/Probes");

group.MapGet("/verified/probe", () => Results.Ok(new { ok = true }))
    .RequireAuthorization(AuthPolicies.VerifiedForSensitiveActions)
    .WithTags("Health/Probes");
```

- [ ] **Step 2: Run the targeted Swagger grouping test**

Run: `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter FullyQualifiedName~SwaggerEndpointTagGroupingTests.SwaggerV1_UsesAudienceTags`

Expected: still FAIL (catalog endpoints still need tags), but auth/probe cases should now pass if run individually.

- [ ] **Step 3: Commit auth/probe tagging changes**

```bash
git add src/API/Auth/AuthEndpoints.cs
git commit -m "feat(api): tag auth and probe swagger endpoints"
```

Expected: one commit containing only `AuthEndpoints.cs` tag updates.

### Task 3: Tag catalog endpoints explicitly

**Files:**
- Modify: `src/API/Catalog/CatalogEndpoints.cs`
- Test: `tests/IntegrationTests/API/SwaggerEndpointTagGroupingTests.cs`

- [ ] **Step 1: Add public catalog tags to customer endpoints**

Apply this exact chaining for customer routes:

```csharp
app.MapGet("/products", async (
    [AsParameters] ProductListQueryRequest query,
    CatalogService catalogService,
    CancellationToken ct) =>
{
    var request = new Application.Catalog.Contracts.ListProductsRequest(
        Page: query.Page,
        PageSize: query.PageSize,
        Category: query.Category,
        Slug: query.Slug
    );

    var result = await catalogService.ListProducts(request, ct);
    var hasPreviousPage = result.Page > 1;
    var hasNextPage = result.Items.Count == result.PageSize;

    return Results.Ok(new ProductListResponse(
        result.Items.Select(x => new ProductListItemResponse(x.Name, x.Slug, x.Description, x.Price, x.CategorySlug)).ToList(),
        result.Page,
        result.PageSize,
        new ProductListAppliedFiltersResponse(query.Category, query.Slug),
        new ProductListPaginationResponse(result.Page, result.PageSize, hasPreviousPage, hasNextPage)));
})
.WithTags("Public Catalog");

app.MapGet("/products/{slug}", async (string slug, CatalogService catalogService, CancellationToken ct) =>
{
    var product = await catalogService.GetBySlug(slug, ct);
    if (product is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new ProductResponse(product.Name, product.Slug, product.Description, product.Price, product.CategorySlug));
})
.WithTags("Public Catalog");
```

- [ ] **Step 2: Add admin catalog tags to admin endpoints**

Apply this exact chaining for admin routes:

```csharp
admin.MapPost("/categories", async (CreateCategoryRequest request, CatalogService catalogService, CancellationToken ct) =>
{
    await catalogService.CreateCategory(new Application.Catalog.Contracts.CreateCategoryRequest(request.Name, request.Slug, request.Description), ct);
    return Results.Ok();
})
.WithTags("Admin Catalog");

admin.MapDelete("/categories/{slug}", async (string slug, CatalogService catalogService, CancellationToken ct) =>
{
    await catalogService.DeleteCategory(slug, ct);
    return Results.Ok();
})
.WithTags("Admin Catalog");

admin.MapPost("/products", async (CreateProductRequest request, CatalogService catalogService, CancellationToken ct) =>
{
    var created = await catalogService.CreateProduct(
        new Application.Catalog.Contracts.CreateProductRequest(
            request.Name,
            request.Slug,
            request.Description,
            request.Price,
            request.CategorySlug),
        ct);

    return Results.Ok(new ProductResponse(created.Name, created.Slug, created.Description, created.Price, created.CategorySlug));
})
.WithTags("Admin Catalog");

admin.MapPut("/products/{slug}", async (string slug, UpdateProductPutReplaceRequest request, CatalogService catalogService, CancellationToken ct) =>
{
    var updated = await catalogService.UpdateProductPutReplace(
        new Application.Catalog.Contracts.UpdateProductPutReplaceRequest(
            RouteSlug: slug,
            PayloadSlug: request.Slug,
            Name: request.Name,
            Description: request.Description,
            Price: request.Price,
            CategorySlug: request.CategorySlug),
        ct);

    return Results.Ok(new ProductResponse(updated.Name, updated.Slug, updated.Description, updated.Price, updated.CategorySlug));
})
.WithTags("Admin Catalog");
```

- [ ] **Step 3: Run targeted Swagger grouping test and verify pass**

Run: `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter FullyQualifiedName~SwaggerEndpointTagGroupingTests.SwaggerV1_UsesAudienceTags`

Expected: PASS for all 15 route+method+tag combinations.

- [ ] **Step 4: Commit catalog tagging changes**

```bash
git add src/API/Catalog/CatalogEndpoints.cs
git commit -m "feat(api): tag catalog swagger endpoints by audience"
```

Expected: one commit containing only catalog endpoint tag changes.

### Task 4: Final verification and documentation sanity check

**Files:**
- Modify: none expected
- Test: `tests/IntegrationTests/API/SwaggerEndpointTagGroupingTests.cs`

- [ ] **Step 1: Run full integration test project**

Run: `dotnet test tests/IntegrationTests/IntegrationTests.csproj`

Expected: PASS (including new Swagger grouping contract test).

- [ ] **Step 2: Run full build**

Run: `dotnet build`

Expected: PASS with zero compile errors.

- [ ] **Step 3: Manual Swagger UI smoke check**

Run API in Development and open `/swagger`, then verify sections display:
- `Public Catalog`
- `Auth`
- `Admin Catalog`
- `Health/Probes`

Expected: endpoints appear grouped exactly by those four tags.

- [ ] **Step 4: Commit verification-complete state**

```bash
git status
```

Expected: clean working tree, or only intended uncommitted files if commit batching is deferred.
