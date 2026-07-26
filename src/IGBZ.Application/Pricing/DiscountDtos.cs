using IGBZ.Domain.Discounts;

namespace IGBZ.Application.Pricing;

public sealed record CreateDiscountRequest(string Name, DiscountType Type, decimal Value, string? CouponCode);

public sealed record DiscountResponse(
    string Id,
    string Name,
    DiscountType Type,
    decimal Value,
    string? CouponCode,
    bool IsActive);
