using Application.Catalog.Contracts;
using Domain.Catalog;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Catalog.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _dbContext;

    public CategoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories.SingleOrDefaultAsync(x => x.Slug == slug, cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _dbContext.Categories.AddAsync(category, cancellationToken);
    }

    public Task DeleteAsync(Category category, CancellationToken cancellationToken = default)
    {
        _dbContext.Categories.Remove(category);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
