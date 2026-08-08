namespace IGBZ.Application.Tests;

using IGBZ.Application.Payments;
using IGBZ.Application.Tenancy;
using IGBZ.Application.Tests.Fakes;
using IGBZ.Domain.Common;
using IGBZ.Domain.Payments;
using Xunit;

public class PaymentServiceTests
{
    private readonly TenantContext _tenantContext = new();

    private (PaymentService service, FakeTenantScopedRepository<PaymentTransactionLedger> ledger, FakePaymentGateway gateway) Build()
    {
        _tenantContext.Set("t1");
        var ledger = new FakeTenantScopedRepository<PaymentTransactionLedger>(_tenantContext);
        var gateway = new FakePaymentGateway("fake");
        var service = new PaymentService(ledger, new[] { gateway });
        return (service, ledger, gateway);
    }

    [Fact]
    public async Task Request_ThenVerify_Success()
    {
        var (service, ledger, gateway) = Build();

        var request = await service.RequestAsync("order-1", "fake", new Money(100_000), "https://callback.test");

        Assert.True(request.IsSuccess);
        Assert.NotNull(request.RedirectUrl);

        var verify = await service.VerifyAsync(request.TrackingNumber!, new Money(100_000));

        Assert.True(verify.IsSuccess);
        Assert.False(verify.AlreadyProcessed);
        Assert.NotNull(verify.BankRefId);

        var entry = ledger.Store.Single();
        Assert.Equal(PaymentTransactionState.VerifiedSuccess, entry.State);
    }

    [Fact]
    public async Task Verify_SecondTime_IsAlreadyProcessed_NoDoubleCredit()
    {
        var (service, _, gateway) = Build();

        var request = await service.RequestAsync("order-1", "fake", new Money(100_000), "https://callback.test");
        await service.VerifyAsync(request.TrackingNumber!, new Money(100_000));
        var second = await service.VerifyAsync(request.TrackingNumber!, new Money(100_000));

        Assert.True(second.IsSuccess);
        Assert.True(second.AlreadyProcessed);
        Assert.Equal(1, gateway.VerifyCallCount); // درگاه فقط یک بار فراخوانی شد
    }

    [Fact]
    public async Task Verify_Fails_WhenGatewayRejects()
    {
        var (service, _, gateway) = Build();
        gateway.ShouldFailVerify = true;

        var request = await service.RequestAsync("order-1", "fake", new Money(100_000), "https://callback.test");
        var verify = await service.VerifyAsync(request.TrackingNumber!, new Money(100_000));

        Assert.False(verify.IsSuccess);
    }

    [Fact]
    public async Task Verify_Fails_WhenAmountMismatch()
    {
        var (service, _, gateway) = Build();
        gateway.ConfirmedAmountToman = 90_000; // بانک مبلغ دیگری تایید کرده

        var request = await service.RequestAsync("order-1", "fake", new Money(100_000), "https://callback.test");
        var verify = await service.VerifyAsync(request.TrackingNumber!, new Money(100_000));

        Assert.False(verify.IsSuccess);
    }

    [Fact]
    public async Task Request_UnknownGateway_Fails()
    {
        var (service, _, _) = Build();

        var request = await service.RequestAsync("order-1", "nope", new Money(100_000), "https://callback.test");

        Assert.False(request.IsSuccess);
        Assert.Contains("پشتیبانی نمی", request.ErrorMessage);
    }

    [Fact]
    public async Task Verify_UnknownTracking_Fails()
    {
        var (service, _, _) = Build();

        var verify = await service.VerifyAsync("does-not-exist", new Money(100_000));

        Assert.False(verify.IsSuccess);
    }
}
