namespace IGBZ.Api.Controllers;

using IGBZ.Application.Auth;
using IGBZ.Application.Tenancy;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITenantContext _tenantContext;

    public AuthController(IAuthService authService, ITenantContext tenantContext)
    {
        _authService = authService;
        _tenantContext = tenantContext;
    }

    /// <summary>ثبت‌نام مشتری در فروشگاه جاری (تننت از هدر/Host/توکن تشخیص داده می‌شود).</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto?.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { success = false, message = "ایمیل و رمز عبور الزامی است." });

        var tenantId = _tenantContext.TenantId ?? dto.TenantId;
        if (string.IsNullOrWhiteSpace(tenantId))
            return BadRequest(new { success = false, message = "شناسهٔ تننت مشخص نیست (هدر X-Tenant-Id یا Host لازم است)." });

        var result = await _authService.RegisterAsync(new RegisterCustomerRequest
        {
            TenantId = tenantId,
            Email = dto.Email,
            Password = dto.Password,
            Phone = dto.Phone,
            IsTenantOwner = dto.IsTenantOwner
        });

        if (!result.Success)
            return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, accessToken = result.AccessToken, customerId = result.Customer!.Id });
    }

    /// <summary>ورود — بازگشت JWT.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var tenantId = _tenantContext.TenantId ?? dto.TenantId;
        if (string.IsNullOrWhiteSpace(tenantId))
            return BadRequest(new { success = false, message = "شناسهٔ تننت مشخص نیست." });

        var result = await _authService.LoginAsync(tenantId, dto.Email, dto.Password);
        if (!result.Success)
            return Unauthorized(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, accessToken = result.AccessToken, customerId = result.Customer!.Id });
    }
}

public class RegisterRequestDto
{
    public string? TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsTenantOwner { get; set; }
}

public class LoginRequestDto
{
    public string? TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
