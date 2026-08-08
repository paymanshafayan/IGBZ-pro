namespace IGBZ.Api.Controllers.Admin;

using IGBZ.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>داشبورد ادمین تننت — آمار و سفارش‌ها (فقط مالک فروشگاه).</summary>
[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = "TenantOwner")]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;

    public AdminDashboardController(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        var summary = await _dashboardService.GetSummaryAsync(cancellationToken);
        return Ok(new { success = true, summary });
    }

    [HttpGet("orders")]
    public async Task<IActionResult> Orders(CancellationToken cancellationToken)
    {
        var orders = await _dashboardService.GetOrdersAsync(cancellationToken);
        return Ok(new { success = true, orders });
    }
}
