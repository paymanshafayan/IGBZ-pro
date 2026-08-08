namespace IGBZ.Domain.Common;

/// <summary>
/// پول به تومان — Value Object با قواعد: غیرمنفی، بدون رقم اعشار.
/// تمام محاسبات مالی دامنه از این نوع عبور می‌کنند (نه decimal خام).
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Toman { get; }

    public Money(decimal toman)
    {
        if (toman < 0)
            throw new ArgumentOutOfRangeException(nameof(toman), "مبلغ نمی‌تواند منفی باشد.");
        Toman = decimal.Round(toman, 0, MidpointRounding.AwayFromZero);
    }

    public static Money Zero => new(0);

    public static Money operator +(Money a, Money b) => new(a.Toman + b.Toman);

    public static Money operator -(Money a, Money b)
    {
        var result = a.Toman - b.Toman;
        if (result < 0)
            throw new InvalidOperationException("نتیجهٔ تفریق مبلغ منفی شد.");
        return new Money(result);
    }

    public static Money operator *(Money a, decimal factor)
    {
        if (factor < 0)
            throw new ArgumentOutOfRangeException(nameof(factor), "ضریب نمی‌تواند منفی باشد.");
        return new Money(a.Toman * factor);
    }

    public bool IsZero => Toman == 0;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Toman;
    }

    public override string ToString() => $"{Toman:N0} تومان";
}
