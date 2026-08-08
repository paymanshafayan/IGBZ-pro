namespace IGBZ.Application.Tests;

using IGBZ.Application.Marketplace;
using IGBZ.Application.Tenancy;
using IGBZ.Application.Tests.Fakes;
using IGBZ.Domain.Catalog;
using Xunit;

public class MarketplaceServiceTests
{
    private readonly TenantContext _tenantContext = new();

    private (MarketplaceService service, FakeProductInventoryRepository products, FakeCredentialRepository credentials) Build()
    {
        _tenantContext.Set("t1");
        var products = new FakeProductInventoryRepository(_tenantContext);
        var credentials = new FakeCredentialRepository(_tenantContext);
        var service = new MarketplaceService(products, credentials, new FakeHttpClientFactory());
        return (service, products, credentials);
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
            Images = { $"https://img/{slug}.jpg" },
            Variants = { new ProductVariant { Sku = $"{slug}-S", PriceToman = price, StockQuantity = 5 } }
        };
    }

    [Fact]
    public async Task TorobFeed_OnlyPublishedNonDeleted_FromRealCatalog()
    {
        var (service, products, _) = Build();
        products.Store.Add(SampleProduct("p1", "shirt", "تی‌شرت", 100_000));
        products.Store.Add(SampleProduct("p2", "pants", "شلوار", 200_000, published: false));
        products.Store.Add(SampleProduct("p3", "hat", "کلاه", 50_000, deleted: true));

        var feed = await service.GetTorobFeedAsync();

        Assert.Equal(1, feed.TotalCount);
        var item = feed.Products.Single();
        Assert.Equal("prod-p1", item.PageUniqueId);
        Assert.Equal("shirt", item.ProductUrl.Contains("shirt") ? "shirt" : "");
        Assert.Equal(100_000m, item.PriceToman);
        Assert.True(item.IsAvailability);
        Assert.Equal("https://img/shirt.jpg", item.ImageUrl);
    }

    [Fact]
    public async Task TorobFeed_Availability_False_WhenNoStock()
    {
        var (service, products, _) = Build();
        var outOfStock = SampleProduct("p1", "shirt", "تی‌شرت", 100_000);
        outOfStock.Variants[0].StockQuantity = 0;
        products.Store.Add(outOfStock);

        var feed = await service.GetTorobFeedAsync();

        Assert.False(feed.Products.Single().IsAvailability);
    }

    [Fact]
    public async Task SyncDigikala_NoCredential_Fails()
    {
        var (service, _, _) = Build();

        var result = await service.SyncStockAndPriceWithDigikalaAsync("variant-1", 5, 100_000);

        Assert.False(result.IsSuccess);
        Assert.Contains("دیجی‌کالا", result.Message);
    }

    [Fact]
    public async Task PublishDivar_NoCredential_Fails()
    {
        var (service, _, _) = Build();

        var result = await service.PublishPostOnDivarAsync("عنوان", "توضیح", 100_000, null);

        Assert.False(result.IsSuccess);
        Assert.Contains("دیوار", result.Message);
    }

    [Fact]
    public async Task SyncDigikala_EmptyVariantId_Fails()
    {
        var (service, _, credentials) = Build();
        credentials.AddActiveCredential("digikala", "token");

        var result = await service.SyncStockAndPriceWithDigikalaAsync("", 5, 100_000);

        Assert.False(result.IsSuccess);
    }
}
