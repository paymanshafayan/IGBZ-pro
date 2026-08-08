namespace IGBZ.Api.Controllers;

using IGBZ.Application.Provisioning;
using IGBZ.Application.Subscriptions;
using IGBZ.Application.Tenancy;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/tenant")]
public class TenantController : ControllerBase
{
    private readonly ITenantProvisioningService _provisioningService;
    private readonly ITenantSubscriptionService _subscriptionService;
    private readonly ITenantContext _tenantContext;

    public TenantController(
        ITenantProvisioningService provisioningService,
        ITenantSubscriptionService subscriptionService,
        ITenantContext tenantContext)
    {
        _provisioningService = provisioningService;
        _subscriptionService = subscriptionService;
        _tenantContext = tenantContext;
    }

    /// <summary>ثبت‌نام فروشگاه جدید (سایت مادر/ویزارد) — ساخت Tenant + مالک + اشتراک.</summary>
    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupRequestDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Subdomain) || string.IsNullOrWhiteSpace(dto.StoreName)
            || string.IsNullOrWhiteSpace(dto.AdminEmail) || string.IsNullOrWhiteSpace(dto.AdminPassword))
        {
            return BadRequest(new { success = false, message = "زیردامنه، نام فروشگاه، ایمیل و رمز عبور الزامی است." });
        }

        var result = await _provisioningService.ProvisionAsync(new ProvisionTenantRequest
        {
            Subdomain = dto.Subdomain,
            StoreName = dto.StoreName,
            AdminEmail = dto.AdminEmail,
            AdminPassword = dto.AdminPassword,
            AdminPhone = dto.AdminPhone,
            PlanId = dto.PlanId
        });

        if (!result.Success)
            return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new
        {
            success = true,
            tenantId = result.TenantId,
            subscriptionId = result.SubscriptionId,
            requiresPayment = result.RequiresPayment,
            amountToman = result.AmountToman,
            accessToken = result.AccessToken
        });
    }

    /// <summary>بررسی آزاد بودن زیردامنه (برای ویزارد ثبت‌نام).</summary>
    [HttpGet("check-subdomain")]
    public async Task<IActionResult> CheckSubdomain([FromQuery] string subdomain)
    {
        var available = await _provisioningService.IsSubdomainAvailableAsync(subdomain);
        return Ok(new { subdomain = subdomain?.Trim().ToLowerInvariant(), isAvailable = available });
    }

    /// <summary>وضعیت اشتراک فروشگاه جاری.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var tenantId = _tenantContext.TenantId;
        if (string.IsNullOrWhiteSpace(tenantId))
            return BadRequest(new { success = false, message = "شناسهٔ تننت مشخص نیست." });

        var status = await _subscriptionService.GetStatusAsync(tenantId);
        return Ok(new { success = true, status });
    }
}

public class SignupRequestDto
{
    public string Subdomain { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string? AdminPhone { get; set; }
    public string? PlanId { get; set; }
}
