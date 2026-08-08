namespace IGBZ.Domain.Tests;

using IGBZ.Domain.Catalog;
using Xunit;

public class ProductVariantTests
{
    [Fact]
    public void AvailableQuantity_IsStockMinusReserved()
    {
        var variant = new ProductVariant { Sku = "S1", StockQuantity = 10, ReservedQuantity = 3 };
        Assert.Equal(7, variant.AvailableQuantity);
    }

    [Fact]
    public void Product_GetVariant_IsCaseInsensitive()
    {
        var product = new Product
        {
            TenantId = "t1",
            Variants = { new ProductVariant { Sku = "ABC", StockQuantity = 5 } }
        };

        Assert.NotNull(product.GetVariant("abc"));
        Assert.Null(product.GetVariant("xyz"));
    }

    [Fact]
    public void Product_IsAvailable_ChecksQuantity()
    {
        var product = new Product
        {
            TenantId = "t1",
            Variants = { new ProductVariant { Sku = "S1", StockQuantity = 5, ReservedQuantity = 4 } }
        };

        Assert.True(product.IsAvailable("S1", 1));
        Assert.False(product.IsAvailable("S1", 2));
    }
}
