namespace IGBZ.Api.Controllers.Admin;

using IGBZ.Application.Abstractions;
using IGBZ.Application.AiStudio;
using IGBZ.Domain.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// ابزارهای AI ادمین (فقط مالک تننت) — تولید سئو و ترجمهٔ محصول با ذخیرهٔ واقعی.
/// </summary>
[ApiController]
[Route("api/admin/ai-tools")]
[Authorize(Policy = "TenantOwner")]
public class AdminAiToolsController : ControllerBase
{
    private readonly IAiStudioService _aiStudioService;
    private readonly ITenantScopedRepository<Product> _productRepository;

    public AdminAiToolsController(IAiStudioService aiStudioService, ITenantScopedRepository<Product> productRepository)
    {
        _aiStudioService = aiStudioService;
        _productRepository = productRepository;
    }

    /// <summary>تولید متادیتای سئو و ذخیرهٔ واقعی روی محصول.</summary>
    [HttpPost("generate-seo")]
    public async Task<IActionResult> GenerateSeo([FromBody] GenerateSeoDto dto, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(dto.ProductId, cancellationToken);
        if (product == null)
            return NotFound(new { success = false, message = "محصول یافت نشد." });

        var seo = _aiStudioService.GenerateSeoMeta(product.Name, product.Description);

        product.MetaTitle = seo.MetaTitle;
        product.MetaDescription = seo.MetaDescription;
        product.MetaKeywords = seo.MetaKeywords;
        product.UpdatedOnUtc = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product, cancellationToken);

        return Ok(new { success = true, metaTitle = seo.MetaTitle, metaDescription = seo.MetaDescription });
    }

    /// <summary>ترجمهٔ خودکار نام/توضیحات و ذخیرهٔ واقعی در ترجمه‌های محصول.</summary>
    [HttpPost("translate-product")]
    public async Task<IActionResult> TranslateProduct([FromBody] TranslateProductDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.TargetLanguageCode))
            return BadRequest(new { success = false, message = "زبان مقصد الزامی است." });

        var product = await _productRepository.GetByIdAsync(dto.ProductId, cancellationToken);
        if (product == null)
            return NotFound(new { success = false, message = "محصول یافت نشد." });

        var nameResult = await _aiStudioService.AutoTranslateAsync(new TranslateRequest
        {
            Text = product.Name,
            TargetLanguageCode = dto.TargetLanguageCode
        }, cancellationToken);

        if (!nameResult.IsSuccess)
            return BadRequest(new { success = false, message = nameResult.ErrorMessage });

        string? descTranslation = null;
        if (!string.IsNullOrWhiteSpace(product.Description))
        {
            var descResult = await _aiStudioService.AutoTranslateAsync(new TranslateRequest
            {
                Text = product.Description,
                TargetLanguageCode = dto.TargetLanguageCode
            }, cancellationToken);

            if (!descResult.IsSuccess)
                return BadRequest(new { success = false, message = descResult.ErrorMessage });

            descTranslation = descResult.TranslatedText;
        }

        // ذخیرهٔ واقعی در ترجمه‌های محصول (مثل جدول LocalizedProperty در نسخهٔ قبلی)
        product.Translations.RemoveAll(t => t.LanguageCode == dto.TargetLanguageCode);
        product.Translations.Add(new ProductTranslation
        {
            LanguageCode = dto.TargetLanguageCode,
            Name = nameResult.TranslatedText!,
            Description = descTranslation,
            CreatedOnUtc = DateTime.UtcNow
        });
        product.UpdatedOnUtc = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product, cancellationToken);

        return Ok(new { success = true, translatedName = nameResult.TranslatedText, translatedDescription = descTranslation });
    }
}

public class GenerateSeoDto
{
    public string ProductId { get; set; } = string.Empty;
}

public class TranslateProductDto
{
    public string ProductId { get; set; } = string.Empty;
    public string TargetLanguageCode { get; set; } = string.Empty;
}
