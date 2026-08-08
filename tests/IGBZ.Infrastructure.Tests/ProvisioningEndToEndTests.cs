namespace IGBZ.Infrastructure.Tests;

using IGBZ.Application;
using IGBZ.Application.Auth;
using IGBZ.Application.Provisioning;
using IGBZ.Application.Subscriptions;
using IGBZ.Application.Tenancy;
using IGBZ.Domain.Plans;
using IGBZ.Domain.Tenants;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// سناریوی end-to-end فاز ۲ (سند بخش ۱۱):
/// ثبت‌نام فروشگاه (ساخت Tenant + مالک + اشتراک PendingPayment) →
/// درخواست پرداخت از درگاه تست → تایید پرداخت → فعال‌سازی خودکار اشتراک و فروشگاه.
/// </summary>
public class ProvisioningEndToEndTests : MongoTestBase
{
    protected override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // سرویس‌های لایهٔ اپلیکیشن را هم به DI اضافه کن (MongoTestBase فقط Infrastructure را دارد)
        RebuildProvider(services => services.AddIgBzApplication());

        // یک پلن فعال (پولی) در دیتابیس
        var planRepository = Provider.GetRequiredService<IGBZ.Application.Abstractions.IRepository<TenantPlan>>();
        await planRepository.InsertAsync(new TenantPlan
        {
            Name = "نقره‌ای",
            SystemName = "silver",
            PriceMonthlyToman = 890_000,
            PriceSixMonthsToman = 4_800_000,
            PriceYearlyToman = 8_900_000,
            DisplayOrder = 10,
            IsActive = true
        });
    }

    [Fact]
    public async Task Signup_Payment_Verify_ActivatesStore()
    {
        var tenantContext = Provider.GetRequiredService<ITenantContext>();
        var provisioning = Provider.GetRequiredService<ITenantProvisioningService>();
        var subscriptions = Provider.GetRequiredService<ITenantSubscriptionService>();

        // ۱) ثبت‌نام فروشگاه
        var signup = await provisioning.ProvisionAsync(new ProvisionTenantRequest
        {
            Subdomain = "endtoend",
            StoreName = "فروشگاه End-to-End",
            AdminEmail = "owner@e2e.ir",
            AdminPassword = "secret-123"
        });

        Assert.True(signup.Success);
        Assert.True(signup.RequiresPayment);

        // ۲) درخواست پرداخت (در بافت تننت جدید)
        tenantContext.Set(signup.TenantId!);
        var payment = await subscriptions.RequestPaymentAsync(signup.TenantId!, "test", "https://cb.test");

        Assert.True(payment.IsSuccess);
        Assert.NotNull(payment.RedirectUrl);
        Assert.NotNull(payment.TrackingNumber);

        // ۳) تایید پرداخت → فعال‌سازی
        var verify = await subscriptions.VerifyAndActivateAsync(signup.TenantId!, payment.TrackingNumber!);
        Assert.True(verify.IsSuccess);

        // ۴) بررسی وضعیت نهایی
        var status = await subscriptions.GetStatusAsync(signup.TenantId!);
        Assert.True(status.HasSubscription);
        Assert.True(status.IsUsable);
        Assert.Equal(nameof(SubscriptionStatus.Active), status.Status);

        var tenant = (await Provider.GetRequiredService<IGBZ.Application.Abstractions.IRepository<Tenant>>()
            .FindAsync(t => t.TenantId == signup.TenantId)).Single();
        Assert.Equal(TenantStatus.Active, tenant.Status);
    }

    [Fact]
    public async Task Signup_ThenVerify_Fails_WithoutPaymentRequest()
    {
        var tenantContext = Provider.GetRequiredService<ITenantContext>();
        var provisioning = Provider.GetRequiredService<ITenantProvisioningService>();
        var subscriptions = Provider.GetRequiredService<ITenantSubscriptionService>();

        var signup = await provisioning.ProvisionAsync(new ProvisionTenantRequest
        {
            Subdomain = "nopay",
            StoreName = "بدون پرداخت",
            AdminEmail = "a@nopay.ir",
            AdminPassword = "secret-123"
        });

        tenantContext.Set(signup.TenantId!);

        var verify = await subscriptions.VerifyAndActivateAsync(signup.TenantId!, "fake-tracking");
        Assert.False(verify.IsSuccess);

        var status = await subscriptions.GetStatusAsync(signup.TenantId!);
        Assert.False(status.IsUsable); // هنوز فعال نشده
    }
}
