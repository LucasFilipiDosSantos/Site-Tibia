using Domain.Identity;

namespace Application.Identity.Contracts;

public interface ISecurityTokenRepository
{
    Task AddAsync(SecurityToken token, CancellationToken cancellationToken = default);
    Task<SecurityToken?> GetActiveByTokenHashAsync(string tokenHash, string purpose, DateTimeOffset nowUtc, CancellationToken cancellationToken = default);
    Task UpdateAsync(SecurityToken token, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
