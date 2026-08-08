namespace IGBZ.Application.Tests;

using IGBZ.Application.Admin;
using IGBZ.Application.Tenancy;
using IGBZ.Application.Tests.Fakes;
using IGBZ.Domain.Catalog;
using Xunit;

public class AdminProductServiceTests
{
    private readonly TenantContext _tenantContext = new();

    private (AdminProductService service, FakeProductInventoryRepository products) Build()
    {
        _tenantContext.Set("t1");
        var products = new FakeProductInventoryRepository(_tenantContext);
        return (new AdminProductService(products), products);
    }

    private static AdminProductInput ValidInput(string slug = "shirt", string sku = "SH-1") => new()
    {
        Name = "تی‌شرت",
        Slug = slug,
        Description = "توضیح",
        IsPublished = true,
        Variants = new List<AdminProductVariantInput>
        {
            new() { Sku = sku, PriceToman = 100_000, StockQuantity = 10 }
        }
    };

    [Fact]
    public async Task Create_SetsSlugAndVariant()
    {
        var (service, products) = Build();

        var id = await service.CreateAsync(ValidInput());

        var product = products.Store.Single();
        Assert.Equal("shirt", product.Slug);
        Assert.Single(product.Variants);
        Assert.Equal(100_000m, product.Variants[0].PriceToman);
        Assert.True(product.IsPublished);
        Assert.Equal(id, product.Id);
    }

    [Fact]
    public async Task Create_DuplicateSlug_Throws()
    {
        var (service, products) = Build();
        await service.CreateAsync(ValidInput());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(ValidInput()));
    }

    [Fact]
    public async Task Create_DuplicateSku_Throws()
    {
        var (service, _) = Build();
        await service.CreateAsync(ValidInput(slug: "shirt", sku: "SH-1"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(ValidInput(slug: "pants", sku: "SH-1")));
    }

    [Fact]
    public async Task Create_DuplicateSkuInsideInput_Throws()
    {
        var (service, _) = Build();

        var input = ValidInput();
        input.Variants.Add(new AdminProductVariantInput { Sku = "SH-1", PriceToman = 90_000, StockQuantity = 5 });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(input));
    }

    [Fact]
    public async Task Update_AllowsSameSkuForSameProduct()
    {
        var (service, products) = Build();
        var id = await service.CreateAsync(ValidInput());

        var input = ValidInput(slug: "shirt-new", sku: "SH-1");
        await service.UpdateAsync(id, input);

        var product = products.Store.Single();
        Assert.Equal("shirt-new", product.Slug);
        Assert.Single(product.Variants);
    }

    [Fact]
    public async Task Delete_IsSoftDelete()
    {
        var (service, products) = Build();
        var id = await service.CreateAsync(ValidInput());

        await service.DeleteAsync(id);

        var product = products.Store.Single();
        Assert.True(product.Deleted);
    }

    [Fact]
    public async Task SetPublished_Toggles()
    {
        var (service, products) = Build();
        var id = await service.CreateAsync(ValidInput());

        await service.SetPublishedAsync(id, false);

        Assert.False(products.Store.Single().IsPublished);
    }

    [Fact]
    public async Task Create_NoVariants_Throws()
    {
        var (service, _) = Build();

        var input = ValidInput();
        input.Variants.Clear();

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(input));
    }
}
