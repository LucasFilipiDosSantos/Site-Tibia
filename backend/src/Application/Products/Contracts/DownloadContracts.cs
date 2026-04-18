namespace Application.Products.Contracts;

public record DownloadAccessRequest(
    Guid ProductId,
    Guid UserId,
    bool HasPurchased
);

public record SignedUrlResponse(
    string SignedUrl,
    DateTimeOffset ExpiresAtUtc,
    string FileName,
    string ContentType,
    long FileSizeBytes
);

public interface IDownloadEntitlementService
{
    Task<SignedUrlResponse?> GenerateSignedUrlAsync(Guid productId, Guid userId, CancellationToken ct = default);
    Task<bool> CanAccessFreeDownloadAsync(Guid productId, Guid userId, CancellationToken ct = default);
}