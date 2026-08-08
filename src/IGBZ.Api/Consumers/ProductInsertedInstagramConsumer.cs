namespace IGBZ.Api.Consumers;

using IGBZ.Application.Instagram;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// انتشار خودکار پست اینستاگرام هنگام ثبت محصول جدید (سند بخش ۱۳).
/// این یک Service/Consumer ساده است — در فاز کامل به رویداد EntityInserted وصل می‌شود.
/// </summary>
public interface IProductInstagramPublisher
{
    Task PublishForNewProductAsync(string productId, string imageUrl, CancellationToken cancellationToken = default);
}

public class ProductInstagramPublisher : IProductInstagramPublisher
{
    private readonly IInstagramService _instagramService;

    public ProductInstagramPublisher(IInstagramService instagramService)
    {
        _instagramService = instagramService;
    }

    public async Task PublishForNewProductAsync(string productId, string imageUrl, CancellationToken cancellationToken = default)
    {
        var result = await _instagramService.PublishProductPostAsync(productId, imageUrl, cancellationToken);
        if (!result.IsSuccess)
            throw new InvalidOperationException($"انتشار پست اینستاگرام ناموفق بود: {result.Message}");
    }
}

/// <summary>Endpoint آزمایشی/دستی برای انتشار خودکار پست (فقط مالک تننت).</summary>
[ApiController]
[Route("api/admin/instagram")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "TenantOwner")]
public class InstagramPublishController : ControllerBase
{
    private readonly IProductInstagramPublisher _publisher;

    public InstagramPublishController(IProductInstagramPublisher publisher)
    {
        _publisher = publisher;
    }

    [HttpPost("publish-product/{productId}")]
    public async Task<IActionResult> Publish(string productId, [FromBody] PublishProductDto dto, CancellationToken cancellationToken)
    {
        try
        {
            await _publisher.PublishForNewProductAsync(productId, dto.ImageUrl, cancellationToken);
            return Ok(new { success = true, message = "پست منتشر شد." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

public class PublishProductDto
{
    public string ImageUrl { get; set; } = string.Empty;
}
