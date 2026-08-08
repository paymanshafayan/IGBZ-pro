namespace IGBZ.Api.Controllers;

using IGBZ.Application.Subscriptions;
using IGBZ.Application.Tenancy;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly ITenantSubscriptionService _subscriptionService;
    private readonly ITenantContext _tenantContext;

    public PaymentController(ITenantSubscriptionService subscriptionService, ITenantContext tenantContext)
    {
        _subscriptionService = subscriptionService;
        _tenantContext = tenantContext;
    }

    /// <summary>درخواست پرداخت اشتراک — بازگشت لینک درگاه.</summary>
    [HttpPost("request")]
    public async Task<IActionResult> Request([FromBody] PaymentRequestDto dto)
    {
        var tenantId = _tenantContext.TenantId;
        if (string.IsNullOrWhiteSpace(tenantId))
            return BadRequest(new { success = false, message = "شناسهٔ تننت مشخص نیست." });

        if (dto == null || string.IsNullOrWhiteSpace(dto.CallbackUrl))
            return BadRequest(new { success = false, message = "آدرس بازگشت (CallbackUrl) الزامی است." });

        try
        {
            var result = await _subscriptionService.RequestPaymentAsync(
                tenantId, dto.GatewayName ?? "test", dto.CallbackUrl);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.ErrorMessage });

            return Ok(new
            {
                success = true,
                trackingNumber = result.TrackingNumber,
                redirectUrl = result.RedirectUrl
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// تایید پرداخت (بازگشت از درگاه) — فقط بعد از تایید واقعی، اشتراک و فروشگاه فعال می‌شوند.
    /// </summary>
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] PaymentVerifyDto dto)
    {
        var tenantId = _tenantContext.TenantId;
        if (string.IsNullOrWhiteSpace(tenantId))
            return BadRequest(new { success = false, message = "شناسهٔ تننت مشخص نیست." });

        if (dto == null || string.IsNullOrWhiteSpace(dto.TrackingNumber))
            return BadRequest(new { success = false, message = "کد پیگیری الزامی است." });

        var result = await _subscriptionService.VerifyAndActivateAsync(tenantId, dto.TrackingNumber);

        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new
        {
            success = true,
            alreadyProcessed = result.AlreadyProcessed,
            bankRefId = result.BankRefId,
            message = "پرداخت تایید شد و فروشگاه فعال گردید."
        });
    }
}

public class PaymentRequestDto
{
    public string? GatewayName { get; set; }
    public string CallbackUrl { get; set; } = string.Empty;
}

public class PaymentVerifyDto
{
    public string TrackingNumber { get; set; } = string.Empty;
}
