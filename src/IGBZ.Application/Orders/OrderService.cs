using IGBZ.Application.Abstractions;
using IGBZ.Application.Pricing;
using IGBZ.Domain.Catalog;
using IGBZ.Domain.Discounts;
using IGBZ.Domain.Orders;
using IGBZ.Domain.Shared;

namespace IGBZ.Application.Orders;

public sealed class OrderService(
    ITenantContextAccessor tenantContextAccessor,
    ITenantScopedRepository<Product> products,
    ITenantScopedRepository<Discount> discounts,
    ITenantScopedRepository<Order> orders,
    IInventoryService inventoryService,
    IOrderTotalCalculator totalCalculator,
    IOrderNumberGenerator orderNumberGenerator,
    CommercePricingOptions pricingOptions,
    IClock clock)
{
    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new DomainException("Checkout requires at least one item.");
        }

        var tenantId = tenantContextAccessor.RequiredTenantId;
        var now = clock.UtcNow;
        var orderNumber = orderNumberGenerator.Generate(tenantId, now);
        var orderItems = new List<OrderItem>();
        var priceLines = new List<PriceLine>();
        var reservationIds = new List<string>();

        try
        {
            foreach (var item in request.Items)
            {
                var product = await products.GetByIdAsync(item.ProductId, cancellationToken)
                    ?? throw new DomainException("Product was not found.");

                if (product.Status != ProductStatus.Published)
                {
                    throw new DomainException("Product is not published.");
                }

                var variant = product.GetActiveVariant(item.VariantId);
                Guard.AgainstNonPositive(item.Quantity, nameof(item.Quantity));

                var reservation = await inventoryService.TryReserveAsync(
                    product.Id,
                    variant.Id,
                    item.Quantity,
                    $"checkout:{orderNumber}",
                    pricingOptions.ReservationTtl,
                    cancellationToken);

                if (!reservation.Succeeded || reservation.ReservationId is null)
                {
                    throw new DomainException(reservation.FailureReason ?? "Insufficient stock.");
                }

                reservationIds.Add(reservation.ReservationId);

                orderItems.Add(new OrderItem(
                    product.Id,
                    variant.Id,
                    product.Name,
                    variant.Name,
                    variant.Sku,
                    item.Quantity,
                    variant.Price,
                    variant.Currency));

                priceLines.Add(new PriceLine(
                    product.Id,
                    variant.Id,
                    product.Name,
                    variant.Name,
                    variant.Sku,
                    item.Quantity,
                    variant.Price,
                    variant.Currency));
            }

            var availableDiscounts = await discounts.ListAsync(discount => discount.IsActive, cancellationToken);
            var pricing = totalCalculator.Calculate(
                new PricingRequest(priceLines, request.CouponCode, request.ShippingAmount, pricingOptions.VatRate, now),
                availableDiscounts);

            var customer = new OrderCustomerSnapshot(
                request.Customer.CustomerId,
                request.Customer.FullName,
                request.Customer.PhoneNumber,
                request.Customer.Email);

            var address = new Address(
                request.ShippingAddress.FullName,
                request.ShippingAddress.PhoneNumber,
                request.ShippingAddress.Province,
                request.ShippingAddress.City,
                request.ShippingAddress.StreetLine,
                request.ShippingAddress.PostalCode,
                request.ShippingAddress.NationalCode);

            var order = Order.Create(
                tenantId,
                orderNumber,
                customer,
                address,
                orderItems,
                reservationIds,
                pricing.SubTotal,
                pricing.DiscountTotal,
                pricing.TaxTotal,
                pricing.ShippingTotal,
                pricing.GrandTotal,
                pricing.Currency,
                request.CouponCode,
                now);

            await orders.AddAsync(order, cancellationToken);
            return ToResponse(order, pricing);
        }
        catch
        {
            foreach (var reservationId in reservationIds)
            {
                await inventoryService.ReleaseReservationAsync(reservationId, cancellationToken);
            }

            throw;
        }
    }

    public async Task<OrderResponse> GetOrderAsync(string id, CancellationToken cancellationToken = default)
    {
        var order = await orders.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Order was not found.");

        return ToResponse(order, ToPricingBreakdown(order));
    }

    public async Task<IReadOnlyList<OrderResponse>> ListOrdersAsync(CancellationToken cancellationToken = default)
    {
        var result = await orders.ListAsync(null, cancellationToken);
        return result.OrderByDescending(order => order.CreatedAtUtc).Select(order => ToResponse(order, ToPricingBreakdown(order))).ToList();
    }


    public async Task<OrderResponse> StartProcessingAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderAggregateAsync(orderId, cancellationToken);
        order.StartProcessing(clock.UtcNow);
        await orders.ReplaceAsync(order, cancellationToken);
        return ToResponse(order, ToPricingBreakdown(order));
    }

    public async Task<OrderResponse> MarkAsShippedAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderAggregateAsync(orderId, cancellationToken);
        order.MarkAsShipped(clock.UtcNow);
        await orders.ReplaceAsync(order, cancellationToken);
        return ToResponse(order, ToPricingBreakdown(order));
    }

    public async Task<OrderResponse> MarkAsDeliveredAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderAggregateAsync(orderId, cancellationToken);
        order.MarkAsDelivered(clock.UtcNow);
        await orders.ReplaceAsync(order, cancellationToken);
        return ToResponse(order, ToPricingBreakdown(order));
    }

    public async Task<OrderResponse> CancelAsync(string orderId, string reason, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderAggregateAsync(orderId, cancellationToken);
        if (order.Status == OrderStatus.Pending)
        {
            foreach (var reservationId in order.StockReservationIds)
            {
                await inventoryService.ReleaseReservationAsync(reservationId, cancellationToken);
            }
        }

        order.Cancel(reason, clock.UtcNow);
        await orders.ReplaceAsync(order, cancellationToken);
        return ToResponse(order, ToPricingBreakdown(order));
    }

    public async Task<OrderResponse> ReturnAsync(string orderId, string reason, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderAggregateAsync(orderId, cancellationToken);
        order.Return(reason, clock.UtcNow);
        await orders.ReplaceAsync(order, cancellationToken);
        return ToResponse(order, ToPricingBreakdown(order));
    }

    public async Task<OrderResponse> RefundAsync(string orderId, string reason, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderAggregateAsync(orderId, cancellationToken);
        order.Refund(reason, clock.UtcNow);
        await orders.ReplaceAsync(order, cancellationToken);
        return ToResponse(order, ToPricingBreakdown(order));
    }

    private async Task<Order> GetOrderAggregateAsync(string orderId, CancellationToken cancellationToken)
        => await orders.GetByIdAsync(orderId, cancellationToken)
            ?? throw new DomainException("Order was not found.");

    public async Task<Order> MarkOrderAsPaidAsync(string orderId, string paymentTransactionId, CancellationToken cancellationToken = default)
    {
        var order = await orders.GetByIdAsync(orderId, cancellationToken)
            ?? throw new DomainException("Order was not found.");

        if (order.Status == OrderStatus.Paid && order.PaymentTransactionId == paymentTransactionId)
        {
            return order;
        }

        foreach (var reservationId in order.StockReservationIds)
        {
            await inventoryService.CommitReservationAsync(reservationId, cancellationToken);
        }

        order.MarkAsPaid(paymentTransactionId, clock.UtcNow);
        await orders.ReplaceAsync(order, cancellationToken);
        return order;
    }

    private static OrderResponse ToResponse(Order order, PricingBreakdown pricing)
        => new(
            order.Id,
            order.TenantId,
            order.OrderNumber,
            order.Status,
            order.Items.Select(ToItemResponse).ToList(),
            order.SubTotal,
            order.DiscountTotal,
            order.TaxTotal,
            order.ShippingTotal,
            order.GrandTotal,
            order.Currency,
            order.CouponCode,
            pricing);

    private static OrderItemResponse ToItemResponse(OrderItem item)
        => new(
            item.ProductId,
            item.VariantId,
            item.ProductName,
            item.VariantName,
            item.Sku,
            item.Quantity,
            item.UnitPrice,
            item.LineTotal,
            item.Currency);

    private static PricingBreakdown ToPricingBreakdown(Order order)
        => new(
            order.SubTotal,
            order.DiscountTotal,
            order.TaxTotal,
            order.ShippingTotal,
            order.GrandTotal,
            order.Currency,
            []);
}
