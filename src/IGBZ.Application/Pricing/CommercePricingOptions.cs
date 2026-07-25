namespace IGBZ.Application.Pricing;

public sealed class CommercePricingOptions
{
    /// <summary>Default Iranian VAT rate. 0.09 means 9%.</summary>
    public decimal VatRate { get; init; } = 0.09m;

    /// <summary>Default reservation lifetime for checkout stock reservations.</summary>
    public TimeSpan ReservationTtl { get; init; } = TimeSpan.FromMinutes(20);
}
