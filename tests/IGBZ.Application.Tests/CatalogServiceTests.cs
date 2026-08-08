namespace IGBZ.Application.Tests;

using IGBZ.Application.Catalog;
using IGBZ.Application.Tenancy;
using IGBZ.Application.Tests.Fakes;
using IGBZ.Domain.Catalog;
using Xunit;

public class CatalogServiceTests
{
    private readonly TenantContext _tenantContext = new();

    private (CatalogService service, FakeProductInventoryRepository products) Build()
    {
        _tenantContext.Set("t1");
        var products = new FakeProductInventoryRepository(_tenantContext);
        return (new CatalogService(products), products);
    }

    private static Product SampleProduct(string id, string slug, string name, decimal price, bool published = true, bool deleted = false)
    {
        return new Product
        {
            Id = id,
            TenantId = "t1",
            Slug = slug,
            Name = name,
            IsPublished = published,
            Deleted = deleted,
            Images = { $"https://img.test/{slug}.jpg" },
            Variants = { new ProductVariant { Sku = $"{slug}-S", PriceToman = price, StockQuantity = 5 } }
        };
    }

    [Fact]
    public async Task GetPublishedProducts_ReturnsOnlyPublishedNonDeleted()
    {
        var (service, products) = Build();
        products.Store.Add(SampleProduct("p1", "shirt", "تی‌شرت", 100_000));
        products.Store.Add(SampleProduct("p2", "pants", "شلوار", 200_000, published: false));
        products.Store.Add(SampleProduct("p3", "hat", "کلاه", 50_000, deleted: true));

        var list = await service.GetPublishedProductsAsync();

        Assert.Single(list);
        Assert.Equal("shirt", list[0].Slug);
        Assert.Equal(100_000m, list[0].PriceToman);
        Assert.Equal("https://img.test/shirt.jpg", list[0].ImageUrl);
    }

    [Fact]
    public async Task GetProductBySlug_ReturnsDetail()
    {
        var (service, products) = Build();
        products.Store.Add(SampleProduct("p1", "shirt", "تی‌شرت", 100_000));

        var detail = await service.GetProductBySlugAsync("SHIRT"); // case-insensitive

        Assert.NotNull(detail);
        Assert.Equal("تی‌شرت", detail!.Name);
        Assert.Single(detail.Variants);
        Assert.Equal(5, detail.Variants[0].AvailableQuantity);
    }

    [Fact]
    public async Task GetProductBySlug_Unpublished_ReturnsNull()
    {
        var (service, products) = Build();
        products.Store.Add(SampleProduct("p1", "shirt", "تی‌شرت", 100_000, published: false));

        var detail = await service.GetProductBySlugAsync("shirt");

        Assert.Null(detail);
    }

    [Fact]
    public async Task GetProducts_IsTenantScoped()
    {
        var (service, products) = Build();
        products.Store.Add(SampleProduct("p1", "shirt", "تی‌شرت", 100_000));

        // تننت دیگر — هیچ محصولی نباید ببیند
        _tenantContext.Set("t2");
        var list = await service.GetPublishedProductsAsync();

        Assert.Empty(list);
    }
}
