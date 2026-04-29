using API.Auth;
using Application.Catalog.Services;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace API.Catalog;

public static class CatalogEndpoints
{
    private const long MaxProductImageBytes = 5 * 1024 * 1024;
    private static readonly Dictionary<string, string[]> AllowedProductImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = ["image/png"],
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".webp"] = ["image/webp"]
    };
    private static readonly Dictionary<string, string> ProductImageExtensionByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp"
    };

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/categories", async (AppDbContext dbContext, CancellationToken ct) =>
        {
            var categories = await dbContext.Categories
                .AsNoTracking()
                .OrderBy(category => category.Name)
                .Select(category => new CategoryResponse(
                    category.Id,
                    category.Name,
                    category.Slug,
                    category.Description))
                .ToListAsync(ct);

            return Results.Ok(categories);
        })
        .WithTags("Public Catalog");

        app.MapGet("/products", async (
            string? category,
            string? slug,
            int? page,
            int? pageSize,
            CatalogService catalogService,
            IMemoryCache cache,
            CancellationToken ct) =>
        {
            var request = new Application.Catalog.Contracts.ListProductsRequest(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                Category: category,
                Slug: slug
            );

            var cacheKey = $"catalog:products:{request.Page}:{request.PageSize}:{request.Category?.Trim().ToLowerInvariant()}:{request.Slug?.Trim().ToLowerInvariant()}";
            var response = await cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                entry.SlidingExpiration = TimeSpan.FromSeconds(10);

                var result = await catalogService.ListProducts(request, ct);
                var hasPreviousPage = result.Page > 1;
                var hasNextPage = result.Items.Count == result.PageSize;

                return new ProductListResponse(
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
                    x.ReviewCount,
                    x.SalesCount)).ToList(),
                    result.Page,
                    result.PageSize,
                    new ProductListAppliedFiltersResponse(category, slug),
                    new ProductListPaginationResponse(result.Page, result.PageSize, hasPreviousPage, hasNextPage));
            });

            return Results.Ok(response);
        })
        .WithTags("Public Catalog");

        app.MapGet("/products/{slug}", async (string slug, CatalogService catalogService, IMemoryCache cache, CancellationToken ct) =>
        {
            var cacheKey = $"catalog:product:{slug.Trim().ToLowerInvariant()}";
            var product = await cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                entry.SlidingExpiration = TimeSpan.FromSeconds(10);
                return await catalogService.GetBySlug(slug, ct);
            });
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
                product.ReviewCount,
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
                    request.Server,
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
                created.ReviewCount,
                created.SalesCount));
        })
        .WithTags("Admin Catalog");

        admin.MapPost("/products/form", async (HttpContext context, CatalogService catalogService, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var form = await context.Request.ReadFormAsync(ct);
            var imageUrl = await ResolveProductImageUrlAsync(form, env, null, ct);
            var created = await catalogService.CreateProduct(
                new Application.Catalog.Contracts.CreateProductRequest(
                    RequiredFormValue(form, "name"),
                    RequiredFormValue(form, "slug"),
                    RequiredFormValue(form, "description"),
                    decimal.Parse(RequiredFormValue(form, "price"), System.Globalization.CultureInfo.InvariantCulture),
                    RequiredFormValue(form, "categorySlug"),
                    OptionalFormValue(form, "server"),
                    imageUrl),
                ct);

            return Results.Ok(ToProductResponse(created));
        })
        .Accepts<IFormFile>("multipart/form-data")
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
                    Server: request.Server,
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
                updated.ReviewCount,
                updated.SalesCount));
        })
        .WithTags("Admin Catalog");

        admin.MapPut("/products/{slug}/form", async (string slug, HttpContext context, CatalogService catalogService, IWebHostEnvironment env, CancellationToken ct) =>
        {
            return await UpdateProductFromFormAsync(slug, context, catalogService, env, ct);
        })
        .Accepts<IFormFile>("multipart/form-data")
        .WithTags("Admin Catalog");

        admin.MapPut("/products/form/{slug}", async (string slug, HttpContext context, CatalogService catalogService, IWebHostEnvironment env, CancellationToken ct) =>
        {
            return await UpdateProductFromFormAsync(slug, context, catalogService, env, ct);
        })
        .Accepts<IFormFile>("multipart/form-data")
        .WithTags("Admin Catalog");

        admin.MapDelete("/products/{slug}", async (string slug, CatalogService catalogService, CancellationToken ct) =>
        {
            await catalogService.DeleteProduct(slug, ct);
            return Results.NoContent();
        })
        .WithTags("Admin Catalog");

        return app;
    }

    private static async Task<IResult> UpdateProductFromFormAsync(
        string slug,
        HttpContext context,
        CatalogService catalogService,
        IWebHostEnvironment env,
        CancellationToken ct)
    {
        var form = await context.Request.ReadFormAsync(ct);
        var currentImageUrl = OptionalFormValue(form, "currentImageUrl");
        var imageUrl = await ResolveProductImageUrlAsync(form, env, currentImageUrl, ct);
        var updated = await catalogService.UpdateProductPutReplace(
            new Application.Catalog.Contracts.UpdateProductPutReplaceRequest(
                RouteSlug: slug,
                PayloadSlug: RequiredFormValue(form, "slug"),
                Name: RequiredFormValue(form, "name"),
                Description: RequiredFormValue(form, "description"),
                Price: decimal.Parse(RequiredFormValue(form, "price"), System.Globalization.CultureInfo.InvariantCulture),
                CategorySlug: RequiredFormValue(form, "categorySlug"),
                Server: OptionalFormValue(form, "server"),
                ImageUrl: imageUrl),
            ct);

        DeleteOldUploadedProductImage(currentImageUrl, imageUrl, env);
        return Results.Ok(ToProductResponse(updated));
    }

    private static ProductResponse ToProductResponse(Application.Catalog.Contracts.ProductBySlugResponse product)
    {
        return new ProductResponse(
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
            product.ReviewCount,
            product.SalesCount);
    }

    private static string RequiredFormValue(IFormCollection form, string key)
    {
        var value = OptionalFormValue(form, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{key} is required.", key);
        }

        return value;
    }

    private static string? OptionalFormValue(IFormCollection form, string key)
    {
        return form.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString().Trim()
            : null;
    }

    private static async Task<string?> ResolveProductImageUrlAsync(IFormCollection form, IWebHostEnvironment env, string? currentImageUrl, CancellationToken ct)
    {
        var imageFile = form.Files.GetFile("imageFile");
        var imageUrl = OptionalFormValue(form, "imageUrl");

        if (imageFile is not null && imageFile.Length > 0)
        {
            return await SaveProductImageAsync(imageFile, env, ct);
        }

        return !string.IsNullOrWhiteSpace(imageUrl) ? imageUrl : currentImageUrl;
    }

    private static async Task<string> SaveProductImageAsync(IFormFile file, IWebHostEnvironment env, CancellationToken ct)
    {
        if (file.Length > MaxProductImageBytes)
        {
            throw new ArgumentException("Product image must be 5MB or smaller.", nameof(file));
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension)
            && ProductImageExtensionByContentType.TryGetValue(file.ContentType, out var extensionFromContentType))
        {
            extension = extensionFromContentType;
        }

        if (string.IsNullOrWhiteSpace(extension)
            || !AllowedProductImageTypes.TryGetValue(extension, out var allowedTypes)
            || !allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Product image must be PNG, JPG, JPEG, or WEBP.", nameof(file));
        }

        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var uploadsRoot = Path.Combine(webRoot, "uploads", "products");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsRoot, fileName);
        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, ct);

        return $"/uploads/products/{fileName}";
    }

    private static void DeleteOldUploadedProductImage(string? oldImageUrl, string? newImageUrl, IWebHostEnvironment env)
    {
        if (string.IsNullOrWhiteSpace(oldImageUrl)
            || string.Equals(oldImageUrl, newImageUrl, StringComparison.Ordinal)
            || !oldImageUrl.StartsWith("/uploads/products/", StringComparison.Ordinal))
        {
            return;
        }

        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var uploadsRoot = Path.Combine(webRoot, "uploads", "products");
        var fileName = Path.GetFileName(oldImageUrl);
        var fullPath = Path.GetFullPath(Path.Combine(uploadsRoot, fileName));
        var safeRoot = Path.GetFullPath(uploadsRoot);
        if (fullPath.StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
