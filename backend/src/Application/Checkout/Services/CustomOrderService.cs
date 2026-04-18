using Application.Checkout.Contracts;
using Domain.Checkout;

namespace Application.Checkout.Services;

public sealed class CustomOrderService : ICustomOrderService
{
    private readonly ICustomOrderRepository _repository;

    public CustomOrderService(ICustomOrderRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<CustomRequestResponse> CreateRequestAsync(Guid customerId, CreateCustomRequestInput input)
    {
        var request = CustomRequest.Create(customerId, input.Description, input.OrderId);
        await _repository.AddAsync(request);
        await _repository.SaveChangesAsync();
        return ToResponse(request);
    }

    public async Task<CustomRequestResponse?> GetByIdAsync(Guid requestId, Guid customerId)
    {
        var request = await _repository.GetByIdAsync(requestId);
        if (request == null || request.CustomerId != customerId)
            return null;
        return ToResponse(request);
    }

    public async Task<IReadOnlyList<CustomRequestResponse>> GetCustomerRequestsAsync(Guid customerId)
    {
        var requests = await _repository.GetByCustomerIdAsync(customerId);
        return requests.Select(ToResponse).ToList();
    }

    public async Task<CustomRequestResponse> StartProgressAsync(Guid requestId, Guid adminId)
    {
        var request = await _repository.GetByIdAsync(requestId)
            ?? throw new InvalidOperationException($"Custom request {requestId} not found.");

        request.StartProgress();
        await _repository.SaveChangesAsync();
        return ToResponse(request);
    }

    public async Task<CustomRequestResponse> MarkDeliveredAsync(Guid requestId, Guid adminId)
    {
        var request = await _repository.GetByIdAsync(requestId)
            ?? throw new InvalidOperationException($"Custom request {requestId} not found.");

        request.MarkDelivered();
        await _repository.SaveChangesAsync();
        return ToResponse(request);
    }

    private static CustomRequestResponse ToResponse(CustomRequest request)
    {
        return new CustomRequestResponse(
            request.Id,
            request.OrderId,
            request.CustomerId,
            request.Description,
            request.Status.ToString(),
            request.CreatedAtUtc,
            request.UpdatedAtUtc
        );
    }
}