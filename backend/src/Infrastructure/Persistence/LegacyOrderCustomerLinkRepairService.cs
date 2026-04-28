using Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public sealed class LegacyOrderCustomerLinkRepairService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<LegacyOrderCustomerLinkRepairService> _logger;

    public LegacyOrderCustomerLinkRepairService(
        AppDbContext dbContext,
        ILogger<LegacyOrderCustomerLinkRepairService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<LegacyOrderCustomerLinkRepairResult> RepairAsync(CancellationToken cancellationToken = default)
    {
        var candidateOrders = await _dbContext.Orders
            .Where(order => order.CustomerEmail != null && order.CustomerEmail != string.Empty)
            .OrderBy(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (candidateOrders.Count == 0)
        {
            _logger.LogInformation("Legacy order relink completed. No candidate orders with customer email were found.");
            return new LegacyOrderCustomerLinkRepairResult(0, 0, 0, 0);
        }

        var normalizedEmails = candidateOrders
            .Select(order => UserAccount.NormalizeEmail(order.CustomerEmail!))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var usersByEmail = await _dbContext.Users
            .Where(user => normalizedEmails.Contains(user.Email))
            .ToDictionaryAsync(user => user.Email, StringComparer.Ordinal, cancellationToken);

        var relinked = 0;
        var alreadyLinked = 0;
        var unmatched = 0;

        foreach (var order in candidateOrders)
        {
            var normalizedEmail = UserAccount.NormalizeEmail(order.CustomerEmail!);
            if (!usersByEmail.TryGetValue(normalizedEmail, out var user))
            {
                unmatched++;
                _logger.LogWarning(
                    "Legacy order relink could not match order {OrderId} intent {OrderIntentKey} email {CustomerEmail} to any user.",
                    order.Id,
                    order.OrderIntentKey,
                    order.CustomerEmail);
                continue;
            }

            if (order.CustomerId == user.Id)
            {
                alreadyLinked++;
                continue;
            }

            var previousCustomerId = order.CustomerId;
            order.RelinkCustomer(user.Id);
            relinked++;

            _logger.LogInformation(
                "Legacy order relink updated order {OrderId} intent {OrderIntentKey} from customer {PreviousCustomerId} to {CustomerId} using email {CustomerEmail}.",
                order.Id,
                order.OrderIntentKey,
                previousCustomerId,
                user.Id,
                order.CustomerEmail);
        }

        if (relinked > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var result = new LegacyOrderCustomerLinkRepairResult(
            candidateOrders.Count,
            relinked,
            alreadyLinked,
            unmatched);

        _logger.LogInformation(
            "Legacy order relink completed. Candidates={CandidateCount}, Relinked={RelinkedCount}, AlreadyLinked={AlreadyLinkedCount}, Unmatched={UnmatchedCount}.",
            result.CandidateCount,
            result.RelinkedCount,
            result.AlreadyLinkedCount,
            result.UnmatchedCount);

        return result;
    }
}

public sealed record LegacyOrderCustomerLinkRepairResult(
    int CandidateCount,
    int RelinkedCount,
    int AlreadyLinkedCount,
    int UnmatchedCount);
