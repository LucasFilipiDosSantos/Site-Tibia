namespace Domain.Products;

using Domain.Identity;

public sealed class DownloadAccessPolicy
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string PolicyName { get; private set; } = string.Empty;
    public Guid? ProductCategoryId { get; private set; }
    public bool AllowFreeDownload { get; private set; }
    public UserRole[] AllowedRoles { get; private set; } = [];
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private DownloadAccessPolicy()
    {
    }

    public DownloadAccessPolicy(string policyName, Guid? productCategoryId, bool allowFreeDownload, UserRole[] allowedRoles)
    {
        PolicyName = RequirePolicyName(policyName);
        ProductCategoryId = productCategoryId;
        AllowFreeDownload = allowFreeDownload;
        AllowedRoles = allowedRoles ?? [];
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static DownloadAccessPolicy CreateFreePolicy(string policyName, Guid? productCategoryId, UserRole[] allowedRoles)
    {
        return new DownloadAccessPolicy(policyName, productCategoryId, allowFreeDownload: true, allowedRoles);
    }

    public static DownloadAccessPolicy CreatePaidOnlyPolicy(string policyName, Guid? productCategoryId)
    {
        return new DownloadAccessPolicy(policyName, productCategoryId, allowFreeDownload: false, allowedRoles: []);
    }

    private static string RequirePolicyName(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            throw new ArgumentException("Policy name is required.", nameof(policyName));
        }

        return policyName.Trim();
    }

    public bool AllowsDownload(UserRole userRole, bool hasPurchased)
    {
        // Paid download requires purchase
        if (!AllowFreeDownload)
        {
            return hasPurchased;
        }

        // Free download: if no role restrictions, allow all
        if (AllowedRoles.Length == 0)
        {
            return true;
        }

        // Check user's role against allowed roles
        return AllowedRoles.Contains(userRole);
    }
}