using IGBZ.Application.Abstractions;
using IGBZ.Domain.Discounts;
using IGBZ.Domain.Shared;

namespace IGBZ.Application.Pricing;

public sealed class DiscountService(
    ITenantContextAccessor tenantContextAccessor,
    ITenantScopedRepository<Discount> discounts,
    IClock clock)
{
    public async Task<DiscountResponse> CreateAsync(CreateDiscountRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.RequiredTenantId;
        var now = clock.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var code = request.CouponCode.Trim().ToUpperInvariant();
            var existing = await discounts.FirstOrDefaultAsync(discount => discount.CouponCode == code, cancellationToken);
            if (existing is not null)
            {
                throw new DomainException("A discount with this coupon code already exists.");
            }

            var coupon = Discount.CreateCoupon(tenantId, request.Name, request.Type, request.Value, code, now);
            await discounts.AddAsync(coupon, cancellationToken);
            return ToResponse(coupon);
        }

        var automatic = Discount.CreateAutomatic(tenantId, request.Name, request.Type, request.Value, now);
        await discounts.AddAsync(automatic, cancellationToken);
        return ToResponse(automatic);
    }

    public async Task<IReadOnlyList<DiscountResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = await discounts.ListAsync(null, cancellationToken);
        return result.OrderByDescending(discount => discount.CreatedAtUtc).Select(ToResponse).ToList();
    }

    public static DiscountResponse ToResponse(Discount discount)
        => new(discount.Id, discount.Name, discount.Type, discount.Value, discount.CouponCode, discount.IsActive);
}
