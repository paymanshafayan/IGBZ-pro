namespace IGBZ.Application.Logistics;

using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using IGBZ.Application.Abstractions;
using IGBZ.Domain.Integration;

/// <summary>
/// لجستیک — مسیر بر اساس وزن/شهر (قطعی)، PIN با RandomNumberGenerator،
/// ثبت مرسوله در تاپین با HTTP واقعی و خواندن کد رهگیری از پاسخ.
/// </summary>
public class LogisticsService : ILogisticsService
{
    private const string TapinProviderKey = "tapin";

    private readonly ITenantScopedRepository<IntegrationCredential> _credentialRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IHttpClientFactory _httpClientFactory;

    public LogisticsService(
        ITenantScopedRepository<IntegrationCredential> credentialRepository,
        IEncryptionService encryptionService,
        IHttpClientFactory httpClientFactory)
    {
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
        _httpClientFactory = httpClientFactory;
    }

    public RouteCategoryResult CategorizeRoute(decimal weightKg, string destinationCity, bool isExpressNeeded)
    {
        if (weightKg > 30)
        {
            return new RouteCategoryResult
            {
                RouteType = "HEAVY_FREIGHT",
                CarrierName = "باربری / تیپاژ سنگین",
                EstimatedCostToman = 150_000,
                DeliveryPinRequired = true
            };
        }

        if (string.Equals(destinationCity, "تهران", StringComparison.OrdinalIgnoreCase) || isExpressNeeded)
        {
            return new RouteCategoryResult
            {
                RouteType = "EXPRESS_COURIER",
                CarrierName = "اسنپ‌باکس / الوپیک (ارسال فوری درون‌شهری)",
                EstimatedCostToman = 65_000,
                DeliveryPinRequired = true
            };
        }

        return new RouteCategoryResult
        {
            RouteType = "NATIONAL_POST",
            CarrierName = "پست پیشتاز (اتصال تاپین / پستکس)",
            EstimatedCostToman = 45_000,
            DeliveryPinRequired = false
        };
    }

    public string GenerateDeliveryPin()
    {
        // RandomNumberGenerator — هرگز new Random(seed) (سند بخش ۱۸.۴)
        return RandomNumberGenerator.GetInt32(1000, 10_000).ToString();
    }

    public async Task<ShipmentRegistrationResult> RegisterShipmentAsync(
        string orderId, decimal weightKg, string destinationCity, string recipientAddress, string recipientPhone,
        bool isCod, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(recipientAddress) || string.IsNullOrWhiteSpace(recipientPhone))
            return new ShipmentRegistrationResult { IsSuccess = false, Message = "اطلاعات مرسوله ناقص است." };

        var token = await GetTapinTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
            return new ShipmentRegistrationResult { IsSuccess = false, Message = "توکن تاپین فعال نیست." };

        var route = CategorizeRoute(weightKg, destinationCity, false);
        var deliveryPin = route.DeliveryPinRequired ? GenerateDeliveryPin() : null;

        try
        {
            var client = _httpClientFactory.CreateClient("TapinPost");
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var response = await client.PostAsJsonAsync(
                "https://api.tapin.ir/v1/shipments",
                new TapinShipmentPayload
                {
                    OrderId = orderId,
                    RouteType = route.RouteType,
                    CarrierName = route.CarrierName,
                    WeightKg = weightKg,
                    DestinationCity = destinationCity,
                    RecipientAddress = recipientAddress,
                    RecipientPhone = recipientPhone,
                    IsCod = isCod,
                    DeliveryPin = deliveryPin
                },
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new ShipmentRegistrationResult
                {
                    IsSuccess = false,
                    Message = $"تاپین خطا داد (کد {(int)response.StatusCode}): {Truncate(body)}"
                };

            var payload = JsonSerializer.Deserialize<TapinShipmentResponse>(
                body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (string.IsNullOrWhiteSpace(payload?.TrackingCode))
                return new ShipmentRegistrationResult { IsSuccess = false, Message = "پاسخ تاپین بدون کد رهگیری بود." };

            return new ShipmentRegistrationResult
            {
                IsSuccess = true,
                TrackingCode = payload.TrackingCode,
                DeliveryPin = deliveryPin,
                Message = "مرسوله در تاپین ثبت شد."
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ShipmentRegistrationResult { IsSuccess = false, Message = $"ارتباط با تاپین برقرار نشد: {ex.Message}" };
        }
    }

    private async Task<string?> GetTapinTokenAsync(CancellationToken cancellationToken)
    {
        var credential = await _credentialRepository.FirstOrDefaultAsync(
            c => c.ProviderKey == TapinProviderKey && c.IsActive, cancellationToken);
        return credential == null ? null : _encryptionService.Decrypt(credential.ApiKeyEncrypted ?? string.Empty);
    }

    private static string Truncate(string s, int max = 200) =>
        s.Length <= max ? s : s[..max];
}

internal class TapinShipmentPayload
{
    [JsonPropertyName("orderId")] public string OrderId { get; set; } = string.Empty;
    [JsonPropertyName("routeType")] public string RouteType { get; set; } = string.Empty;
    [JsonPropertyName("carrierName")] public string CarrierName { get; set; } = string.Empty;
    [JsonPropertyName("weightKg")] public decimal WeightKg { get; set; }
    [JsonPropertyName("destinationCity")] public string DestinationCity { get; set; } = string.Empty;
    [JsonPropertyName("recipientAddress")] public string RecipientAddress { get; set; } = string.Empty;
    [JsonPropertyName("recipientPhone")] public string RecipientPhone { get; set; } = string.Empty;
    [JsonPropertyName("isCod")] public bool IsCod { get; set; }
    [JsonPropertyName("deliveryPin")] public string? DeliveryPin { get; set; }
}

internal class TapinShipmentResponse
{
    [JsonPropertyName("trackingCode")] public string? TrackingCode { get; set; }
}
