namespace IGBZ.Application.Orders;

using IGBZ.Application.Abstractions;
using IGBZ.Application.Pricing;
using IGBZ.Domain.Catalog;
using IGBZ.Domain.Common;
using IGBZ.Domain.Orders;

public class OrderService : IOrderService
{
    private readonly IProductInventoryRepository _inventoryRepository;
    private readonly ITenantScopedRepository<Order> _orderRepository;
    private readonly IPricingPipeline _pricingPipeline;

    public OrderService(
        IProductInventoryRepository inventoryRepository,
        ITenantScopedRepository<Order> orderRepository,
        IPricingPipeline pricingPipeline)
    {
        _inventoryRepository = inventoryRepository;
        _orderRepository = orderRepository;
        _pricingPipeline = pricingPipeline;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerId))
            throw new ArgumentException("شناسهٔ مشتری الزامی است.", nameof(request));
        if (request.Lines.Count == 0)
            throw new ArgumentException("سبد خرید خالی است.", nameof(request));

        var order = new Order
        {
            CustomerId = request.CustomerId,
            CreatedOnUtc = DateTime.UtcNow
        };

        var pricingLines = new List<PricingLine>();

        // ۱) رزرو اتمیک موجودی برای هر خط + خواندن قیمت واحد از کاتالوگ
        foreach (var line in request.Lines)
        {
            var product = await _inventoryRepository.GetByIdAsync(line.ProductId, cancellationToken)
                ?? throw new InvalidOperationException($"محصول «{line.ProductId}» یافت نشد.");

            var variant = product.GetVariant(line.Sku)
                ?? throw new InvalidOperationException($"واریانت «{line.Sku}» برای محصول «{product.Name}» یافت نشد.");

            var reserved = await _inventoryRepository.TryReserveAsync(product.Id, line.Sku, line.Quantity, cancellationToken);
            if (!reserved)
                throw new InvalidOperationException($"موجودی کافی برای «{product.Name}» (کد {line.Sku}) وجود ندارد.");

            var unitPrice = new Money(variant.PriceToman);
            order.AddItem(product.Id, line.Sku, line.Quantity, unitPrice);
            pricingLines.Add(new PricingLine { UnitPriceToman = unitPrice, Quantity = line.Quantity });
        }

        // ۲) محاسبهٔ قیمت با پایپلاین
        var pricingContext = new PricingContext
        {
            Lines = pricingLines,
            TaxRatePercent = request.TaxRatePercent,
            ShippingToman = request.ShippingToman,
            CouponCode = request.CouponCode,
            CustomerId = request.CustomerId
        };

        var pricingResult = await _pricingPipeline.CalculateAsync(pricingContext, cancellationToken);
        if (pricingResult.HasErrors)
            throw new InvalidOperationException(string.Join(" | ", pricingResult.Errors));

        // ۳) اعمال قیمت‌گذاری روی سفارش و ذخیره
        order.ApplyPricing(
            pricingResult.Accumulator.SubTotalToman,
            pricingResult.Accumulator.DiscountToman,
            pricingResult.Accumulator.TaxToman,
            pricingResult.Accumulator.ShippingToman,
            pricingResult.Accumulator.AppliedCouponCodes);

        await _orderRepository.InsertAsync(order, cancellationToken);
        return order;
    }

    public async Task<Order?> GetByIdAsync(string orderId, CancellationToken cancellationToken = default)
    {
        return await _orderRepository.GetByIdAsync(orderId, cancellationToken);
    }
}
