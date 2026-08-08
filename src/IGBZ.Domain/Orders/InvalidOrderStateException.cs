namespace IGBZ.Domain.Orders;

/// <summary>انتقال نامعتبر در ماشین‌حالت سفارش.</summary>
public class InvalidOrderStateException : InvalidOperationException
{
    public OrderStatus Current { get; }
    public IReadOnlyList<OrderStatus> Allowed { get; }

    public InvalidOrderStateException(OrderStatus current, IReadOnlyList<OrderStatus> allowed, string? message = null)
        : base(message ?? $"انتقال وضعیت سفارش از «{current}» به وضعیت‌های مجاز ({string.Join(", ", allowed)}) مجاز نیست.")
    {
        Current = current;
        Allowed = allowed;
    }
}
