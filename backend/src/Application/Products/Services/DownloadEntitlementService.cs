namespace Application.Products.Services;

using System.Security.Cryptography;
using System.Text;
using Application.Checkout.Contracts;
using Application.Identity.Contracts;
using Application.Products.Contracts;
using Domain.Identity;
using Domain.Products;

public sealed class DownloadEntitlementService : IDownloadEntitlementService
{
    private const int SignedUrlExpirationMinutes = 15;
    private const string DownloadTokenPrefix = "dl_";

    private readonly IProductDownloadRepository _downloadRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrderLifecycleRepository _orderRepository;
    private readonly ISystemClock _clock;
    private readonly ISigningKeyProvider _signingKeyProvider;
    private readonly string _baseUrl;

    public DownloadEntitlementService(
        IProductDownloadRepository downloadRepository,
        IUserRepository userRepository,
        IOrderLifecycleRepository orderRepository,
        ISystemClock clock,
        ISigningKeyProvider signingKeyProvider,
        string baseUrl)
    {
        _downloadRepository = downloadRepository;
        _userRepository = userRepository;
        _orderRepository = orderRepository;
        _clock = clock;
        _signingKeyProvider = signingKeyProvider;
        _baseUrl = baseUrl;
    }

    public async Task<SignedUrlResponse?> GenerateSignedUrlAsync(Guid productId, Guid userId, CancellationToken ct = default)
    {
        // 1. Get product download metadata
        var download = await _downloadRepository.GetByProductIdAsync(productId, ct);
        if (download is null)
        {
            return null;
        }

        // 2. Check if user has purchased (entitlement)
        var hasPurchased = await CheckPurchaseEntitlementAsync(productId, userId, ct);
        if (!hasPurchased)
        {
            return null;
        }

        // 3. Generate signed token with embedded expiration
        var expiresAt = _clock.UtcNow.AddMinutes(SignedUrlExpirationMinutes);
        var token = GenerateSignedToken(download.Id, expiresAt, _signingKeyProvider.GetKey());

        var signedUrl = $"{_baseUrl}/api/downloads/file/{token}";

        return new SignedUrlResponse(
            SignedUrl: signedUrl,
            ExpiresAtUtc: expiresAt,
            FileName: download.FileName,
            ContentType: download.ContentType,
            FileSizeBytes: download.FileSizeBytes
        );
    }

    public async Task<bool> CanAccessFreeDownloadAsync(Guid productId, Guid userId, CancellationToken ct = default)
    {
        // Get user account to check role
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
        {
            return false;
        }

        // Check if product has free download access policy (by category or global)
        // For now, we implement simple product-based free access check
        var download = await _downloadRepository.GetByProductIdAsync(productId, ct);
        if (download is null)
        {
            return false;
        }

        // Check user's role against policy
        // TODO: Load DownloadAccessPolicy from repository
        // For now, return true for Admin role (can access all free downloads)
        return user.Role == UserRole.Admin || user.Role == UserRole.Costumer;
    }

    private async Task<bool> CheckPurchaseEntitlementAsync(Guid productId, Guid userId, CancellationToken ct)
    {
        // Check if user has any paid order containing this product
        var orders = await _orderRepository.GetCustomerOrdersAsync(userId, 1, 100, ct);
        return orders.Any(o => o.Status == Domain.Checkout.OrderStatus.Paid 
            && o.Items.Any(i => i.ProductId == productId));
    }

    private static string GenerateSignedToken(Guid downloadId, DateTimeOffset expiresAt, string secretKey)
    {
        var payload = $"{downloadId}:{expiresAt:O}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        var signature = Convert.ToBase64String(hash);

        var token = $"{DownloadTokenPrefix}{Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))}:{signature}";
        return token.Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    public static bool ValidateSignedToken(string token, string secretKey, DateTimeOffset currentTime, out Guid downloadId, out DateTimeOffset expiresAt)
    {
        downloadId = Guid.Empty;
        expiresAt = DateTimeOffset.MinValue;

        if (string.IsNullOrEmpty(token) || !token.StartsWith(DownloadTokenPrefix))
        {
            return false;
        }

        try
        {
            var tokenBody = token[DownloadTokenPrefix.Length..];
            var parts = tokenBody.Split(':');
            if (parts.Length != 2)
            {
                return false;
            }

            var payload = parts[0].Replace("-", "+").Replace("_", "/");
            var signature = parts[1].Replace("-", "+").Replace("_", "/");

            // Decode payload
            var padLength = (4 - payload.Length % 4) % 4;
            var payloadPadded = payload + new string('=', padLength);
            var payloadBytes = Convert.FromBase64String(payloadPadded);
            var payloadStr = Encoding.UTF8.GetString(payloadBytes);
            var payloadParts = payloadStr.Split(':');

            if (payloadParts.Length != 2 || !Guid.TryParse(payloadParts[0], out downloadId))
            {
                return false;
            }

            if (!DateTimeOffset.TryParse(payloadParts[1], out expiresAt))
            {
                return false;
            }

            // Check expiration
            if (expiresAt <= currentTime)
            {
                return false;
            }

            // Verify HMAC
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            using var hmac = new HMACSHA256(keyBytes);
            var expectedHash = hmac.ComputeHash(payloadBytes);
            var expectedSignature = Convert.ToBase64String(expectedHash);

            return signature == expectedSignature;
        }
        catch
        {
            return false;
        }
    }
}

public interface ISigningKeyProvider
{
    string GetKey();
}