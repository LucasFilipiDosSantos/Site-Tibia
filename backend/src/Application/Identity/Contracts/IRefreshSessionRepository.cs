using Domain.Identity;

namespace Application.Identity.Contracts;

public interface IRefreshSessionRepository
{
    Task<RefreshSession?> GetActiveByTokenHashAsync(string tokenHash, DateTimeOffset nowUtc, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshSession session, CancellationToken cancellationToken = default);
    Task RevokeCurrentAndInsertNextAsync(
        RefreshSession currentSession,
        RefreshSession nextSession,
        DateTimeOffset revokedAtUtc,
        string? revokedByIp,
        CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
