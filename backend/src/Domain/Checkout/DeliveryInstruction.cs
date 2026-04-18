namespace Domain.Checkout;

public sealed class DeliveryInstruction
{
    public Guid ProductId { get; private set; }
    public FulfillmentType FulfillmentType { get; private set; }
    public DeliveryStatus Status { get; private set; } = DeliveryStatus.Pending;
    public DateTime? CompletedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public string? TargetCharacter { get; private set; }
    public string? TargetServer { get; private set; }
    public string? DeliveryChannelOrContact { get; private set; }
    public string? RequestBrief { get; private set; }
    public string? ContactHandle { get; private set; }

    private DeliveryInstruction()
    {
    }

    public static DeliveryInstruction CreateAutomated(
        Guid productId,
        string targetCharacter,
        string targetServer,
        string deliveryChannelOrContact)
    {
        return new DeliveryInstruction
        {
            ProductId = productId == Guid.Empty
                ? throw new ArgumentException("Product id is required.", nameof(productId))
                : productId,
            FulfillmentType = FulfillmentType.Automated,
            Status = DeliveryStatus.Pending,
            TargetCharacter = RequireText(targetCharacter, nameof(targetCharacter)),
            TargetServer = RequireText(targetServer, nameof(targetServer)),
            DeliveryChannelOrContact = RequireText(deliveryChannelOrContact, nameof(deliveryChannelOrContact))
        };
    }

    public static DeliveryInstruction CreateManual(Guid productId, string requestBrief, string contactHandle)
    {
        return new DeliveryInstruction
        {
            ProductId = productId == Guid.Empty
                ? throw new ArgumentException("Product id is required.", nameof(productId))
                : productId,
            FulfillmentType = FulfillmentType.Manual,
            Status = DeliveryStatus.Pending,
            RequestBrief = RequireText(requestBrief, nameof(requestBrief)),
            ContactHandle = RequireText(contactHandle, nameof(contactHandle))
        };
    }

    public void Complete()
    {
        MarkCompleted(DateTime.UtcNow);
    }

    public void Fail(string reason)
    {
        MarkFailed(reason);
    }

    private void MarkCompleted(DateTime completedAtUtc)
    {
        Status = DeliveryStatus.Completed;
        CompletedAtUtc = completedAtUtc;
        FailureReason = null;
    }

    private void MarkFailed(string reason)
    {
        Status = DeliveryStatus.Failed;
        FailureReason = reason ?? throw new ArgumentNullException(nameof(reason));
        CompletedAtUtc = null;
    }

    private static string RequireText(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", paramName);
        }

        return value.Trim();
    }
}