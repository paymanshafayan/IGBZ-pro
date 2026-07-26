using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Orders;

public sealed class Order : TenantScopedEntity
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Paid, OrderStatus.Cancelled],
        [OrderStatus.Paid] = [OrderStatus.Processing, OrderStatus.Cancelled, OrderStatus.Refunded],
        [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Delivered, OrderStatus.Returned],
        [OrderStatus.Delivered] = [OrderStatus.Returned, OrderStatus.Refunded],
        [OrderStatus.Returned] = [OrderStatus.Refunded],
        [OrderStatus.Cancelled] = [],
        [OrderStatus.Refunded] = []
    };

    private Order()
    {
    }

    private Order(
        string tenantId,
        string orderNumber,
        OrderCustomerSnapshot customer,
        Address shippingAddress,
        IReadOnlyCollection<OrderItem> items,
        IReadOnlyCollection<string> stockReservationIds,
        decimal subTotal,
        decimal discountTotal,
        decimal taxTotal,
        decimal shippingTotal,
        decimal grandTotal,
        string currency,
        string? couponCode,
        DateTimeOffset now)
        : base(tenantId, now)
    {
        if (items.Count == 0)
        {
            throw new DomainException("Order must contain at least one item.");
        }

        OrderNumber = Guard.AgainstEmpty(orderNumber, nameof(orderNumber));
        Customer = customer;
        ShippingAddress = shippingAddress;
        Items = items.ToList();
        StockReservationIds = stockReservationIds.ToList();
        SubTotal = Guard.AgainstNegative(subTotal, nameof(subTotal));
        DiscountTotal = Guard.AgainstNegative(discountTotal, nameof(discountTotal));
        TaxTotal = Guard.AgainstNegative(taxTotal, nameof(taxTotal));
        ShippingTotal = Guard.AgainstNegative(shippingTotal, nameof(shippingTotal));
        GrandTotal = Guard.AgainstNegative(grandTotal, nameof(grandTotal));
        Currency = Guard.AgainstEmpty(currency, nameof(currency)).ToUpperInvariant();
        CouponCode = couponCode?.Trim().ToUpperInvariant();
        Status = OrderStatus.Pending;
    }

    public string OrderNumber { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public OrderCustomerSnapshot Customer { get; private set; } = new(null, "unknown", "unknown", null);
    public Address ShippingAddress { get; private set; } = new("unknown", "unknown", "unknown", "unknown", "unknown");
    public List<OrderItem> Items { get; private set; } = [];
    public List<string> StockReservationIds { get; private set; } = [];
    public decimal SubTotal { get; private set; }
    public decimal DiscountTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal ShippingTotal { get; private set; }
    public decimal GrandTotal { get; private set; }
    public string Currency { get; private set; } = "IRR";
    public string? CouponCode { get; private set; }
    public string? PaymentTransactionId { get; private set; }
    public DateTimeOffset? PaidAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public List<OrderStatusHistoryEntry> StatusHistory { get; private set; } = [];

    public static Order Create(
        string tenantId,
        string orderNumber,
        OrderCustomerSnapshot customer,
        Address shippingAddress,
        IReadOnlyCollection<OrderItem> items,
        IReadOnlyCollection<string> stockReservationIds,
        decimal subTotal,
        decimal discountTotal,
        decimal taxTotal,
        decimal shippingTotal,
        decimal grandTotal,
        string currency,
        string? couponCode,
        DateTimeOffset now)
        => new(
            tenantId,
            orderNumber,
            customer,
            shippingAddress,
            items,
            stockReservationIds,
            subTotal,
            discountTotal,
            taxTotal,
            shippingTotal,
            grandTotal,
            currency,
            couponCode,
            now);

    public void MarkAsPaid(string paymentTransactionId, DateTimeOffset now)
    {
        Guard.AgainstEmpty(paymentTransactionId, nameof(paymentTransactionId));
        if (Status == OrderStatus.Paid)
        {
            if (PaymentTransactionId == paymentTransactionId)
            {
                return;
            }

            throw new DomainException("Order has already been paid with a different transaction.");
        }

        ChangeStatus(OrderStatus.Paid, "Payment captured", now);
        PaymentTransactionId = paymentTransactionId;
        PaidAtUtc = now;
    }

    public void StartProcessing(DateTimeOffset now)
        => ChangeStatus(OrderStatus.Processing, "Processing started", now);

    public void MarkAsShipped(DateTimeOffset now)
        => ChangeStatus(OrderStatus.Shipped, "Order shipped", now);

    public void MarkAsDelivered(DateTimeOffset now)
        => ChangeStatus(OrderStatus.Delivered, "Order delivered", now);

    public void Cancel(string reason, DateTimeOffset now)
    {
        CancellationReason = Guard.AgainstEmpty(reason, nameof(reason));
        ChangeStatus(OrderStatus.Cancelled, CancellationReason, now);
    }

    public void Return(string reason, DateTimeOffset now)
        => ChangeStatus(OrderStatus.Returned, Guard.AgainstEmpty(reason, nameof(reason)), now);

    public void Refund(string reason, DateTimeOffset now)
        => ChangeStatus(OrderStatus.Refunded, Guard.AgainstEmpty(reason, nameof(reason)), now);

    private void ChangeStatus(OrderStatus newStatus, string reason, DateTimeOffset now)
    {
        if (newStatus == Status)
        {
            return;
        }

        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
        {
            throw new DomainException($"Order status cannot transition from {Status} to {newStatus}.");
        }

        var previous = Status;
        Status = newStatus;
        StatusHistory.Add(new OrderStatusHistoryEntry(previous, newStatus, reason, now));
        Touch(now);
    }
}
