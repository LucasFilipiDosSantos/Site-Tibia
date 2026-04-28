using Domain.Checkout;
using Application.Checkout.Contracts;
using Domain.Identity;
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

    public async Task<IReadOnlyList<Order>> GetCustomerOrdersAsync(Guid customerId, string? customerEmail, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Per D-10: Default sort is newest-first by CreatedAtUtc
        var offset = Math.Max(page - 1, 0) * pageSize;
        var normalizedEmail = NormalizeEmail(customerEmail);

        return await _context.Orders
            .AsNoTracking()
            .Where(o => !o.IsHidden && (o.CustomerId == customerId
                || (normalizedEmail != null
                    && o.CustomerEmail != null
                    && o.CustomerEmail.ToLower() == normalizedEmail)))
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip(offset)
            .Take(pageSize)
            .Include(o => o.Items)
            .Include(o => o.DeliveryInstructions)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasPaidOrderForProductAsync(Guid customerId, string? customerEmail, Guid productId, CancellationToken cancellationToken = default)
    {
        var eligibleStatuses = OrderStatusExtensions.GetReviewEligibleStatuses();
        var normalizedEmail = NormalizeEmail(customerEmail);

        return _context.Orders
            .AsNoTracking()
            .Where(o => (o.CustomerId == customerId
                || (normalizedEmail != null
                    && o.CustomerEmail != null
                    && o.CustomerEmail.ToLower() == normalizedEmail))
                && !o.IsHidden
                && eligibleStatuses.Contains(o.Status))
            .AnyAsync(o => o.Items.Any(i => i.ProductId == productId), cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewOrderDiagnostic>> GetReviewOrderDiagnosticsAsync(Guid customerId, string? customerEmail, Guid productId, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(customerEmail);

        return await _context.Orders
            .AsNoTracking()
            .Where(o => (o.CustomerId == customerId
                || (normalizedEmail != null
                    && o.CustomerEmail != null
                    && o.CustomerEmail.ToLower() == normalizedEmail))
                && o.Items.Any(i => i.ProductId == productId))
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new ReviewOrderDiagnostic(
                o.Id,
                o.OrderIntentKey,
                o.CustomerId,
                o.CustomerEmail,
                o.Status,
                o.IsHidden,
                o.Items.Count,
                o.Items
                    .OrderBy(i => i.ProductSlug)
                    .Select(i => new ReviewOrderItemDiagnostic(i.ProductId, i.ProductSlug))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email) ? null : UserAccount.NormalizeEmail(email);
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
