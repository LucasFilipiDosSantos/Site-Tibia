namespace Domain.Checkout;

public sealed class CustomRequest
{
    public Guid Id { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public CustomRequestStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private CustomRequest() { }

    public static CustomRequest Create(Guid customerId, string description, Guid? orderId = null)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer id is required.", nameof(customerId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        var now = DateTime.UtcNow;
        return new CustomRequest
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Description = description.Trim(),
            OrderId = orderId,
            Status = CustomRequestStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void StartProgress()
    {
        if (Status != CustomRequestStatus.Pending)
            throw new InvalidOperationException("Can only start pending requests");
        Status = CustomRequestStatus.InProgress;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkDelivered()
    {
        if (Status != CustomRequestStatus.InProgress)
            throw new InvalidOperationException("Can only deliver in-progress requests");
        Status = CustomRequestStatus.Delivered;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}