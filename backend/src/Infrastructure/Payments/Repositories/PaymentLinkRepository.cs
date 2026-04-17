using Application.Payments.Contracts;
using Domain.Payments;
using Infrastructure.Persistence;

namespace Infrastructure.Payments.Repositories;

public sealed class PaymentLinkRepository : IPaymentLinkRepository
{
    private readonly AppDbContext _dbContext;

    public PaymentLinkRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(PaymentLinkSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var entity = new PaymentLink(
            Guid.NewGuid(),
            snapshot.OrderId,
            snapshot.PreferenceId,
            snapshot.ExpectedAmount,
            snapshot.ExpectedCurrency,
            snapshot.CreatedAtUtc);

        await _dbContext.PaymentLinks.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
