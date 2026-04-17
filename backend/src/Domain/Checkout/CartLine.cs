namespace Domain.Checkout;

public sealed class CartLine
{
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    public CartLine(Guid productId, int quantity)
    {
        ProductId = productId == Guid.Empty
            ? throw new ArgumentException("Product id is required.", nameof(productId))
            : productId;

        Quantity = quantity > 0
            ? quantity
            : throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
    }

    public void Increase(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        Quantity += quantity;
    }

    public void SetQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        Quantity = quantity;
    }

    private CartLine()
    {
    }
}
