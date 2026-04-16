using Application.Identity.Contracts;
using Domain.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity.Repositories;

public sealed class SecurityTokenRepository : ISecurityTokenRepository
{
    private readonly AppDbContext _dbContext;

    public SecurityTokenRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(SecurityToken token, CancellationToken cancellationToken = default)
    {
        await _dbContext.SecurityTokens.AddAsync(token, cancellationToken);
    }

    public Task<SecurityToken?> GetActiveByTokenHashAsync(string tokenHash, string purpose, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
    {
        return _dbContext.SecurityTokens
            .SingleOrDefaultAsync(x =>
                x.TokenHash == tokenHash &&
                x.Purpose == purpose &&
                x.ConsumedAtUtc == null &&
                x.ExpiresAtUtc > nowUtc,
                cancellationToken);
    }

    public Task UpdateAsync(SecurityToken token, CancellationToken cancellationToken = default)
    {
        _dbContext.SecurityTokens.Update(token);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
