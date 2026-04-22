using Domain.Catalog;

namespace UnitTests.Catalog;

public sealed class CatalogDomainInvariantTests
{
    [Fact]
    public void Product_SlugIsNormalizedAndImmutableAfterConstruction()
    {
        var product = new Product("Gold Package", "  Gold-Pack-01  ", "Starter package", 10m, Guid.NewGuid(), "gold");

        Assert.Equal("gold-pack-01", product.Slug);
        Assert.Equal("gold-pack-01", product.Slug);
    }

    [Fact]
    public void Category_SlugIsRequiredAndImmutableAfterCreation()
    {
        Assert.Throws<ArgumentException>(() => new Category("Gold", "   ", "Gold category"));

        var category = new Category("Gold", "gold", "Gold category");
        category.UpdateDetails("Gold Updated", "Updated description");

        Assert.Equal("gold", category.Slug);
    }

    [Fact]
    public void Product_AllowsZeroPriceOnCreateAndUpdate()
    {
        var product = new Product("Gold Package", "gold-package", "Starter package", 0m, Guid.NewGuid(), "gold");

        product.ReplaceDetails("Gold Package Updated", "Updated package", 0m, Guid.NewGuid(), "gold");

        Assert.Equal(0m, product.Price);
    }

    [Fact]
    public void Product_RejectsNegativePriceOnCreateAndUpdate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Product("Gold Package", "gold-package", "Starter package", -0.01m, Guid.NewGuid(), "gold"));

        var product = new Product("Gold Package", "gold-package", "Starter package", 10m, Guid.NewGuid(), "gold");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            product.ReplaceDetails("Gold Package", "Starter package", -1m, Guid.NewGuid(), "gold"));
    }

    [Fact]
    public void Product_CatalogMetadataIsValidated()
    {
        var product = new Product("Coin Package", "coin-package", "Starter package", 10m, Guid.NewGuid(), "coin", "Aurera", 4.8m, 10);

        Assert.Equal("Aurera", product.Server);
        Assert.Equal(4.8m, product.Rating);
        Assert.Equal(10, product.SalesCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => product.UpdateCatalogMetadata("Aurera", 5.1m, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => product.UpdateCatalogMetadata("Aurera", 4.8m, -1));
    }
}
