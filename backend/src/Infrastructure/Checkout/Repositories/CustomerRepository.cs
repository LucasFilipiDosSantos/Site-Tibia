using Application.Checkout.Contracts;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Checkout.Repositories;

/// <summary>
/// Current customer profile persistence does not yet store a notification phone.
/// This adapter satisfies checkout dependencies and returns null until the
/// customer contact field is introduced in the identity model.
/// </summary>
public sealed class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _dbContext;

    public CustomerRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> GetNotificationPhoneAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.Users
            .AnyAsync(user => user.Id == customerId, cancellationToken);

        return exists ? null : null;
    }
}
