namespace IGBZ.Api.Controllers.Admin;

using IGBZ.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// مدیریت محصولات تننت — فقط مالک فروشگاه (توکن JWT با isTenantOwner=True).
/// تننت از ادعای JWT توسط TenantResolutionMiddleware تشخیص داده می‌شود.
/// </summary>
[ApiController]
[Route("api/admin/products")]
[Authorize(Policy = "TenantOwner")]
public class AdminProductsController : ControllerBase
{
    private readonly IAdminProductService _productService;

    public AdminProductsController(IAdminProductService productService)
    {
        _productService = productService;
    }

    /// <summary>لیست همهٔ محصولات (شامل منتشرنشده) — برای پنل ادمین.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var products = await _productService.ListAllAsync(cancellationToken);
        return Ok(new { success = true, products });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminProductInput input)
    {
        try
        {
            var id = await _productService.CreateAsync(input);
            return Ok(new { success = true, productId = id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("{productId}")]
    public async Task<IActionResult> Update(string productId, [FromBody] AdminProductInput input)
    {
        try
        {
            await _productService.UpdateAsync(productId, input);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{productId}")]
    public async Task<IActionResult> Delete(string productId)
    {
        try
        {
            await _productService.DeleteAsync(productId);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{productId}/publish")]
    public async Task<IActionResult> SetPublished(string productId, [FromBody] PublishRequestDto dto)
    {
        try
        {
            await _productService.SetPublishedAsync(productId, dto.Published);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

public class PublishRequestDto
{
    public bool Published { get; set; }
}
