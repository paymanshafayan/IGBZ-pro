namespace IGBZ.Infrastructure.Gateways;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using IGBZ.Application.BNPL;

/// <summary>
/// اسنپ‌پی BNPL — طبق الگوی پلاگین مرجع (github.com/parshan2/SnappPay):
/// ۱) OAuth: POST {base}/api/online/v1/oauth/token (Basic + form با scope=online-merchant)
/// ۲) اجازه: GET {base}/api/online/offer/v1/eligible?amount=
/// ۳) توکن: POST {base}/api/online/payment/v1/token
/// ۴) تایید: POST {base}/api/online/payment/v1/verify
/// apiKey JSON: {username, password, clientId, clientSecret, baseUrl?}
/// </summary>
public class SnapppayBnplGateway : IBnplGateway
{
    public const string Key = "snapppay";

    private const string DefaultBaseUrl = "https://fms-gateway-staging.apps.public.okd4.teh-1.snappcloud.io";

    private readonly HttpClient _httpClient;

    public SnapppayBnplGateway(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string ProviderKey => Key;

    public async Task<BnplEligibilityResult> CheckEligibilityAsync(
        BnplEligibilityRequest request, string apiKey, CancellationToken cancellationToken = default)
    {
        var config = TryParse(apiKey);
        if (config == null)
            return new BnplEligibilityResult { IsEligible = false, Message = "تنظیمات اسنپ‌پی نامعتبر است." };

        var accessToken = await GetTokenAsync(config, cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
            return new BnplEligibilityResult { IsEligible = false, Message = "احراز هویت اسنپ‌پی ناموفق بود." };

        try
        {
            var client = _httpClient;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync(
                $"{GetBase(config)}/api/online/offer/v1/eligible?amount={(long)(request.AmountToman * 10)}",
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var dto = System.Text.Json.JsonSerializer.Deserialize<SnapppayResultDto<SnapppayEligibleDto>>(
                body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dto?.Successful == true && dto.Response?.Eligible == true)
                return new BnplEligibilityResult { IsEligible = true, Message = "واجد شرایط است." };

            return new BnplEligibilityResult { IsEligible = false, Message = dto?.ErrorData?.Message ?? "واجد شرایط نیست." };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new BnplEligibilityResult { IsEligible = false, Message = $"ارتباط با اسنپ‌پی برقرار نشد: {ex.Message}" };
        }
    }

    public async Task<BnplPaymentRequestResult> RequestPaymentAsync(
        BnplPaymentRequest request, string apiKey, CancellationToken cancellationToken = default)
    {
        var config = TryParse(apiKey);
        if (config == null)
            return new BnplPaymentRequestResult { IsSuccess = false, ErrorMessage = "تنظیمات اسنپ‌پی نامعتبر است." };

        var accessToken = await GetTokenAsync(config, cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
            return new BnplPaymentRequestResult { IsSuccess = false, ErrorMessage = "احراز هویت اسنپ‌پی ناموفق بود." };

        try
        {
            var client = _httpClient;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var payload = new
            {
                transactionId = request.TransactionId,
                amount = (int)(request.AmountToman * 10),
                returnURL = request.CallbackUrl,
                paymentMethodTypeDto = "INSTALLMENT",
                mobile = request.CustomerMobile,
                cartList = new[]
                {
                    new
                    {
                        cartId = 1,
                        totalAmount = (int)(request.AmountToman * 10),
                        isShipmentIncluded = false,
                        shippingAmount = 0,
                        isTaxIncluded = false,
                        taxAmount = 0,
                        cartItems = new[]
                        {
                            new { id = 1, name = "سفارش", count = 1, amount = (int)(request.AmountToman * 10), category = "General" }
                        }
                    }
                }
            };

            var response = await client.PostAsJsonAsync(
                $"{GetBase(config)}/api/online/payment/v1/token", payload, cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var dto = System.Text.Json.JsonSerializer.Deserialize<SnapppayResultDto<SnapppayPaymentTokenDto>>(
                body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dto?.Successful == true && !string.IsNullOrWhiteSpace(dto.Response?.PaymentPageUrl))
            {
                return new BnplPaymentRequestResult
                {
                    IsSuccess = true,
                    RedirectUrl = dto.Response.PaymentPageUrl,
                    PaymentToken = dto.Response.PaymentToken
                };
            }

            return new BnplPaymentRequestResult { IsSuccess = false, ErrorMessage = dto?.ErrorData?.Message ?? "اسنپ‌پی توکن را رد کرد." };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new BnplPaymentRequestResult { IsSuccess = false, ErrorMessage = $"ارتباط با اسنپ‌پی برقرار نشد: {ex.Message}" };
        }
    }

    public async Task<BnplVerifyResult> VerifyPaymentAsync(
        BnplVerifyRequest request, string apiKey, CancellationToken cancellationToken = default)
    {
        var config = TryParse(apiKey);
        if (config == null)
            return new BnplVerifyResult { IsSuccess = false, ErrorMessage = "تنظیمات اسنپ‌پی نامعتبر است." };

        var accessToken = await GetTokenAsync(config, cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
            return new BnplVerifyResult { IsSuccess = false, ErrorMessage = "احراز هویت اسنپ‌پی ناموفق بود." };

        try
        {
            var client = _httpClient;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.PostAsJsonAsync(
                $"{GetBase(config)}/api/online/payment/v1/verify",
                new { paymentToken = request.PaymentToken },
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var dto = System.Text.Json.JsonSerializer.Deserialize<SnapppayResultDto<SnapppayVerifyDto>>(
                body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dto?.Successful == true)
            {
                return new BnplVerifyResult
                {
                    IsSuccess = true,
                    TrackingCode = dto.Response?.TransactionId
                };
            }

            return new BnplVerifyResult { IsSuccess = false, ErrorMessage = dto?.ErrorData?.Message ?? "تایید اسنپ‌پی ناموفق بود." };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new BnplVerifyResult { IsSuccess = false, ErrorMessage = $"ارتباط با اسنپ‌پی برقرار نشد: {ex.Message}" };
        }
    }

    private async Task<string?> GetTokenAsync(SnapppayConfig config, CancellationToken cancellationToken)
    {
        var client = _httpClient;
        var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{config.ClientId}:{config.ClientSecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "scope", "online-merchant" },
            { "username", config.Username },
            { "password", config.Password }
        });

        var response = await client.PostAsync($"{GetBase(config)}/api/online/v1/oauth/token", form, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var token = await response.Content.ReadFromJsonAsync<SnapppayTokenDto>(cancellationToken: cancellationToken);
        return token?.AccessToken;
    }

    private static string GetBase(SnapppayConfig config) =>
        string.IsNullOrWhiteSpace(config.BaseUrl) ? DefaultBaseUrl : config.BaseUrl.TrimEnd('/');

    private static SnapppayConfig? TryParse(string apiKey)
    {
        try
        {
            var config = System.Text.Json.JsonSerializer.Deserialize<SnapppayConfig>(apiKey);
            if (config == null || string.IsNullOrWhiteSpace(config.Username) || string.IsNullOrWhiteSpace(config.Password)
                || string.IsNullOrWhiteSpace(config.ClientId) || string.IsNullOrWhiteSpace(config.ClientSecret))
                return null;
            return config;
        }
        catch
        {
            return null;
        }
    }

    private class SnapppayConfig
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string? BaseUrl { get; set; }
    }

    private class SnapppayTokenDto
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    }

    private class SnapppayResultDto<T>
    {
        [JsonPropertyName("successful")] public bool Successful { get; set; }
        [JsonPropertyName("response")] public T? Response { get; set; }
        [JsonPropertyName("errorData")] public SnapppayErrorData? ErrorData { get; set; }
    }

    private class SnapppayErrorData
    {
        [JsonPropertyName("message")] public string? Message { get; set; }
    }

    private class SnapppayEligibleDto
    {
        [JsonPropertyName("eligible")] public bool Eligible { get; set; }
    }

    private class SnapppayPaymentTokenDto
    {
        [JsonPropertyName("paymentToken")] public string? PaymentToken { get; set; }
        [JsonPropertyName("paymentPageUrl")] public string? PaymentPageUrl { get; set; }
    }

    private class SnapppayVerifyDto
    {
        [JsonPropertyName("transactionId")] public string? TransactionId { get; set; }
    }
}
