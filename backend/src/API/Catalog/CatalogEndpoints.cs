using API.Auth;
using Application.Catalog.Services;

namespace API.Catalog;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
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
        });

        app.MapGet("/products/{slug}", async (string slug, CatalogService catalogService, CancellationToken ct) =>
        {
            var product = await catalogService.GetBySlug(slug, ct);
            if (product is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new ProductResponse(product.Name, product.Slug, product.Description, product.Price, product.CategorySlug));
        });

        var admin = app.MapGroup("/admin/catalog")
            .RequireAuthorization(AuthPolicies.AdminOnly);

        admin.MapPost("/categories", async (CreateCategoryRequest request, CatalogService catalogService, CancellationToken ct) =>
        {
            await catalogService.CreateCategory(new Application.Catalog.Contracts.CreateCategoryRequest(request.Name, request.Slug, request.Description), ct);
            return Results.Ok();
        });

        admin.MapDelete("/categories/{slug}", async (string slug, CatalogService catalogService, CancellationToken ct) =>
        {
            await catalogService.DeleteCategory(slug, ct);
            return Results.Ok();
        });

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
        });

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
        });

        return app;
    }
}
