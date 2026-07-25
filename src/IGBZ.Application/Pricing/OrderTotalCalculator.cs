using IGBZ.Domain.Discounts;
using IGBZ.Domain.Shared;

namespace IGBZ.Application.Pricing;

public sealed class OrderTotalCalculator : IOrderTotalCalculator
{
    public PricingBreakdown Calculate(PricingRequest request, IReadOnlyCollection<Discount> discounts)
    {
        if (request.Lines.Count == 0)
        {
            throw new DomainException("Cannot calculate totals for an empty cart.");
        }

        var currency = request.Lines.First().Currency.ToUpperInvariant();
        if (request.Lines.Any(line => !string.Equals(line.Currency, currency, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException("All order lines must use the same currency.");
        }

        var subTotal = request.Lines.Sum(line => line.LineTotal);
        var appliedDiscounts = new List<AppliedDiscountDto>();
        var discountTotal = 0m;

        var matchingDiscount = discounts
            .Where(discount => discount.CanApply(request.CouponCode, request.Now))
            .OrderByDescending(discount => discount.CouponCode is not null)
            .ThenBy(discount => discount.CreatedAtUtc)
            .FirstOrDefault();

        if (matchingDiscount is not null)
        {
            discountTotal = matchingDiscount.Calculate(subTotal);
            if (discountTotal > 0)
            {
                appliedDiscounts.Add(new AppliedDiscountDto(
                    matchingDiscount.Id,
                    matchingDiscount.Name,
                    matchingDiscount.CouponCode,
                    discountTotal));
            }
        }

        var taxableAmount = Math.Max(0, subTotal - discountTotal);
        var taxTotal = Math.Round(taxableAmount * request.VatRate, 2, MidpointRounding.AwayFromZero);
        var shippingTotal = Guard.AgainstNegative(request.ShippingAmount, nameof(request.ShippingAmount));
        var grandTotal = taxableAmount + taxTotal + shippingTotal;

        return new PricingBreakdown(
            subTotal,
            discountTotal,
            taxTotal,
            shippingTotal,
            grandTotal,
            currency,
            appliedDiscounts);
    }
}
