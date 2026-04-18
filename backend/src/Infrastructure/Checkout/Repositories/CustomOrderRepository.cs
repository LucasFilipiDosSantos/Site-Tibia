using Application.Checkout.Contracts;
using Domain.Checkout;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Checkout.Repositories;

public sealed class CustomOrderRepository : ICustomOrderRepository
{
    private readonly AppDbContext _db;

    public CustomOrderRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CustomRequest?> GetByIdAsync(Guid id)
    {
        return await _db.CustomRequests.FindAsync(id);
    }

    public async Task<IReadOnlyList<CustomRequest>> GetByCustomerIdAsync(Guid customerId)
    {
        return await _db.CustomRequests
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();
    }

    public Task AddAsync(CustomRequest request)
    {
        _db.CustomRequests.Add(request);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}