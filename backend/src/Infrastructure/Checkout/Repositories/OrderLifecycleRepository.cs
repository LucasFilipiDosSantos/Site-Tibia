using Domain.Checkout;
using Application.Checkout.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Checkout.Repositories;

public sealed class OrderLifecycleRepository : IOrderLifecycleRepository
{
    private readonly Persistence.AppDbContext _context;

    public OrderLifecycleRepository(Persistence.AppDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.DeliveryInstructions)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetCustomerOrdersAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Per D-10: Default sort is newest-first by CreatedAtUtc
        return await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Include(o => o.Items)
            .Include(o => o.DeliveryInstructions)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(Order order, CancellationToken cancellationToken = default)
    {
        // Check if order exists
        var exists = await _context.Orders.AnyAsync(o => o.Id == order.Id, cancellationToken);
        
        if (exists)
        {
            _context.Orders.Update(order);
        }
        else
        {
            await _context.Orders.AddAsync(order, cancellationToken);
        }

        // Per D-08: Transition events are append-only, add new ones
        foreach (var evt in order.StatusHistory)
        {
            var eventExists = await _context.OrderStatusTransitionEvents
                .AnyAsync(e => e.Id == evt.Id, cancellationToken);
            
            if (!eventExists)
            {
                await _context.OrderStatusTransitionEvents.AddAsync(evt, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}