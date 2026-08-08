namespace IGBZ.Infrastructure.Repositories;

using IGBZ.Application.Abstractions;
using IGBZ.Application.Discounts;
using IGBZ.Domain.Discounts;

/// <summary>
/// دسترسی MongoDB به کوپن‌ها برای موتور تخفیف.
/// نکته: شمارش واقعی «تعداد استفاده» نیازمند جدول/ردیف‌های استفاده است که در فاز ۲ (به‌همراه
/// ثبت استفاده هنگام اعمال) اضافه می‌شود؛ فعلاً سقف NTimes با مقدار ۰ برمی‌گردد.
/// </summary>
public class MongoDiscountLookup : IDiscountLookup
{
    private readonly ITenantScopedRepository<Discount> _discountRepository;

    public MongoDiscountLookup(ITenantScopedRepository<Discount> discountRepository)
    {
        _discountRepository = discountRepository;
    }

    public Task<Discount?> GetActiveByCodeAsync(string couponCode, CancellationToken cancellationToken = default)
    {
        return _discountRepository.FirstOrDefaultAsync(
            d => d.RequiresCouponCode
                 && d.CouponCode != null
                 && d.CouponCode.ToLower() == couponCode.ToLower()
                 && d.IsActive,
            cancellationToken);
    }

    public Task<int> GetUsageCountAsync(string discountId, string? customerId, CancellationToken cancellationToken = default)
    {
        // فاز ۲: شمارش واقعی از جدول DiscountUsage
        return Task.FromResult(0);
    }
}
