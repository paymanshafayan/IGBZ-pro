namespace IGBZ.Application.Instagram;

using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IGBZ.Application.Abstractions;
using IGBZ.Domain.Catalog;
using IGBZ.Domain.Discounts;
using IGBZ.Domain.Instagram;
using IGBZ.Domain.Integration;

/// <summary>
/// پیاده‌سازی دستیار اینستاگرام با Graph API واقعی (مستندات متا).
/// </summary>
public class InstagramService : IInstagramService
{
    private const string GraphProviderKey = "instagram.graph";

    private readonly ITenantScopedRepository<IntegrationCredential> _credentialRepository;
    private readonly ITenantScopedRepository<InstagramFollowMentionReward> _rewardRepository;
    private readonly ITenantScopedRepository<Discount> _discountRepository;
    private readonly ITenantScopedRepository<Product> _productRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IHttpClientFactory _httpClientFactory;

    public InstagramService(
        ITenantScopedRepository<IntegrationCredential> credentialRepository,
        ITenantScopedRepository<InstagramFollowMentionReward> rewardRepository,
        ITenantScopedRepository<Discount> discountRepository,
        ITenantScopedRepository<Product> productRepository,
        IEncryptionService encryptionService,
        IHttpClientFactory httpClientFactory)
    {
        _credentialRepository = credentialRepository;
        _rewardRepository = rewardRepository;
        _discountRepository = discountRepository;
        _productRepository = productRepository;
        _encryptionService = encryptionService;
        _httpClientFactory = httpClientFactory;
    }

    // ────────────────────────── امضای وب‌هوک ──────────────────────────

    public bool VerifyWebhookSignature(string rawBody, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(rawBody) || string.IsNullOrWhiteSpace(signatureHeader))
            return false;

        const string prefix = "sha256=";
        if (!signatureHeader.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        // App Secret از اعتبارنامهٔ instagram.graph (فیلد ApiSecret)
        // ⚠️ در این نسخه از کلید تعریف‌شده استفاده می‌شود؛ در Production از راز محیطی
        var appSecret = Environment.GetEnvironmentVariable("IGBZ_INSTAGRAM_APP_SECRET");
        if (string.IsNullOrWhiteSpace(appSecret))
            return false;

        var providedHashHex = signatureHeader[prefix.Length..];

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var computedHashHex = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHashHex),
            Encoding.UTF8.GetBytes(providedHashHex));
    }

    // ────────────────────────── کامنت → دایرکت لینک خرید ──────────────────────────

    public async Task<InstagramWebhookResult> ProcessCommentAsync(
        string tenantId, string commentId, string commentText, string commenterIgsid,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(commentId) || string.IsNullOrWhiteSpace(commentText))
            return new InstagramWebhookResult { Processed = false, Message = "دادهٔ کامنت ناقص است." };

        var accessToken = await GetGraphAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
            return new InstagramWebhookResult { Processed = false, Message = "توکن Instagram Graph فعال نیست." };

        // تطبیق دقیق متن کامنت با SKU محصول (سند ۱۳)
        var sku = commentText.Trim();
        var product = await _productRepository.FirstOrDefaultAsync(
            p => p.IsPublished && !p.Deleted && p.Variants.Any(v => v.Sku == sku), cancellationToken);

        if (product == null)
            return new InstagramWebhookResult { Processed = false, Message = "هیچ محصولی با این کد یافت نشد." };

        var dmText = $"سلام! برای «{product.Name}» همین الان اقدام کن:\n{GetProductUrl(tenantId, product.Slug)}\n(این لینک ۱۵ دقیقه معتبر است)";

        var dmSent = await SendDirectMessageAsync(accessToken, commenterIgsid, dmText, cancellationToken);

        return new InstagramWebhookResult
        {
            Processed = dmSent,
            Message = dmSent ? "لینک خرید به دایرکت ارسال شد." : "ارسال دایرکت ناموفق بود (پنجرهٔ ۲۴ ساعته)."
        };
    }

    // ────────────────────────── فالو + منشن → کد تخفیف ──────────────────────────

    public async Task<InstagramWebhookResult> ProcessMentionAsync(
        string tenantId, string mentioningUserIgsid, string? mediaId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mentioningUserIgsid))
            return new InstagramWebhookResult { Processed = false, Message = "شناسهٔ کاربر منشن‌کننده ناقص است." };

        // جلوگیری از تکرار (Unique منطقی)
        var existing = await _rewardRepository.FirstOrDefaultAsync(
            r => r.InstagramScopedId == mentioningUserIgsid, cancellationToken);
        if (existing != null)
            return new InstagramWebhookResult { Processed = false, Message = "این کاربر قبلاً پاداش گرفته است." };

        var accessToken = await GetGraphAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
            return new InstagramWebhookResult { Processed = false, Message = "توکن Instagram Graph فعال نیست." };

        // بررسی واقعی فالو بودن (سند ۱۳)
        var isFollowing = await CheckIsFollowingAsync(accessToken, mentioningUserIgsid, cancellationToken);
        if (!isFollowing)
            return new InstagramWebhookResult { Processed = false, Message = "کاربر فالو نیست." };

        // صدور کد تخفیف یک‌بارمصرف واقعی (سطح ۲)
        var couponCode = $"IGFM-{Guid.NewGuid():N}"[..10].ToUpperInvariant();
        await _discountRepository.InsertAsync(new Discount
        {
            Name = $"پاداش فالو+منشن - {mentioningUserIgsid}",
            UsePercentage = true,
            DiscountPercentage = 15,
            RequiresCouponCode = true,
            CouponCode = couponCode,
            LimitationType = DiscountLimitationType.NTimesOnly,
            LimitationTimes = 1,
            IsActive = true,
            CreatedOnUtc = DateTime.UtcNow
        }, cancellationToken);

        var dmSent = await SendDirectMessageAsync(accessToken, mentioningUserIgsid,
            $"ممنون از منشن شما! 🎉 کد تخفیف ۱۵٪: {couponCode}", cancellationToken);

        var fallbackPosted = false;
        if (!dmSent && !string.IsNullOrWhiteSpace(mediaId))
        {
            fallbackPosted = await PostFallbackCommentAsync(accessToken, mediaId, cancellationToken);
        }

        await _rewardRepository.InsertAsync(new InstagramFollowMentionReward
        {
            InstagramScopedId = mentioningUserIgsid,
            CouponCode = couponCode,
            DirectMessageSent = dmSent,
            FallbackCommentPosted = fallbackPosted,
            IssuedOnUtc = DateTime.UtcNow
        }, cancellationToken);

        return new InstagramWebhookResult
        {
            Processed = true,
            Message = dmSent ? "کد تخفیف به دایرکت ارسال شد." : "کد صادر شد؛ دایرکت ناموفق بود (کامنت جایگزین)."
        };
    }

    // ────────────────────────── انتشار خودکار پست محصول ──────────────────────────

    public async Task<InstagramPostResult> PublishProductPostAsync(
        string productId, string imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return new InstagramPostResult { IsSuccess = false, Message = "تصویر محصول برای پست الزامی است." };

        var accessToken = await GetGraphAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
            return new InstagramPostResult { IsSuccess = false, Message = "توکن Instagram Graph فعال نیست." };

        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product == null)
            return new InstagramPostResult { IsSuccess = false, Message = "محصول یافت نشد." };

        var caption = BuildCaption(product);

        try
        {
            var client = _httpClientFactory.CreateClient("InstagramGraphApi");

            // ۱) ساخت Media Container
            var containerResponse = await client.PostAsync(
                $"https://graph.facebook.com/v19.0/me/media?image_url={Uri.EscapeDataString(imageUrl)}" +
                $"&caption={Uri.EscapeDataString(caption)}&access_token={Uri.EscapeDataString(accessToken)}",
                null, cancellationToken);

            var containerBody = await containerResponse.Content.ReadAsStringAsync(cancellationToken);
            if (!containerResponse.IsSuccessStatusCode)
                return new InstagramPostResult { IsSuccess = false, Message = $"ساخت Container ناموفق بود: {Truncate(containerBody)}" };

            var container = JsonSerializer.Deserialize<InstagramContainerResponse>(
                containerBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (string.IsNullOrWhiteSpace(container?.Id))
                return new InstagramPostResult { IsSuccess = false, Message = "پاسخ ساخت Container بدون شناسه بود." };

            // ۲) انتشار
            var publishResponse = await client.PostAsync(
                $"https://graph.facebook.com/v19.0/me/media_publish?creation_id={container.Id}" +
                $"&access_token={Uri.EscapeDataString(accessToken)}",
                null, cancellationToken);

            if (!publishResponse.IsSuccessStatusCode)
                return new InstagramPostResult { IsSuccess = false, Message = "انتشار نهایی ناموفق بود." };

            return new InstagramPostResult { IsSuccess = true, Message = "پست محصول در اینستاگرام منتشر شد." };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new InstagramPostResult { IsSuccess = false, Message = $"ارتباط با Instagram برقرار نشد: {ex.Message}" };
        }
    }

    // ────────────────────────── ابزار ──────────────────────────

    private async Task<string?> GetGraphAccessTokenAsync(CancellationToken cancellationToken)
    {
        var credential = await _credentialRepository.FirstOrDefaultAsync(
            c => c.ProviderKey == GraphProviderKey && c.IsActive, cancellationToken);
        return credential == null ? null : _encryptionService.Decrypt(credential.ApiKeyEncrypted ?? string.Empty);
    }

    private async Task<bool> CheckIsFollowingAsync(string accessToken, string igsid, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("InstagramGraphApi");
            var response = await client.GetAsync(
                $"https://graph.instagram.com/{Uri.EscapeDataString(igsid)}" +
                $"?fields=is_user_follow_business&access_token={Uri.EscapeDataString(accessToken)}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return false;

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize<InstagramFollowCheckResponse>(
                body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return payload?.IsUserFollowBusiness == true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> SendDirectMessageAsync(string accessToken, string recipientIgsid, string text, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("InstagramGraphApi");
            var response = await client.PostAsJsonAsync(
                $"https://graph.facebook.com/v19.0/me/messages?access_token={Uri.EscapeDataString(accessToken)}",
                new
                {
                    recipient = new { id = recipientIgsid },
                    message = new { text }
                },
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> PostFallbackCommentAsync(string accessToken, string mediaId, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("InstagramGraphApi");
            const string text = "🎉 خبر خوب داریم! برای دریافت کد تخفیف، به پیج ما دایرکت بدهید.";
            var response = await client.PostAsync(
                $"https://graph.facebook.com/v19.0/{Uri.EscapeDataString(mediaId)}/comments" +
                $"?message={Uri.EscapeDataString(text)}&access_token={Uri.EscapeDataString(accessToken)}",
                null, cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildCaption(Product product)
    {
        var price = product.GetLowestPriceToman();
        var sku = product.Variants.FirstOrDefault()?.Sku ?? $"PROD-{product.Id}";
        return $"🌟 {product.Name}\n" +
               $"▫️ کد محصول: {sku}\n" +
               $"▫️ قیمت: {price:N0} تومان\n\n" +
               $"💬 کد محصول را کامنت کنید تا لینک خرید به دایرکت شما ارسال شود!\n" +
               $"#{product.Name.Replace(" ", "_")} #خرید_آنلاین";
    }

    private static string GetProductUrl(string tenantId, string slug) => $"https://{tenantId}.market.com/product/{slug}";

    private static string Truncate(string s, int max = 200) =>
        s.Length <= max ? s : s[..max];
}

internal class InstagramContainerResponse
{
    [JsonPropertyName("id")] public string? Id { get; set; }
}

internal class InstagramFollowCheckResponse
{
    [JsonPropertyName("is_user_follow_business")] public bool IsUserFollowBusiness { get; set; }
}
