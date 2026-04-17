namespace Domain.Checkout;

public sealed class Cart
{
    private readonly List<CartLine> _lines = [];

    public Guid CustomerId { get; private set; }
    public IReadOnlyList<CartLine> Lines => _lines;

    public Cart(Guid customerId)
    {
        CustomerId = customerId == Guid.Empty
            ? throw new ArgumentException("Customer id is required.", nameof(customerId))
            : customerId;
    }

    public void AddOrMerge(Guid productId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        var existingLine = _lines.SingleOrDefault(line => line.ProductId == productId);
        if (existingLine is null)
        {
            _lines.Add(new CartLine(productId, quantity));
            return;
        }

        existingLine.Increase(quantity);
    }

    public void SetQuantity(Guid productId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        var line = _lines.SingleOrDefault(x => x.ProductId == productId)
            ?? throw new InvalidOperationException("Product is not present in cart.");

        line.SetQuantity(quantity);
    }

    public void Remove(Guid productId)
    {
        var line = _lines.SingleOrDefault(x => x.ProductId == productId);
        if (line is null)
        {
            return;
        }

        _lines.Remove(line);
    }

    public void Clear()
    {
        _lines.Clear();
    }

    private Cart()
    {
    }
}
