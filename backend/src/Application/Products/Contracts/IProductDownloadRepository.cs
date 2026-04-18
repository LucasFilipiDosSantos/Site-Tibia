using Domain.Products;

namespace Application.Products.Contracts;

public interface IProductDownloadRepository
{
    Task<ProductDownload?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ProductDownload?> GetByIdAsync(Guid downloadId, CancellationToken cancellationToken = default);
    Task AddAsync(ProductDownload download, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}