namespace API.Downloads;

using System.Security.Cryptography;
using System.Text;
using Application.Identity.Contracts;
using Application.Products.Contracts;
using Application.Products.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public static class DownloadEndpoints
{
    public static IEndpointRouteBuilder MapDownloadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/downloads")
            .WithTags("Downloads");

        // Generate signed download URL (requires auth + purchase)
        group.MapPost("/generate-url", GenerateDownloadUrl)
            .RequireAuthorization();

        // Download file via signed URL token (public endpoint with token validation)
        group.MapGet("/file/{token}", DownloadFile)
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> GenerateDownloadUrl(
        [FromBody] GenerateDownloadUrlRequest request,
        IDownloadEntitlementService entitlementService,
        Guid userId)
    {
        var result = await entitlementService.GenerateSignedUrlAsync(request.ProductId, userId);
        if (result is null)
        {
            return Results.NotFound(new { error = "Download not available or not entitled" });
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> DownloadFile(
        string token,
        IProductDownloadRepository downloadRepository,
        ISystemClock clock,
        ISigningKeyProvider signingKeyProvider)
    {
        // Validate signed token
        var isValid = DownloadEntitlementService.ValidateSignedToken(
            token,
            signingKeyProvider.GetKey(),
            clock.UtcNow,
            out var downloadId,
            out var expiresAt);

        if (!isValid || downloadId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Invalid or expired download token" });
        }

        // Get download metadata
        var download = await downloadRepository.GetByIdAsync(downloadId);
        if (download is null)
        {
            return Results.NotFound(new { error = "Download not found" });
        }

        // TODO: Load file from storage and stream with proper content type
        // For now, return 501 Not Implemented until storage is configured
        return Results.Json(
            new
            {
                fileName = download.FileName,
                contentType = download.ContentType,
                fileSize = download.FileSizeBytes,
                expiresAt = expiresAt,
                message = "File storage not yet configured"
            },
            statusCode: 501);
    }
}

public sealed class GenerateDownloadUrlRequest
{
    public Guid ProductId { get; init; }
}

public sealed class DownloadSigningKeyProvider : ISigningKeyProvider
{
    private readonly string _key;

    public DownloadSigningKeyProvider(IConfiguration configuration)
    {
        _key = configuration["DownloadSigningKey"] 
            ?? throw new InvalidOperationException("DownloadSigningKey is required");
    }

    public string GetKey() => _key;
}