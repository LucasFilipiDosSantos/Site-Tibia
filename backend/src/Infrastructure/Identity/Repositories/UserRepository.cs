using Application.Identity.Contracts;
using Domain.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = UserAccount.NormalizeEmail(email);
        return _dbContext.Users.SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
    }

    public Task<UserAccount?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public async Task AddAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
    }

    public Task UpdateAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Update(user);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
