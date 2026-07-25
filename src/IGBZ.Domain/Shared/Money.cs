namespace IGBZ.Domain.Shared;

public sealed record Money(decimal Amount, string Currency = "IRR")
{
    public static Money Zero(string currency = "IRR") => new(0, currency);

    public Money EnsureNonNegative(string fieldName)
    {
        if (Amount < 0)
        {
            throw new DomainException($"{fieldName} cannot be negative.");
        }

        return this;
    }
}
