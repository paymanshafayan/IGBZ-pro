namespace IGBZ.Application.Discounts;

using IGBZ.Domain.Discounts;

/// <summary>
/// دسترسی به کوپن‌ها برای موتور تخفیف. پیاده‌سازی واقعی روی MongoDB است؛
/// در تست‌ها از Fake استفاده می‌شود.
/// </summary>
public interface IDiscountLookup
{
    Task<Discount?> GetActiveByCodeAsync(string couponCode, CancellationToken cancellationToken = default);

    /// <summary>تعداد دفعات استفاده — برای سقف NTimesOnly/NTimesPerCustomer.</summary>
    Task<int> GetUsageCountAsync(string discountId, string? customerId, CancellationToken cancellationToken = default);
}
