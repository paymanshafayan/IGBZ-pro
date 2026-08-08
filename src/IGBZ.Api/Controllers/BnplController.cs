namespace IGBZ.Api.Controllers;

using IGBZ.Application.BNPL;
using Microsoft.AspNetCore.Mvc;

/// <summary>پرداخت اعتباری/اقساطی (دیجی‌پی/اسنپ‌پی) — برای مشتریان.</summary>
[ApiController]
[Route("api/bnpl")]
public class BnplController : ControllerBase
{
    private readonly IBnplService _bnplService;

    public BnplController(IBnplService bnplService)
    {
        _bnplService = bnplService;
    }

    /// <summary>بررسی اجازهٔ خرید اعتباری.</summary>
    [HttpPost("check-eligibility")]
    public async Task<IActionResult> CheckEligibility([FromBody] BnplCheckEligibilityDto dto, CancellationToken cancellationToken)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.ProviderKey))
            return BadRequest(new { success = false, message = "ارائه‌دهندهٔ BNPL مشخص نشده است." });

        var result = await _bnplService.CheckEligibilityAsync(
            dto.ProviderKey.Trim().ToLowerInvariant(),
            dto.AmountToman,
            dto.CustomerMobile ?? string.Empty,
            dto.CustomerNationalId,
            cancellationToken);

        return Ok(new { success = true, isEligible = result.IsEligible, message = result.Message });
    }

    /// <summary>شروع پرداخت اعتباری — بازگشت لینک درگاه.</summary>
    [HttpPost("start-payment")]
    public async Task<IActionResult> StartPayment([FromBody] BnplStartPaymentDto dto, CancellationToken cancellationToken)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.ProviderKey) || string.IsNullOrWhiteSpace(dto.OrderId))
            return BadRequest(new { success = false, message = "اطلاعات پرداخت ناقص است." });

        var result = await _bnplService.StartPaymentAsync(
            dto.ProviderKey.Trim().ToLowerInvariant(),
            dto.OrderId,
            dto.AmountToman,
            dto.CallbackUrl,
            dto.CustomerMobile ?? string.Empty,
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new
        {
            success = true,
            redirectUrl = result.RedirectUrl,
            transactionId = result.TransactionId,
            paymentToken = result.PaymentToken
        });
    }

    /// <summary>تایید پرداخت اعتباری (بازگشت از درگاه).</summary>
    [HttpPost("verify-payment")]
    public async Task<IActionResult> VerifyPayment([FromBody] BnplVerifyDto dto, CancellationToken cancellationToken)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.ProviderKey) || string.IsNullOrWhiteSpace(dto.TransactionId))
            return BadRequest(new { success = false, message = "اطلاعات تایید ناقص است." });

        var result = await _bnplService.VerifyPaymentAsync(
            dto.ProviderKey.Trim().ToLowerInvariant(),
            dto.TransactionId,
            dto.PaymentToken ?? string.Empty,
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, trackingCode = result.TrackingCode });
    }
}

public class BnplCheckEligibilityDto
{
    public string ProviderKey { get; set; } = string.Empty;
    public decimal AmountToman { get; set; }
    public string? CustomerMobile { get; set; }
    public string? CustomerNationalId { get; set; }
}

public class BnplStartPaymentDto
{
    public string ProviderKey { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public decimal AmountToman { get; set; }
    public string CallbackUrl { get; set; } = string.Empty;
    public string? CustomerMobile { get; set; }
}

public class BnplVerifyDto
{
    public string ProviderKey { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string? PaymentToken { get; set; }
}
