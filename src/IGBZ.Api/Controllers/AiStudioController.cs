namespace IGBZ.Api.Controllers;

using System.Security.Claims;
using IGBZ.Application.AiStudio;
using IGBZ.Application.Wallets;
using IGBZ.Domain.Wallets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// استودیوی هوش مصنوعی محتوا (سند بخش ۱۴) — برای مشتریان واردشده.
/// هر عملیات هزینه از کیف‌پول مشتری کسر می‌شود و در صورت شکست، خودکار بازگردانده می‌شود.
/// </summary>
[ApiController]
[Route("api/ai-studio")]
[Authorize]
public class AiStudioController : ControllerBase
{
    // هزینهٔ هر عملیات به تومان — در فاز کامل از تنظیمات تننت خوانده می‌شود
    private const decimal EnhancePhotoCost = 30_000m;
    private const decimal VideoStoryCost = 75_000m;
    private const decimal VoiceOverCost = 15_000m;

    private readonly IAiStudioService _aiStudioService;
    private readonly IBackgroundMusicCatalogService _musicCatalogService;
    private readonly IWalletService _walletService;

    public AiStudioController(
        IAiStudioService aiStudioService,
        IBackgroundMusicCatalogService musicCatalogService,
        IWalletService walletService)
    {
        _aiStudioService = aiStudioService;
        _musicCatalogService = musicCatalogService;
        _walletService = walletService;
    }

    private string RequireCustomerId()
    {
        var customerId = User.FindFirstValue("customerId");
        if (string.IsNullOrWhiteSpace(customerId))
            throw new UnauthorizedAccessException("شناسهٔ مشتری در توکن یافت نشد.");
        return customerId;
    }

    /// <summary>فهرست موسیقی پس‌زمینهٔ ویدیو.</summary>
    [HttpGet("background-music-tracks")]
    [AllowAnonymous]
    public IActionResult GetMusicTracks()
    {
        return Ok(new { success = true, tracks = _musicCatalogService.GetAvailableTracks() });
    }

    /// <summary>ادیت عکس محصول (حذف پس‌زمینه/استودیویی) — کسر ۳۰٬۰۰۰ تومان.</summary>
    [HttpPost("enhance-photo")]
    public async Task<IActionResult> EnhancePhoto([FromBody] EnhancePhotoRequest dto, CancellationToken cancellationToken)
    {
        var customerId = RequireCustomerId();
        var reference = $"ai-photo-{Guid.NewGuid():N}";

        var (debited, balance, error) = await _walletService.TryDebitAsync(
            customerId, EnhancePhotoCost, WalletTransactionReason.AiFeatureUsageDebit, reference, cancellationToken);
        if (!debited)
            return StatusCode(402, new { success = false, message = error, requiredToman = EnhancePhotoCost, balance });

        var result = await _aiStudioService.EnhancePhotoAsync(dto, cancellationToken);

        if (!result.IsSuccess)
        {
            await _walletService.CreditAsync(customerId, EnhancePhotoCost, WalletTransactionReason.AiFeatureUsageRefund, $"{reference}-refund", cancellationToken);
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new { success = true, outputUrl = result.OutputUrl, tomanCharged = EnhancePhotoCost });
    }

    /// <summary>تولید ویدیوی کوتاه استوری — کسر ۷۵٬۰۰۰ تومان.</summary>
    [HttpPost("video-story")]
    public async Task<IActionResult> VideoStory([FromBody] VideoStoryRequest dto, CancellationToken cancellationToken)
    {
        var customerId = RequireCustomerId();
        var reference = $"ai-video-{Guid.NewGuid():N}";

        var (debited, balance, error) = await _walletService.TryDebitAsync(
            customerId, VideoStoryCost, WalletTransactionReason.AiFeatureUsageDebit, reference, cancellationToken);
        if (!debited)
            return StatusCode(402, new { success = false, message = error, requiredToman = VideoStoryCost, balance });

        var result = await _aiStudioService.GenerateVideoStoryAsync(dto, cancellationToken);

        if (!result.IsSuccess)
        {
            await _walletService.CreditAsync(customerId, VideoStoryCost, WalletTransactionReason.AiFeatureUsageRefund, $"{reference}-refund", cancellationToken);
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new { success = true, outputUrl = result.OutputUrl, tomanCharged = VideoStoryCost });
    }

    /// <summary>صداپیشگی فارسی — کسر ۱۵٬۰۰۰ تومان.</summary>
    [HttpPost("voice-over")]
    public async Task<IActionResult> VoiceOver([FromBody] VoiceOverRequest dto, CancellationToken cancellationToken)
    {
        var customerId = RequireCustomerId();
        var reference = $"ai-voice-{Guid.NewGuid():N}";

        var (debited, balance, error) = await _walletService.TryDebitAsync(
            customerId, VoiceOverCost, WalletTransactionReason.AiFeatureUsageDebit, reference, cancellationToken);
        if (!debited)
            return StatusCode(402, new { success = false, message = error, requiredToman = VoiceOverCost, balance });

        var result = await _aiStudioService.GenerateVoiceOverAsync(dto, cancellationToken);

        if (!result.IsSuccess)
        {
            await _walletService.CreditAsync(customerId, VoiceOverCost, WalletTransactionReason.AiFeatureUsageRefund, $"{reference}-refund", cancellationToken);
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new { success = true, outputUrl = result.OutputUrl, tomanCharged = VoiceOverCost });
    }
}
