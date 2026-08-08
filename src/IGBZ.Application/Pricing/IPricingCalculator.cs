namespace IGBZ.Application.Pricing;

/// <summary>
/// هر مرحله از پایپلاین قیمت‌گذاری یک calculator مستقل و Unit-Testable است (سند بخش ۷.۲).
/// </summary>
public interface IPricingCalculator
{
    /// <summary>ترتیب اجرا — کوچک‌تر زودتر اجرا می‌شود.</summary>
    int Order { get; }

    string Name { get; }

    Task CalculateAsync(PricingContext context, PricingAccumulator accumulator, CancellationToken cancellationToken);
}
