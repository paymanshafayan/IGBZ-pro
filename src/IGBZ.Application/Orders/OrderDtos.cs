using IGBZ.Application.Pricing;
using IGBZ.Domain.Orders;

namespace IGBZ.Application.Orders;

public sealed record CustomerSnapshotRequest(string? CustomerId, string FullName, string PhoneNumber, string? Email);

public sealed record AddressRequest(
    string FullName,
    string PhoneNumber,
    string Province,
    string City,
    string StreetLine,
    string? PostalCode,
    string? NationalCode);

public sealed record CreateOrderItemRequest(string ProductId, string VariantId, int Quantity);

public sealed record CreateOrderRequest(
    CustomerSnapshotRequest Customer,
    AddressRequest ShippingAddress,
    IReadOnlyList<CreateOrderItemRequest> Items,
    string? CouponCode,
    decimal ShippingAmount);

public sealed record OrderItemResponse(
    string ProductId,
    string VariantId,
    string ProductName,
    string VariantName,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string Currency);

public sealed record OrderResponse(
    string Id,
    string TenantId,
    string OrderNumber,
    OrderStatus Status,
    IReadOnlyList<OrderItemResponse> Items,
    decimal SubTotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal ShippingTotal,
    decimal GrandTotal,
    string Currency,
    string? CouponCode,
    PricingBreakdown Pricing);

public sealed record OrderActionRequest(string? Reason);
