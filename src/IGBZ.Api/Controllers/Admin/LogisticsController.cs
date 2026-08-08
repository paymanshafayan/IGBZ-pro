namespace IGBZ.Api.Controllers.Admin;

using IGBZ.Application.Logistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>لجستیک ادمین — پیشنهاد مسیر و ثبت مرسوله در تاپین (فقط مالک تننت).</summary>
[ApiController]
[Route("api/admin/logistics")]
[Authorize(Policy = "TenantOwner")]
public class LogisticsController : ControllerBase
{
    private readonly ILogisticsService _logisticsService;

    public LogisticsController(ILogisticsService logisticsService)
    {
        _logisticsService = logisticsService;
    }

    /// <summary>پیشنهاد مسیر/شرکت حمل‌ونقل بر اساس وزن و شهر.</summary>
    [HttpGet("suggest-route")]
    public IActionResult SuggestRoute([FromQuery] decimal weightKg, [FromQuery] string destinationCity, [FromQuery] bool isExpressNeeded = false)
    {
        var route = _logisticsService.CategorizeRoute(weightKg, destinationCity, isExpressNeeded);
        return Ok(new { success = true, route });
    }

    /// <summary>ثبت مرسوله در تاپین — بازگشت کد رهگیری واقعی + PIN تحویل.</summary>
    [HttpPost("register-shipment")]
    public async Task<IActionResult> RegisterShipment([FromBody] RegisterShipmentDto dto, CancellationToken cancellationToken)
    {
        var result = await _logisticsService.RegisterShipmentAsync(
            dto.OrderId, dto.WeightKg, dto.DestinationCity, dto.RecipientAddress, dto.RecipientPhone, dto.IsCod, cancellationToken);

        return result.IsSuccess
            ? Ok(new { success = true, trackingCode = result.TrackingCode, deliveryPin = result.DeliveryPin, message = result.Message })
            : BadRequest(new { success = false, message = result.Message });
    }

    /// <summary>تولید PIN تحویل امن (برای تست/استفادهٔ دستی).</summary>
    [HttpGet("generate-pin")]
    public IActionResult GeneratePin()
    {
        return Ok(new { success = true, deliveryPin = _logisticsService.GenerateDeliveryPin() });
    }
}

public class RegisterShipmentDto
{
    public string OrderId { get; set; } = string.Empty;
    public decimal WeightKg { get; set; }
    public string DestinationCity { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public bool IsCod { get; set; }
}
