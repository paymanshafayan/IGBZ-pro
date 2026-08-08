namespace IGBZ.Api.Controllers;

using IGBZ.Application.Catalog;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// کاتالوگ عمومی فروشگاه (Storefront) — نیازی به احراز هویت ندارد؛ تننت از Host/X-Tenant-Id
/// توسط TenantResolutionMiddleware تشخیص داده می‌شود.
/// </summary>
[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public CatalogController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    /// <summary>لیست محصولات منتشرشدهٔ فروشگاه جاری.</summary>
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
    {
        var products = await _catalogService.GetPublishedProductsAsync(cancellationToken);
        return Ok(new { success = true, products });
    }

    /// <summary>جزئیات یک محصول با slug.</summary>
    [HttpGet("products/{slug}")]
    public async Task<IActionResult> GetProduct(string slug, CancellationToken cancellationToken)
    {
        var product = await _catalogService.GetProductBySlugAsync(slug, cancellationToken);
        if (product == null)
            return NotFound(new { success = false, message = "محصول یافت نشد." });

        return Ok(new { success = true, product });
    }
}
