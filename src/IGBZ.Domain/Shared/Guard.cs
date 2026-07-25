namespace IGBZ.Domain.Shared;

public static class Guard
{
    public static string AgainstEmpty(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{name} is required.");
        }

        return value.Trim();
    }

    public static decimal AgainstNegative(decimal value, string name)
    {
        if (value < 0)
        {
            throw new DomainException($"{name} cannot be negative.");
        }

        return value;
    }

    public static int AgainstNonPositive(int value, string name)
    {
        if (value <= 0)
        {
            throw new DomainException($"{name} must be greater than zero.");
        }

        return value;
    }
}
