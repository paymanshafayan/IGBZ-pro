namespace IGBZ.Infrastructure.Tests;

using IGBZ.Application.Abstractions;
using IGBZ.Application.Tenancy;
using IGBZ.Domain.Catalog;
using Xunit;

/// <summary>
/// تست اجباری جداسازی چندمستأجری (سند بخش ۵):
/// ۱) Query بدون TenantContext باید Exception بدهد (نه نتیجهٔ خالی).
/// ۲) با TenantContext، فقط دادهٔ همان تننت دیده می‌شود.
/// </summary>
public class TenantIsolationTests : MongoTestBase
{
    [Fact]
    public async Task Query_WithoutTenantContext_ThrowsTenantRequiredException()
    {
        var repository = Provider.GetRequiredService<ITenantScopedRepository<Product>>();
        var tenantContext = Provider.GetRequiredService<ITenantContext>();
        tenantContext.Clear();

        await Assert.ThrowsAsync<TenantRequiredException>(() => repository.FindAsync(p => p.IsPublished));
        await Assert.ThrowsAsync<TenantRequiredException>(() => repository.GetByIdAsync("x"));
        await Assert.ThrowsAsync<TenantRequiredException>(() => repository.InsertAsync(new Product()));
    }

    [Fact]
    public async Task Insert_WithoutTenantContext_Throws()
    {
        var repository = Provider.GetRequiredService<ITenantScopedRepository<Product>>();
        Provider.GetRequiredService<ITenantContext>().Clear();

        await Assert.ThrowsAsync<TenantRequiredException>(() => repository.InsertAsync(new Product { Name = "X" }));
    }

    [Fact]
    public async Task Data_IsScopedPerTenant()
    {
        var repository = Provider.GetRequiredService<ITenantScopedRepository<Product>>();
        var tenantContext = Provider.GetRequiredService<ITenantContext>();

        // تننت ۱ — درج دو محصول
        tenantContext.Set("t1");
        await repository.InsertAsync(new Product { Name = "A" });
        await repository.InsertAsync(new Product { Name = "B" });

        // تننت ۲ — درج یک محصول
        tenantContext.Set("t2");
        await repository.InsertAsync(new Product { Name = "C" });

        // تننت ۱ فقط باید A و B را ببیند
        tenantContext.Set("t1");
        var t1Products = await repository.FindAsync(_ => true);
        Assert.Equal(2, t1Products.Count);
        Assert.All(t1Products, p => Assert.Equal("t1", p.TenantId));

        // تننت ۲ فقط C را می‌بیند
        tenantContext.Set("t2");
        var t2Products = await repository.FindAsync(_ => true);
        Assert.Single(t2Products);
        Assert.Equal("C", t2Products[0].Name);
    }

    [Fact]
    public async Task Insert_WithWrongTenantId_Throws()
    {
        var repository = Provider.GetRequiredService<ITenantScopedRepository<Product>>();
        Provider.GetRequiredService<ITenantContext>().Set("t1");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.InsertAsync(new Product { Name = "X", TenantId = "t2" }));
    }

    [Fact]
    public async Task Update_OnlyAffectsOwnTenant()
    {
        var repository = Provider.GetRequiredService<ITenantScopedRepository<Product>>();
        var tenantContext = Provider.GetRequiredService<ITenantContext>();

        tenantContext.Set("t1");
        await repository.InsertAsync(new Product { Name = "A" });
        var t1Product = (await repository.FindAsync(_ => true)).Single();

        // تننت ۲ نمی‌تواند محصول تننت ۱ را آپدیت کند (فیلتر id+tenant چیزی را پیدا نمی‌کند)
        tenantContext.Set("t2");
        t1Product.Name = "HACKED";
        await repository.UpdateAsync(t1Product); // ReplaceOne با فیلتر id+tenant → 0 تغییر؛ بدون خطا

        tenantContext.Set("t1");
        var after = await repository.GetByIdAsync(t1Product.Id);
        Assert.Equal("A", after!.Name); // دست‌نخورده
    }
}
