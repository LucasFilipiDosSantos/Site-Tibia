using Application.Checkout.Contracts;
using Domain.Checkout;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Checkout.Repositories;

public sealed class CheckoutRepository : ICheckoutRepository
{
    private readonly AppDbContext _dbContext;

    public CheckoutRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _dbContext.Orders.AddAsync(order, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .Include(x => x.Items)
            .Include(x => x.DeliveryInstructions)
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);
    }
}
