namespace IGBZ.Api.Controllers;

using IGBZ.Application.Orders;
using IGBZ.Application.Tenancy;
using IGBZ.Domain.Common;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ITenantContext _tenantContext;

    public OrderController(IOrderService orderService, ITenantContext tenantContext)
    {
        _orderService = orderService;
        _tenantContext = tenantContext;
    }

    /// <summary>ساخت سفارش — رزرو اتمیک موجودی + محاسبهٔ قیمت. (فاز ۲: بدون پرداخت؛ بعداً به Payment وصل می‌شود.)</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        var tenantId = _tenantContext.TenantId;
        if (string.IsNullOrWhiteSpace(tenantId))
            return BadRequest(new { success = false, message = "شناسهٔ تننت مشخص نیست." });

        if (dto == null || dto.Lines == null || dto.Lines.Count == 0)
            return BadRequest(new { success = false, message = "سبد خرید خالی است." });

        try
        {
            var order = await _orderService.CreateOrderAsync(new CreateOrderRequest
            {
                CustomerId = dto.CustomerId,
                CouponCode = dto.CouponCode,
                ShippingToman = new Money(dto.ShippingToman ?? 0),
                TaxRatePercent = dto.TaxRatePercent ?? 9,
                Lines = dto.Lines.Select(l => new OrderLineInput
                {
                    ProductId = l.ProductId,
                    Sku = l.Sku,
                    Quantity = l.Quantity
                }).ToList()
            });

            return Ok(new
            {
                success = true,
                orderId = order.Id,
                status = order.Status.ToString(),
                grandTotalToman = order.GrandTotalToman.Toman,
                discountToman = order.DiscountToman.Toman
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

public class CreateOrderDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string? CouponCode { get; set; }
    public decimal? ShippingToman { get; set; }
    public decimal? TaxRatePercent { get; set; }
    public List<CreateOrderLineDto> Lines { get; set; } = new();
}

public class CreateOrderLineDto
{
    public string ProductId { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
