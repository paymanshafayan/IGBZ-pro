namespace IGBZ.Application.Tests.Fakes;

using IGBZ.Application.Discounts;
using IGBZ.Domain.Discounts;

public class FakeDiscountLookup : IDiscountLookup
{
    public List<Discount> Discounts { get; } = new();

    public Dictionary<string, int> UsageCounts { get; } = new();

    public Task<Discount?> GetActiveByCodeAsync(string couponCode, CancellationToken cancellationToken = default)
    {
        var discount = Discounts.FirstOrDefault(d =>
            d.RequiresCouponCode
            && string.Equals(d.CouponCode, couponCode, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(discount);
    }

    public Task<int> GetUsageCountAsync(string discountId, string? customerId, CancellationToken cancellationToken = default)
    {
        var key = $"{discountId}|{customerId}";
        return Task.FromResult(UsageCounts.TryGetValue(key, out var count) ? count : 0);
    }
}
