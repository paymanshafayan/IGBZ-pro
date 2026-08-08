namespace IGBZ.Application.Tests;

using IGBZ.Application.Logistics;
using IGBZ.Application.Tenancy;
using IGBZ.Application.Tests.Fakes;
using Xunit;

public class LogisticsServiceTests
{
    private readonly TenantContext _tenantContext = new();

    private LogisticsService Build()
    {
        _tenantContext.Set("t1");
        var credentials = new FakeCredentialRepository(_tenantContext);
        // برای تست دسته‌بندی/PIN به HttpClient واقعی نیاز نیست — سرویس را بدون Credential می‌سازیم
        return new LogisticsService(credentials, new FakeHttpClientFactory());
    }

    [Fact]
    public void CategorizeRoute_HeavyFreight_ForHeavyWeight()
    {
        var service = Build();
        var route = service.CategorizeRoute(40, "شیراز", false);
        Assert.Equal("HEAVY_FREIGHT", route.RouteType);
        Assert.True(route.DeliveryPinRequired);
    }

    [Fact]
    public void CategorizeRoute_Express_ForTehranOrExpress()
    {
        var service = Build();
        var tehran = service.CategorizeRoute(5, "تهران", false);
        Assert.Equal("EXPRESS_COURIER", tehran.RouteType);

        var express = service.CategorizeRoute(5, "اصفهان", true);
        Assert.Equal("EXPRESS_COURIER", express.RouteType);
    }

    [Fact]
    public void CategorizeRoute_NationalPost_Otherwise()
    {
        var service = Build();
        var route = service.CategorizeRoute(5, "اصفهان", false);
        Assert.Equal("NATIONAL_POST", route.RouteType);
        Assert.False(route.DeliveryPinRequired);
    }

    [Fact]
    public void GenerateDeliveryPin_IsFourDigits()
    {
        var service = Build();
        for (var i = 0; i < 20; i++)
        {
            var pin = service.GenerateDeliveryPin();
            Assert.Equal(4, pin.Length);
            Assert.True(int.TryParse(pin, out _));
        }
    }

    [Fact]
    public void GenerateDeliveryPin_IsNotPredictableSequence()
    {
        var service = Build();
        var pins = Enumerable.Range(0, 10).Select(_ => service.GenerateDeliveryPin()).Distinct();
        Assert.True(pins.Count() >= 2, "PINها نباید تکراری/قابل‌پیش‌بینی باشند.");
    }

    [Fact]
    public async Task RegisterShipment_NoCredential_Fails()
    {
        var service = Build();

        var result = await service.RegisterShipmentAsync("o1", 5, "تهران", "آدرس", "0912", false);

        Assert.False(result.IsSuccess);
        Assert.Contains("تاپین", result.Message);
    }
}

/// <summary>HttpClientFactory ساختگی (برای ساخت سرویس بدون شبکه).</summary>
public class FakeHttpClientFactory : System.Net.Http.IHttpClientFactory
{
    public System.Net.Http.HttpClient CreateClient(string name) => new();
}
