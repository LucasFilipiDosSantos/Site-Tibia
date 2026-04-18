namespace Application.Checkout.Contracts;

public record CreateCustomRequestInput(
    string Description,
    Guid? OrderId
);

public record CustomRequestResponse(
    Guid Id,
    Guid? OrderId,
    Guid CustomerId,
    string Description,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public interface ICustomOrderService
{
    Task<CustomRequestResponse> CreateRequestAsync(Guid customerId, CreateCustomRequestInput input);
    Task<CustomRequestResponse?> GetByIdAsync(Guid requestId, Guid customerId);
    Task<IReadOnlyList<CustomRequestResponse>> GetCustomerRequestsAsync(Guid customerId);
    Task<CustomRequestResponse> StartProgressAsync(Guid requestId, Guid adminId);
    Task<CustomRequestResponse> MarkDeliveredAsync(Guid requestId, Guid adminId);
}

public interface ICustomOrderRepository
{
    Task<Domain.Checkout.CustomRequest?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Domain.Checkout.CustomRequest>> GetByCustomerIdAsync(Guid customerId);
    Task AddAsync(Domain.Checkout.CustomRequest request);
    Task SaveChangesAsync();
}