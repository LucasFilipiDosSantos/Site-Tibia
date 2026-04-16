using Domain.Catalog;

namespace UnitTests.Catalog;

public sealed class CatalogDomainInvariantTests
{
    [Fact]
    public void Product_SlugIsNormalizedAndImmutableAfterConstruction()
    {
        var product = new Product("Gold Package", "  Gold-Pack-01  ", "Starter package", 10m, "gold");

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
        var product = new Product("Gold Package", "gold-package", "Starter package", 0m, "gold");

        product.ReplaceDetails("Gold Package Updated", "Updated package", 0m, "gold");

        Assert.Equal(0m, product.Price);
    }

    [Fact]
    public void Product_RejectsNegativePriceOnCreateAndUpdate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Product("Gold Package", "gold-package", "Starter package", -0.01m, "gold"));

        var product = new Product("Gold Package", "gold-package", "Starter package", 10m, "gold");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            product.ReplaceDetails("Gold Package", "Starter package", -1m, "gold"));
    }

    [Fact]
    public void Product_HasNoServerScopeFieldInModel()
    {
        var properties = typeof(Product).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("Server", properties);
        Assert.DoesNotContain("World", properties);
        Assert.DoesNotContain("Aurera", properties);
        Assert.DoesNotContain("Eternia", properties);
    }
}
