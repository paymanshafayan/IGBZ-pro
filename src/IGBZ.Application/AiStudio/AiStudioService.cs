namespace IGBZ.Application.AiStudio;

using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using IGBZ.Application.Abstractions;
using IGBZ.Domain.Integration;

/// <summary>
/// پیاده‌سازی استودیوی AI — هر عملیات با HTTP واقعی به Provider (با اعتبارنامهٔ تننت)
/// فراخوانی می‌شود و خروجی از پاسخ واقعی خوانده می‌شود (هرگز دستکاری URL ورودی).
/// ⚠️ Endpointها در <see cref="IntegrationCredential.EndpointOverrideUrl"/> قابل تنظیم‌اند؛
/// در غیر این صورت از آدرس‌های نمادین پیش‌فرض استفاده می‌شود (تا مستندات واقعی هر Provider).
/// </summary>
public class AiStudioService : IAiStudioService
{
    private const string ImageProviderKey = "deepfa";
    private const string VideoProviderKey = "atna";
    private const string VoiceProviderKey = "vira";
    private const string TranslationProviderKey = "tarjomyar";

    private readonly ITenantScopedRepository<IntegrationCredential> _credentialRepository;
    private readonly IHttpClientFactory _httpClientFactory;

    public AiStudioService(
        ITenantScopedRepository<IntegrationCredential> credentialRepository,
        IHttpClientFactory httpClientFactory)
    {
        _credentialRepository = credentialRepository;
        _httpClientFactory = httpClientFactory;
    }

    private async Task<(string apiKey, string? endpoint)> GetProviderAsync(string providerKey, CancellationToken cancellationToken)
    {
        var credential = await _credentialRepository.FirstOrDefaultAsync(
            c => c.ProviderKey == providerKey && c.IsActive, cancellationToken);
        return (credential?.ApiKeyEncrypted ?? string.Empty, credential?.EndpointOverrideUrl);
    }

    public async Task<AiStudioResult> EnhancePhotoAsync(EnhancePhotoRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ImageUrl))
            return new AiStudioResult { IsSuccess = false, ErrorMessage = "آدرس تصویر الزامی است." };

        var (apiKey, endpoint) = await GetProviderAsync(ImageProviderKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            return new AiStudioResult { IsSuccess = false, ErrorMessage = "کلید AI تصویر (deepfa) فعال نیست." };

        try
        {
            var client = _httpClientFactory.CreateClient("AiImageStudio");
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await client.PostAsJsonAsync(
                endpoint ?? "https://api.ai-image-studio.local/v1/enhance",
                new
                {
                    image_url = request.ImageUrl,
                    background_preset = request.BackgroundPreset,
                    sku_code = request.SkuCode
                },
                cancellationToken);

            return await ParseOutputAsync(response, "سرویس ادیت تصویر", cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new AiStudioResult { IsSuccess = false, ErrorMessage = $"ارتباط با سرویس تصویر برقرار نشد: {ex.Message}" };
        }
    }

    public async Task<AiStudioResult> GenerateVideoStoryAsync(VideoStoryRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProductTitle))
            return new AiStudioResult { IsSuccess = false, ErrorMessage = "عنوان محصول الزامی است." };

        var (apiKey, endpoint) = await GetProviderAsync(VideoProviderKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            return new AiStudioResult { IsSuccess = false, ErrorMessage = "کلید AI ویدیو (atna) فعال نیست." };

        try
        {
            var client = _httpClientFactory.CreateClient("AiVideoStudio");
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await client.PostAsJsonAsync(
                endpoint ?? "https://api.ai-video-studio.local/v1/story",
                new
                {
                    product_id = request.ProductId,
                    title = request.ProductTitle,
                    price_toman = request.PriceToman,
                    background_music_track_id = request.BackgroundMusicTrackId
                },
                cancellationToken);

            return await ParseOutputAsync(response, "سرویس ویدیو", cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new AiStudioResult { IsSuccess = false, ErrorMessage = $"ارتباط با سرویس ویدیو برقرار نشد: {ex.Message}" };
        }
    }

    public async Task<AiStudioResult> GenerateVoiceOverAsync(VoiceOverRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return new AiStudioResult { IsSuccess = false, ErrorMessage = "متن صداپیشگی الزامی است." };

        var (apiKey, endpoint) = await GetProviderAsync(VoiceProviderKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            return new AiStudioResult { IsSuccess = false, ErrorMessage = "کلید AI صدا (vira) فعال نیست." };

        try
        {
            var client = _httpClientFactory.CreateClient("AiTtsProvider");
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await client.PostAsJsonAsync(
                endpoint ?? "https://api.ai-tts.local/v1/synthesize",
                new
                {
                    text = request.Text,
                    voice_gender = request.SpeakerGender,
                    language = "fa-IR"
                },
                cancellationToken);

            return await ParseOutputAsync(response, "سرویس صدا", cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new AiStudioResult { IsSuccess = false, ErrorMessage = $"ارتباط با سرویس صدا برقرار نشد: {ex.Message}" };
        }
    }

    public async Task<TranslateResult> AutoTranslateAsync(TranslateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return new TranslateResult { IsSuccess = false, ErrorMessage = "متن ترجمه الزامی است." };

        var (apiKey, endpoint) = await GetProviderAsync(TranslationProviderKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            return new TranslateResult { IsSuccess = false, ErrorMessage = "کلید سرویس ترجمه (tarjomyar) فعال نیست." };

        try
        {
            var client = _httpClientFactory.CreateClient("TranslationProvider");
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await client.PostAsJsonAsync(
                endpoint ?? "https://api.translation.local/v1/translate",
                new { text = request.Text, target_language = request.TargetLanguageCode },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return new TranslateResult { IsSuccess = false, ErrorMessage = $"سرویس ترجمه خطا داد (کد {(int)response.StatusCode}): {Truncate(errBody)}" };
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize<TranslationResponse>(
                body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (string.IsNullOrWhiteSpace(payload?.TranslatedText))
                return new TranslateResult { IsSuccess = false, ErrorMessage = "پاسخ ترجمه بدون متن بود." };

            return new TranslateResult { IsSuccess = true, TranslatedText = payload.TranslatedText };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new TranslateResult { IsSuccess = false, ErrorMessage = $"ارتباط با سرویس ترجمه برقرار نشد: {ex.Message}" };
        }
    }

    public SeoMetaResult GenerateSeoMeta(string productName, string? description)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("نام محصول الزامی است.", nameof(productName));

        var safeDescription = description ?? string.Empty;
        var snippet = safeDescription.Length > 100 ? safeDescription[..100] : safeDescription;

        return new SeoMetaResult
        {
            MetaTitle = $"{productName} | خرید آنلاین با بهترین قیمت و ارسال سریع",
            MetaDescription = $"خرید اینترنتی {productName}. {snippet}... ضمانت اصالت و بازگشت ۷ روزه.",
            MetaKeywords = $"#{productName.Replace(" ", "_")} #خرید_آنلاین #فروشگاه_اینترنتی"
        };
    }

    // ────────────────────────── ابزار ──────────────────────────

    private static async Task<AiStudioResult> ParseOutputAsync(
        HttpResponseMessage response, string serviceName, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new AiStudioResult
            {
                IsSuccess = false,
                ErrorMessage = $"{serviceName} خطا داد (کد {(int)response.StatusCode}): {Truncate(body)}"
            };

        var payload = JsonSerializer.Deserialize<AiOutputResponse>(
            body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (string.IsNullOrWhiteSpace(payload?.OutputUrl))
            return new AiStudioResult { IsSuccess = false, ErrorMessage = $"{serviceName} نتیجه‌ای بدون URL برگرداند." };

        return new AiStudioResult { IsSuccess = true, OutputUrl = payload.OutputUrl };
    }

    private static string Truncate(string s, int max = 200) =>
        s.Length <= max ? s : s[..max];
}

internal class AiOutputResponse
{
    [JsonPropertyName("output_url")] public string? OutputUrl { get; set; }
    [JsonPropertyName("url")] public string? Url { get; set; }
}

internal class TranslationResponse
{
    [JsonPropertyName("translated_text")] public string? TranslatedText { get; set; }
    [JsonPropertyName("translation")] public string? Translation { get; set; }
}
