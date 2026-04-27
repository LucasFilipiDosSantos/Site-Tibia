namespace Domain.Checkout;

public static class OrderStatusExtensions
{
    private static readonly HashSet<OrderStatus> ReviewEligibleStatuses =
    [
        OrderStatus.Paid
    ];

    public static bool IsReviewEligible(this OrderStatus status) => ReviewEligibleStatuses.Contains(status);

    public static IReadOnlyList<OrderStatus> GetReviewEligibleStatuses() => ReviewEligibleStatuses.ToArray();
}
