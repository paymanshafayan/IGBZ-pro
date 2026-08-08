namespace IGBZ.Application.Tests;

using IGBZ.Application.Discounts;
using IGBZ.Application.Orders;
using IGBZ.Application.Pricing;
using IGBZ.Application.Tenancy;
using IGBZ.Application.Tests.Fakes;
using IGBZ.Domain.Catalog;
using IGBZ.Domain.Common;
using IGBZ.Domain.Orders;
using Xunit;

public class OrderServiceTests
{
    private readonly TenantContext _tenantContext = new();

    private (OrderService service, FakeProductInventoryRepository inventory, FakeTenantScopedRepository<Order> orders) Build()
    {
        _tenantContext.Set("t1");

        var inventory = new FakeProductInventoryRepository(_tenantContext);
        var orders = new FakeTenantScopedRepository<Order>(_tenantContext);

        var calculators = new List<IPricingCalculator>
        {
            new SubtotalCalculator(),
            new DiscountCalculator(new DiscountEngine(new FakeDiscountLookup())),
            new TaxCalculator(),
            new ShippingCalculator()
        };
        var pipeline = new PricingPipeline(calculators);

        return (new OrderService(inventory, orders, pipeline), inventory, orders);
    }

    private static Product SampleProduct(string id, string sku, decimal price, int stock = 10)
    {
        return new Product
        {
            Id = id,
            TenantId = "t1",
            Name = $"محصول {sku}",
            Slug = sku.ToLowerInvariant(),
            Variants = { new ProductVariant { Sku = sku, PriceToman = price, StockQuantity = stock } }
        };
    }

    [Fact]
    public async Task CreateOrder_ReservesStock_AndComputesPricing()
    {
        var (service, inventory, orders) = Build();
        inventory.Store.Add(SampleProduct("p1", "SKU-1", 100_000, stock: 5));

        var order = await service.CreateOrderAsync(new CreateOrderRequest
        {
            CustomerId = "c1",
            TaxRatePercent = 9,
            Lines = new[] { new OrderLineInput { ProductId = "p1", Sku = "SKU-1", Quantity = 2 } }
        });

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(200_000m, order.SubTotalToman.Toman);
        Assert.Equal(18_000m, order.TaxToman.Toman);          // 9% از 200k
        Assert.Equal(218_000m, order.GrandTotalToman.Toman);

        // موجودی رزرو شد
        Assert.Equal(2, inventory.Store.Single().Variants.Single().ReservedQuantity);
        Assert.Single(orders.Store);
    }

    [Fact]
    public async Task CreateOrder_WhenInsufficientStock_Throws_AndNoOrderSaved()
    {
        var (service, inventory, orders) = Build();
        inventory.Store.Add(SampleProduct("p1", "SKU-1", 100_000, stock: 1));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateOrderAsync(new CreateOrderRequest
        {
            CustomerId = "c1",
            Lines = new[] { new OrderLineInput { ProductId = "p1", Sku = "SKU-1", Quantity = 5 } }
        }));

        Assert.Empty(orders.Store);
        Assert.Equal(0, inventory.Store.Single().Variants.Single().ReservedQuantity);
    }

    [Fact]
    public async Task CreateOrder_UnknownProduct_Throws()
    {
        var (service, _, _) = Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateOrderAsync(new CreateOrderRequest
        {
            CustomerId = "c1",
            Lines = new[] { new OrderLineInput { ProductId = "p999", Sku = "SKU-1", Quantity = 1 } }
        }));
    }

    [Fact]
    public async Task CreateOrder_EmptyLines_Throws()
    {
        var (service, _, _) = Build();

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateOrderAsync(new CreateOrderRequest
        {
            CustomerId = "c1",
            Lines = Array.Empty<OrderLineInput>()
        }));
    }

    [Fact]
    public async Task CreateOrder_RequiresTenantContext()
    {
        var (service, inventory, _) = Build();
        inventory.Store.Add(SampleProduct("p1", "SKU-1", 100_000));

        _tenantContext.Clear();

        await Assert.ThrowsAsync<IGBZ.Application.Tenancy.TenantRequiredException>(() =>
            service.CreateOrderAsync(new CreateOrderRequest
            {
                CustomerId = "c1",
                Lines = new[] { new OrderLineInput { ProductId = "p1", Sku = "SKU-1", Quantity = 1 } }
            }));
    }
}
