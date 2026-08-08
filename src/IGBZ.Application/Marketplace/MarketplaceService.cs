namespace IGBZ.Application.Marketplace;

using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using IGBZ.Application.Abstractions;
using IGBZ.Domain.Catalog;
using IGBZ.Domain.Integration;

/// <summary>
/// پیاده‌سازی مارکت‌پلیس — فید ترب از کاتالوگ واقعی؛ دیجی‌کالا و دیوار با HTTP واقعی
/// (نتایج از کد وضعیت واقعی خوانده می‌شوند؛ هرگز موفقیت فرضی).
/// </summary>
public class MarketplaceService : IMarketplaceService
{
    private const string DigikalaProviderKey = "digikala";
    private const string DivarProviderKey = "divar";

    private readonly ITenantScopedRepository<Product> _productRepository;
    private readonly ITenantScopedRepository<IntegrationCredential> _credentialRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IHttpClientFactory _httpClientFactory;

    public MarketplaceService(
        ITenantScopedRepository<Product> productRepository,
        ITenantScopedRepository<IntegrationCredential> credentialRepository,
        IEncryptionService encryptionService,
        IHttpClientFactory httpClientFactory)
    {
        _productRepository = productRepository;
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
        _httpClientFactory = httpClientFactory;
    }

    private async Task<string?> GetApiKeyAsync(string providerKey, CancellationToken cancellationToken)
    {
        var credential = await _credentialRepository.FirstOrDefaultAsync(
            c => c.ProviderKey == providerKey && c.IsActive, cancellationToken);
        return credential == null ? null : _encryptionService.Decrypt(credential.ApiKeyEncrypted ?? string.Empty);
    }

    // ────────────────────────── ترب (فید) ──────────────────────────

    public async Task<TorobFeedResult> GetTorobFeedAsync(CancellationToken cancellationToken = default)
    {
        // فید از کاتالوگ واقعی (فقط منتشرشده و حذف‌نشده) — سند بخش ۱۰.۲
        var products = await _productRepository.FindAsync(
            p => p.IsPublished && !p.Deleted, cancellationToken);

        var items = products
            .OrderByDescending(p => p.CreatedOnUtc)
            .Select(p => new TorobProductItem
            {
                PageUniqueId = $"prod-{p.Id}",
                Title = p.Name,
                PriceToman = p.GetLowestPriceToman(),
                OldPriceToman = p.Variants
                    .Where(v => v.OldPriceToman.HasValue)
                    .Select(v => v.OldPriceToman)
                    .OrderByDescending(v => v)
                    .FirstOrDefault(),
                IsAvailability = p.Variants.Any(v => v.AvailableQuantity > 0),
                ProductUrl = $"/product/{p.Slug}",
                ImageUrl = p.Images.FirstOrDefault()
            })
            .ToList();

        return new TorobFeedResult
        {
            TotalCount = items.Count,
            Products = items
        };
    }

    // ────────────────────────── دیجی‌کالا ──────────────────────────

    public async Task<MarketplaceSyncResult> SyncStockAndPriceWithDigikalaAsync(
        string sellerVariantId, int stockCount, decimal priceToman,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sellerVariantId))
            return new MarketplaceSyncResult { IsSuccess = false, Message = "شناسهٔ Variant دیجی‌کالا الزامی است." };

        var token = await GetApiKeyAsync(DigikalaProviderKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
            return new MarketplaceSyncResult { IsSuccess = false, Message = "توکن فروشندهٔ دیجی‌کالا فعال نیست." };

        try
        {
            var client = _httpClientFactory.CreateClient("DigikalaOpenApi");
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var response = await client.PatchAsJsonAsync(
                $"https://openapi.digikala.com/v1/seller/variants/{sellerVariantId}/stock-price",
                new DigikalaStockPricePayload
                {
                    StockCount = stockCount,
                    PriceRials = (long)(priceToman * 10)
                },
                cancellationToken);

            if (response.IsSuccessStatusCode)
                return new MarketplaceSyncResult { IsSuccess = true, Message = "موجودی/قیمت در دیجی‌کالا به‌روزرسانی شد." };

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new MarketplaceSyncResult
            {
                IsSuccess = false,
                Message = $"دیجی‌کالا خطا داد (کد {(int)response.StatusCode}): {Truncate(body)}"
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new MarketplaceSyncResult { IsSuccess = false, Message = $"ارتباط با دیجی‌کالا برقرار نشد: {ex.Message}" };
        }
    }

    // ────────────────────────── دیوار (Kenar) ──────────────────────────

    public async Task<DivarPostResult> PublishPostOnDivarAsync(
        string title, string description, decimal priceToman, string? imageUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            return new DivarPostResult { IsSuccess = false, Message = "عنوان آگهی الزامی است." };

        var token = await GetApiKeyAsync(DivarProviderKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
            return new DivarPostResult { IsSuccess = false, Message = "توکن کنار دیوار فعال نیست." };

        try
        {
            var client = _httpClientFactory.CreateClient("KenarDivarApi");
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var response = await client.PostAsJsonAsync(
                "https://api.divar.ir/v1/open-platform/finder/post",
                new DivarCreatePostPayload
                {
                    Title = title,
                    Description = description,
                    PriceRials = (long)(priceToman * 10),
                    ImageUrl = imageUrl
                },
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new DivarPostResult
                {
                    IsSuccess = false,
                    Message = $"دیوار درخواست را رد کرد (کد {(int)response.StatusCode}): {Truncate(body)}"
                };

            var payload = JsonSerializer.Deserialize<DivarCreatePostResponse>(
                body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new DivarPostResult
            {
                IsSuccess = true,
                PostToken = payload?.PostToken,
                PostUrl = payload?.PostUrl,
                Message = "آگهی در دیوار منتشر شد."
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new DivarPostResult { IsSuccess = false, Message = $"ارتباط با دیوار برقرار نشد: {ex.Message}" };
        }
    }

    private static string Truncate(string s, int max = 200) =>
        s.Length <= max ? s : s[..max];
}

internal class DigikalaStockPricePayload
{
    [JsonPropertyName("stock_count")] public int StockCount { get; set; }
    [JsonPropertyName("price_rials")] public long PriceRials { get; set; }
}

internal class DivarCreatePostPayload
{
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("price_rials")] public long PriceRials { get; set; }
    [JsonPropertyName("image_url")] public string? ImageUrl { get; set; }
}

internal class DivarCreatePostResponse
{
    [JsonPropertyName("post_token")] public string? PostToken { get; set; }
    [JsonPropertyName("post_url")] public string? PostUrl { get; set; }
}
