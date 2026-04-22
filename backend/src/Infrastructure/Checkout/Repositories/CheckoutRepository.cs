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

    public async Task<IReadOnlyList<Order>> SearchOrdersAsync(
        OrderStatus? status,
        Guid? customerId,
        DateTimeOffset? createdFromUtc,
        DateTimeOffset? createdToUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders
            .AsNoTracking()
            .Where(o => !o.IsHidden)
            .AsQueryable();
        
        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);
        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);
        if (createdFromUtc.HasValue)
            query = query.Where(o => o.CreatedAtUtc >= createdFromUtc.Value);
        if (createdToUtc.HasValue)
            query = query.Where(o => o.CreatedAtUtc <= createdToUtc.Value);
            
        var offset = Math.Max(page - 1, 0) * pageSize;

        return await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip(offset)
            .Take(pageSize)
            .Include(o => o.Items)
            .ToListAsync(cancellationToken);
    }
}
