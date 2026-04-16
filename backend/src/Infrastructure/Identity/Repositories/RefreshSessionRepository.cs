using Application.Identity.Contracts;
using Domain.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity.Repositories;

public sealed class RefreshSessionRepository : IRefreshSessionRepository
{
    private readonly AppDbContext _dbContext;

    public RefreshSessionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RefreshSession?> GetActiveByTokenHashAsync(string tokenHash, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
    {
        return _dbContext.RefreshSessions
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash && x.RevokedAtUtc == null && x.ExpiresAtUtc > nowUtc, cancellationToken);
    }

    public async Task AddAsync(RefreshSession session, CancellationToken cancellationToken = default)
    {
        await _dbContext.RefreshSessions.AddAsync(session, cancellationToken);
    }

    public async Task RevokeCurrentAndInsertNextAsync(
        RefreshSession currentSession,
        RefreshSession nextSession,
        DateTimeOffset revokedAtUtc,
        string? revokedByIp,
        CancellationToken cancellationToken = default)
    {
        currentSession.Revoke(revokedAtUtc, revokedByIp, nextSession.TokenHash);
        _dbContext.RefreshSessions.Update(currentSession);
        await _dbContext.RefreshSessions.AddAsync(nextSession, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
