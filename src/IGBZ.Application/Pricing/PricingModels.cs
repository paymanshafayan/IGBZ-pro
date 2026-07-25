using IGBZ.Domain.Discounts;

namespace IGBZ.Application.Pricing;

public sealed record PriceLine(
    string ProductId,
    string VariantId,
    string ProductName,
    string VariantName,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    string Currency)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed record PricingRequest(
    IReadOnlyCollection<PriceLine> Lines,
    string? CouponCode,
    decimal ShippingAmount,
    decimal VatRate,
    DateTimeOffset Now);

public sealed record AppliedDiscountDto(string DiscountId, string Name, string? CouponCode, decimal Amount);

public sealed record PricingBreakdown(
    decimal SubTotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal ShippingTotal,
    decimal GrandTotal,
    string Currency,
    IReadOnlyList<AppliedDiscountDto> AppliedDiscounts);

public interface IOrderTotalCalculator
{
    PricingBreakdown Calculate(PricingRequest request, IReadOnlyCollection<Discount> discounts);
}
