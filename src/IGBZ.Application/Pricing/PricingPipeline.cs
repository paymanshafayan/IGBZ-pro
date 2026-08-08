namespace IGBZ.Application.Pricing;

using IGBZ.Domain.Common;

public interface IPricingPipeline
{
    /// <summary>اجرای کامل: SubTotal → Discount → Tax → Shipping → GrandTotal.</summary>
    Task<PricingResult> CalculateAsync(PricingContext context, CancellationToken cancellationToken = default);
}

public class PricingPipeline : IPricingPipeline
{
    private readonly IReadOnlyList<IPricingCalculator> _calculators;

    public PricingPipeline(IEnumerable<IPricingCalculator> calculators)
    {
        _calculators = calculators.OrderBy(c => c.Order).ToList();
    }

    public async Task<PricingResult> CalculateAsync(PricingContext context, CancellationToken cancellationToken = default)
    {
        if (context.Lines.Count == 0)
            throw new ArgumentException("سبد خرید خالی است.", nameof(context));

        var accumulator = new PricingAccumulator();
        var errors = new List<string>();

        foreach (var calculator in _calculators)
        {
            try
            {
                await calculator.CalculateAsync(context, accumulator, cancellationToken);
            }
            catch (Exception ex)
            {
                errors.Add($"{calculator.Name}: {ex.Message}");
            }
        }

        // GrandTotal = SubTotal - Discount + Tax + Shipping
        accumulator.GrandTotalToman =
            accumulator.SubTotalToman
            - accumulator.DiscountToman
            + accumulator.TaxToman
            + accumulator.ShippingToman;

        return new PricingResult
        {
            Accumulator = accumulator,
            Errors = errors
        };
    }
}
