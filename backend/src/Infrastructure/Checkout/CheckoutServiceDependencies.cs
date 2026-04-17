using Application.Catalog.Contracts;
using Application.Checkout.Contracts;
using Application.Inventory.Contracts;
using Domain.Checkout;

namespace Infrastructure.Checkout;

public sealed class CheckoutInventoryGateway : ICheckoutInventoryGateway
{
    private readonly Application.Inventory.Services.InventoryService _inventoryService;

    public CheckoutInventoryGateway(Application.Inventory.Services.InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task ReserveStockForCheckoutAsync(
        Guid orderId,
        string orderIntentKey,
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _inventoryService.ReserveStockForCheckoutAsync(
                new ReserveStockForCheckoutRequest(orderIntentKey, orderId, productId, quantity),
                cancellationToken);
        }
        catch (InventoryReservationConflictException ex)
        {
            throw new CheckoutReservationConflictException([new CheckoutLineConflict(ex.ProductId, ex.RequestedQuantity, ex.AvailableQuantity)]);
        }
    }
}

public sealed class CheckoutProductCatalogGateway : ICheckoutProductCatalogGateway
{
    private readonly IProductRepository _productRepository;

    public CheckoutProductCatalogGateway(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<CheckoutProductSnapshot> GetSnapshotAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.ListAsync(
            new ProductListQuery(null, null, 0, 2000),
            cancellationToken);

        var product = products.SingleOrDefault(x => x.Id == productId)
            ?? throw new InvalidOperationException($"Product '{productId}' was not found.");

        return new CheckoutProductSnapshot(
            product.Id,
            product.Name,
            product.Slug,
            product.CategorySlug,
            product.Price,
            "BRL",
            ResolveFulfillmentType(product.Slug));
    }

    private static FulfillmentType ResolveFulfillmentType(string productSlug)
    {
        return productSlug.Contains("manual", StringComparison.OrdinalIgnoreCase)
            ? FulfillmentType.Manual
            : FulfillmentType.Automated;
    }
}

public sealed class CartProductAvailabilityGateway : ICartProductAvailabilityGateway
{
    private readonly Application.Inventory.Services.InventoryService _inventoryService;

    public CartProductAvailabilityGateway(Application.Inventory.Services.InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task<ProductAvailabilityResponse> GetAvailabilityAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var availability = await _inventoryService.GetAvailabilityAsync(new GetInventoryAvailabilityRequest(productId), cancellationToken);
        return new ProductAvailabilityResponse(productId, availability.Available);
    }
}
