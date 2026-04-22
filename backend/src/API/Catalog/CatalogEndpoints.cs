using API.Auth;
using Application.Catalog.Services;

namespace API.Catalog;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/products", async (
            string? category,
            string? slug,
            int? page,
            int? pageSize,
            CatalogService catalogService,
            CancellationToken ct) =>
        {
            var request = new Application.Catalog.Contracts.ListProductsRequest(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                Category: category,
                Slug: slug
            );

            var result = await catalogService.ListProducts(request, ct);
            var hasPreviousPage = result.Page > 1;
            var hasNextPage = result.Items.Count == result.PageSize;

            return Results.Ok(new ProductListResponse(
                result.Items.Select(x => new ProductListItemResponse(
                    x.Id,
                    x.Name,
                    x.Slug,
                    x.Description,
                    x.Price,
                    x.CategorySlug,
                    x.ImageUrl,
                    x.Server,
                    x.AvailableStock,
                    x.Rating,
                    x.SalesCount)).ToList(),
                result.Page,
                result.PageSize,
                new ProductListAppliedFiltersResponse(category, slug),
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

            return Results.Ok(new ProductResponse(
                product.Id,
                product.Name,
                product.Slug,
                product.Description,
                product.Price,
                product.CategorySlug,
                product.ImageUrl,
                product.Server,
                product.AvailableStock,
                product.Rating,
                product.SalesCount));
        })
        .WithTags("Public Catalog");

        var admin = app.MapGroup("/admin/catalog")
            .RequireAuthorization(AuthPolicies.AdminOnly);

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
                    request.CategorySlug,
                    request.ImageUrl),
                ct);

            return Results.Ok(new ProductResponse(
                created.Id,
                created.Name,
                created.Slug,
                created.Description,
                created.Price,
                created.CategorySlug,
                created.ImageUrl,
                created.Server,
                created.AvailableStock,
                created.Rating,
                created.SalesCount));
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
                    CategorySlug: request.CategorySlug,
                    ImageUrl: request.ImageUrl),
                ct);

            return Results.Ok(new ProductResponse(
                updated.Id,
                updated.Name,
                updated.Slug,
                updated.Description,
                updated.Price,
                updated.CategorySlug,
                updated.ImageUrl,
                updated.Server,
                updated.AvailableStock,
                updated.Rating,
                updated.SalesCount));
        })
        .WithTags("Admin Catalog");

        admin.MapDelete("/products/{slug}", async (string slug, CatalogService catalogService, CancellationToken ct) =>
        {
            await catalogService.DeleteProduct(slug, ct);
            return Results.NoContent();
        })
        .WithTags("Admin Catalog");

        return app;
    }
}
