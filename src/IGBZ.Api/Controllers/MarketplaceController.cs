namespace IGBZ.Api.Controllers;

using IGBZ.Application.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// مارکت‌پلیس — فید ترب عمومی (برای موتور قیمت‌یاب ترب) + همگام‌سازی ادمین.
/// </summary>
[ApiController]
[Route("api/marketplace")]
public class MarketplaceController : ControllerBase
{
    private readonly IMarketplaceService _marketplaceService;

    public MarketplaceController(IMarketplaceService marketplaceService)
    {
        _marketplaceService = marketplaceService;
    }

    /// <summary>فید JSON ترب — عمومی (تننت از Host تشخیص داده می‌شود).</summary>
    [HttpGet("torob-feed")]
    public async Task<IActionResult> TorobFeed(CancellationToken cancellationToken)
    {
        var feed = await _marketplaceService.GetTorobFeedAsync(cancellationToken);
        return Ok(feed);
    }

    /// <summary>همگام‌سازی موجودی/قیمت با دیجی‌کالا — فقط مالک تننت.</summary>
    [HttpPost("digikala/sync")]
    [Authorize(Policy = "TenantOwner")]
    public async Task<IActionResult> SyncDigikala([FromBody] SyncDigikalaDto dto, CancellationToken cancellationToken)
    {
        var result = await _marketplaceService.SyncStockAndPriceWithDigikalaAsync(
            dto.SellerVariantId, dto.StockCount, dto.PriceToman, cancellationToken);

        return result.IsSuccess ? Ok(new { success = true, message = result.Message }) : BadRequest(new { success = false, message = result.Message });
    }

    /// <summary>انتشار آگهی در دیوار — فقط مالک تننت.</summary>
    [HttpPost("divar/publish")]
    [Authorize(Policy = "TenantOwner")]
    public async Task<IActionResult> PublishDivar([FromBody] PublishDivarDto dto, CancellationToken cancellationToken)
    {
        var result = await _marketplaceService.PublishPostOnDivarAsync(
            dto.Title, dto.Description, dto.PriceToman, dto.ImageUrl, cancellationToken);

        return result.IsSuccess
            ? Ok(new { success = true, postToken = result.PostToken, postUrl = result.PostUrl, message = result.Message })
            : BadRequest(new { success = false, message = result.Message });
    }
}

public class SyncDigikalaDto
{
    public string SellerVariantId { get; set; } = string.Empty;
    public int StockCount { get; set; }
    public decimal PriceToman { get; set; }
}

public class PublishDivarDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PriceToman { get; set; }
    public string? ImageUrl { get; set; }
}
