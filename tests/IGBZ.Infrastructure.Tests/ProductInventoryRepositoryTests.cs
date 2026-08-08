namespace IGBZ.Infrastructure.Tests;

using IGBZ.Application.Abstractions;
using IGBZ.Application.Tenancy;
using IGBZ.Domain.Catalog;
using Xunit;

public class ProductInventoryRepositoryTests : MongoTestBase
{
    [Fact]
    public async Task TryReserve_ReservesWhenStockAvailable()
    {
        var repository = Provider.GetRequiredService<IProductInventoryRepository>();
        Provider.GetRequiredService<ITenantContext>().Set("t1");

        var product = new Product
        {
            Name = "تی‌شرت",
            Variants = { new ProductVariant { Sku = "TS-1", StockQuantity = 10, ReservedQuantity = 0 } }
        };
        await repository.InsertAsync(product);

        var reserved = await repository.TryReserveAsync(product.Id, "TS-1", 4);

        Assert.True(reserved);

        var after = await repository.GetByIdAsync(product.Id);
        Assert.Equal(4, after!.Variants[0].ReservedQuantity);
        Assert.Equal(6, after.Variants[0].AvailableQuantity);
    }

    [Fact]
    public async Task TryReserve_Fails_WhenInsufficientStock()
    {
        var repository = Provider.GetRequiredService<IProductInventoryRepository>();
        Provider.GetRequiredService<ITenantContext>().Set("t1");

        var product = new Product
        {
            Name = "تی‌شرت",
            Variants = { new ProductVariant { Sku = "TS-1", StockQuantity = 2, ReservedQuantity = 0 } }
        };
        await repository.InsertAsync(product);

        var reserved = await repository.TryReserveAsync(product.Id, "TS-1", 3);

        Assert.False(reserved);
        Assert.Equal(0, (await repository.GetByIdAsync(product.Id))!.Variants[0].ReservedQuantity);
    }

    [Fact]
    public async Task ReleaseReservation_ReturnsStock()
    {
        var repository = Provider.GetRequiredService<IProductInventoryRepository>();
        Provider.GetRequiredService<ITenantContext>().Set("t1");

        var product = new Product
        {
            Name = "تی‌شرت",
            Variants = { new ProductVariant { Sku = "TS-1", StockQuantity = 10, ReservedQuantity = 0 } }
        };
        await repository.InsertAsync(product);

        await repository.TryReserveAsync(product.Id, "TS-1", 5);
        await repository.ReleaseReservationAsync(product.Id, "TS-1", 2);

        var after = await repository.GetByIdAsync(product.Id);
        Assert.Equal(3, after!.Variants[0].ReservedQuantity);
        Assert.Equal(7, after.Variants[0].AvailableQuantity);
    }
}
