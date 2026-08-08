namespace IGBZ.Application.Orders;

using IGBZ.Domain.Common;
using IGBZ.Domain.Orders;

public class OrderLineInput
{
    public string ProductId { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public int Quantity { get; init; }
}

public class CreateOrderRequest
{
    public string CustomerId { get; init; } = string.Empty;
    public IReadOnlyList<OrderLineInput> Lines { get; init; } = Array.Empty<OrderLineInput>();
    public string? CouponCode { get; init; }
    public Money ShippingToman { get; init; } = Money.Zero;
    public decimal TaxRatePercent { get; init; }
}

/// <summary>موتور سفارش — ساخت سفارش با رزرو اتمیک موجودی و محاسبهٔ قیمت.</summary>
public interface IOrderService
{
    Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(string orderId, CancellationToken cancellationToken = default);
}
