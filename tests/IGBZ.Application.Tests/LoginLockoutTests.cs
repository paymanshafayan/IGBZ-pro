namespace IGBZ.Application.Tests;

using IGBZ.Application.Auth;
using IGBZ.Application.Tenancy;
using IGBZ.Application.Tests.Fakes;
using IGBZ.Domain.Customers;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

public class LoginLockoutTests
{
    private readonly TenantContext _tenantContext = new();

    private (AuthService service, FakeTenantScopedRepository<Customer> customers) Build()
    {
        _tenantContext.Set("t1");
        var customers = new FakeTenantScopedRepository<Customer>(_tenantContext);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new AuthService(customers, new FakeJwtTokenService(), cache);
        return (service, customers);
    }

    [Fact]
    public async Task Login_AfterMaxFailedAttempts_IsLocked()
    {
        var (service, customers) = Build();
        customers.Store.Add(new Customer
        {
            Id = "c1",
            TenantId = "t1",
            Email = "a@b.ir",
            PasswordHash = PasswordHasher.Hash("correct-password"),
            IsActive = true
        });

        // ۵ تلاش ناموفق
        for (var i = 0; i < 5; i++)
        {
            var fail = await service.LoginAsync("t1", "a@b.ir", "wrong");
            Assert.False(fail.Success);
        }

        // تلاش ششم حتی با رمز درست قفل است
        var locked = await service.LoginAsync("t1", "a@b.ir", "correct-password");
        Assert.False(locked.Success);
        Assert.Contains("بیش از حد", locked.ErrorMessage);
    }

    [Fact]
    public async Task Login_CorrectPassword_ResetsCounter()
    {
        var (service, customers) = Build();
        customers.Store.Add(new Customer
        {
            Id = "c1",
            TenantId = "t1",
            Email = "a@b.ir",
            PasswordHash = PasswordHasher.Hash("correct"),
            IsActive = true
        });

        // دو تلاش ناموفق، سپس موفق → شمارنده پاک می‌شود
        await service.LoginAsync("t1", "a@b.ir", "wrong");
        await service.LoginAsync("t1", "a@b.ir", "wrong");

        var success = await service.LoginAsync("t1", "a@b.ir", "correct");
        Assert.True(success.Success);

        // بعد از موفقیت، دوباره ۵ تلاش ناموفق می‌تواند انجام شود (شمارنده صفر شده)
        for (var i = 0; i < 5; i++)
        {
            await service.LoginAsync("t1", "a@b.ir", "wrong");
        }
        var lockedAgain = await service.LoginAsync("t1", "a@b.ir", "correct");
        Assert.False(lockedAgain.Success);
    }
}
