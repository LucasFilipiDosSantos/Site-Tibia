namespace Domain.Products;

public sealed class ProductDownload
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = "application/octet-stream";
    public long FileSizeBytes { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private ProductDownload()
    {
    }

    public ProductDownload(Guid productId, string fileName, string filePath, long fileSizeBytes)
    {
        ProductId = ValidateProductId(productId);
        FileName = RequireFileName(fileName);
        FilePath = RequireFilePath(filePath);
        FileSizeBytes = ValidateFileSize(fileSizeBytes);
        ContentType = DetectContentType(fileName);
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static ProductDownload Create(Guid productId, string fileName, string filePath, long fileSizeBytes)
    {
        return new ProductDownload(productId, fileName, filePath, fileSizeBytes);
    }

    private static Guid ValidateProductId(Guid productId)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID is required.", nameof(productId));
        }

        return productId;
    }

    private static string RequireFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        return fileName.Trim();
    }

    private static string RequireFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        return filePath.Trim();
    }

    private static long ValidateFileSize(long fileSizeBytes)
    {
        if (fileSizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "File size cannot be negative.");
        }

        if (fileSizeBytes > 1_000_000_000) // 1GB limit
        {
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "File size exceeds maximum allowed (1GB).");
        }

        return fileSizeBytes;
    }

    private static string DetectContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;
        return extension switch
        {
            ".zip" => "application/zip",
            ".txt" => "text/plain",
            ".lua" => "text/plain",
            ".xml" => "application/xml",
            ".json" => "application/json",
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            _ => "application/octet-stream"
        };
    }
}