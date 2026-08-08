namespace IGBZ.Domain.Orders;

/// <summary>
/// Aggregate سفارش — ماشین‌حالت: Pending → Paid → Processing → Shipped → Delivered
/// (با شاخه‌های Cancel و Return/Refunded). تغییر وضعیت فقط از طریق متدهای صریح انجام می‌شود؛
/// هرگز نوشتن مستقیم فیلد Status (سند بخش ۶.۳ و ۷.۱).
/// </summary>
public class Order : Entity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;

    public OrderStatus Status { get; private set; } = OrderStatus.Pending;

    private readonly List<OrderItem> _items = new();
    public IReadOnlyList<OrderItem> Items => _items;

    // ── قیمت‌گذاری (محاسبه شده توسط پایپلاین — فقط از طریق ApplyPricing) ──
    public Money SubTotalToman { get; private set; } = Money.Zero;
    public Money DiscountToman { get; private set; } = Money.Zero;
    public Money TaxToman { get; private set; } = Money.Zero;
    public Money ShippingToman { get; private set; } = Money.Zero;
    public Money GrandTotalToman { get; private set; } = Money.Zero;

    private readonly List<string> _appliedCouponCodes = new();
    public IReadOnlyList<string> AppliedCouponCodes => _appliedCouponCodes;

    public OrderPayment Payment { get; private set; } = new();
    public OrderShipping? Shipping { get; private set; }

    private readonly List<OrderStatusHistoryEntry> _statusHistory = new();
    public IReadOnlyList<OrderStatusHistoryEntry> StatusHistory => _statusHistory;

    // ────────────────────────── ساخت ──────────────────────────

    public void AddItem(string productId, string sku, int quantity, Money unitPriceToman)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "تعداد باید مثبت باشد.");
        if (unitPriceToman.Toman < 0)
            throw new ArgumentException("قیمت واحد نمی‌تواند منفی باشد.", nameof(unitPriceToman));

        EnsureStatus("افزودن آیتم فقط در وضعیت در انتظار ممکن است.", OrderStatus.Pending);

        var existing = _items.FirstOrDefault(i => i.ProductId == productId && i.Sku == sku);
        if (existing != null)
            existing.Quantity += quantity;
        else
            _items.Add(new OrderItem
            {
                ProductId = productId,
                Sku = sku,
                Quantity = quantity,
                UnitPriceToman = unitPriceToman.Toman
            });
    }

    /// <summary>
    /// اعمال نتیجهٔ پایپلاین قیمت‌گذاری (SubTotal → Discount → Tax → Shipping → GrandTotal).
    /// فقط در وضعیت Pending قابل فراخوانی است.
    /// </summary>
    public void ApplyPricing(
        Money subTotalToman,
        Money discountToman,
        Money taxToman,
        Money shippingToman,
        IReadOnlyList<string> appliedCouponCodes)
    {
        EnsureStatus("اعمال قیمت‌گذاری فقط در وضعیت در انتظار ممکن است.", OrderStatus.Pending);

        if (discountToman.Toman > subTotalToman.Toman)
            throw new InvalidOperationException("تخفیف نمی‌تواند از مبلغ سبد بیشتر باشد.");

        SubTotalToman = subTotalToman;
        DiscountToman = discountToman;
        TaxToman = taxToman;
        ShippingToman = shippingToman;
        GrandTotalToman = subTotalToman - discountToman + taxToman + shippingToman;

        _appliedCouponCodes.Clear();
        _appliedCouponCodes.AddRange(appliedCouponCodes);
        UpdatedOnUtc = DateTime.UtcNow;
    }

    // ────────────────────────── ماشین‌حالت ──────────────────────────

    public void MarkAsPaid(string gatewayName, string trackingNumber, string? bankRefId)
    {
        EnsureStatus("فقط سفارش در انتظار را می‌توان پرداخت‌شده کرد.", OrderStatus.Pending);
        if (string.IsNullOrWhiteSpace(gatewayName) || string.IsNullOrWhiteSpace(trackingNumber))
            throw new ArgumentException("درگاه و کد پیگیری برای پرداخت الزامی است.");

        Status = OrderStatus.Paid;
        Payment = new OrderPayment
        {
            GatewayName = gatewayName,
            TrackingNumber = trackingNumber,
            BankRefId = bankRefId,
            PaidOnUtc = DateTime.UtcNow
        };
        AddHistory();
    }

    public void MarkAsProcessing()
    {
        EnsureStatus("فقط سفارش پرداخت‌شده را می‌توان در حال پردازش کرد.", OrderStatus.Paid);
        Status = OrderStatus.Processing;
        AddHistory();
    }

    public void Ship(string carrier, string? trackingCode, string? deliveryPin)
    {
        EnsureStatus("فقط سفارش در حال پردازش را می‌توان ارسال کرد.", OrderStatus.Processing);
        if (string.IsNullOrWhiteSpace(carrier))
            throw new ArgumentException("نام حمل‌کننده الزامی است.", nameof(carrier));

        Status = OrderStatus.Shipped;
        Shipping = new OrderShipping
        {
            Carrier = carrier,
            TrackingCode = trackingCode,
            DeliveryPin = deliveryPin,
            ShippedOnUtc = DateTime.UtcNow
        };
        AddHistory();
    }

    public void Deliver()
    {
        EnsureStatus("فقط سفارش ارسال‌شده را می‌توان تحویل‌شده کرد.", OrderStatus.Shipped);
        Status = OrderStatus.Delivered;
        AddHistory();
    }

    public void Cancel(string reason)
    {
        EnsureStatus("فقط سفارش در انتظار یا پرداخت‌شده را می‌توان لغو کرد.", OrderStatus.Pending, OrderStatus.Paid);
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("دلیل لغو الزامی است.", nameof(reason));

        Status = OrderStatus.Cancelled;
        AddHistory();
    }

    public void Return()
    {
        EnsureStatus("فقط سفارش تحویل‌شده را می‌توان مرجوع کرد.", OrderStatus.Delivered);
        Status = OrderStatus.Returned;
        AddHistory();
    }

    // ────────────────────────── ابزار ──────────────────────────

    private void EnsureStatus(params OrderStatus[] allowed)
    {
        if (!allowed.Contains(Status))
            throw new InvalidOrderStateException(Status, allowed);
    }

    private void EnsureStatus(string message, params OrderStatus[] allowed)
    {
        if (!allowed.Contains(Status))
            throw new InvalidOrderStateException(Status, allowed, message);
    }

    private void AddHistory() =>
        _statusHistory.Add(new OrderStatusHistoryEntry { Status = Status, AtUtc = DateTime.UtcNow });
}

public enum OrderStatus
{
    Pending = 0,
    Paid = 10,
    Processing = 20,
    Shipped = 30,
    Delivered = 40,
    Cancelled = 50,
    Returned = 60
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPriceToman { get; set; }
}

public class OrderPayment
{
    public string GatewayName { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public string? BankRefId { get; set; }
    public DateTime? PaidOnUtc { get; set; }
}

public class OrderShipping
{
    public string Carrier { get; set; } = string.Empty;
    public string? TrackingCode { get; set; }
    public string? DeliveryPin { get; set; }
    public DateTime? ShippedOnUtc { get; set; }
}

public class OrderStatusHistoryEntry
{
    public OrderStatus Status { get; set; }
    public DateTime AtUtc { get; set; }
}
