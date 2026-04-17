using Application.Checkout.Contracts;
using Domain.Checkout;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Checkout.Repositories;

public sealed class CartRepository : ICartRepository
{
    private readonly AppDbContext _dbContext;

    public CartRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Cart?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Carts
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CustomerId == customerId, cancellationToken);
    }

    public async Task SaveAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Carts
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CustomerId == cart.CustomerId, cancellationToken);

        if (existing is null)
        {
            await _dbContext.Carts.AddAsync(cart, cancellationToken);
        }
        else
        {
            existing.Clear();
            foreach (var line in cart.Lines)
            {
                existing.AddOrMerge(line.ProductId, line.Quantity);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Carts
            .SingleOrDefaultAsync(x => x.CustomerId == customerId, cancellationToken);

        if (existing is null)
        {
            return;
        }

        _dbContext.Carts.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
