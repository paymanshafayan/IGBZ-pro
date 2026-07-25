namespace IGBZ.Domain.Orders;

public sealed class OrderStatusHistoryEntry
{
    private OrderStatusHistoryEntry()
    {
    }

    public OrderStatusHistoryEntry(OrderStatus from, OrderStatus to, string reason, DateTimeOffset changedAtUtc)
    {
        From = from;
        To = to;
        Reason = reason;
        ChangedAtUtc = changedAtUtc;
    }

    public OrderStatus From { get; private set; }
    public OrderStatus To { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset ChangedAtUtc { get; private set; }
}
