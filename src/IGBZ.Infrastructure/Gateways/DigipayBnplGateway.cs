namespace IGBZ.Infrastructure.Gateways;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using IGBZ.Application.BNPL;

/// <summary>
/// دیجی‌پی BNPL — طبق مستندات رسمی mydigipay.com/developers/docs/upg:
/// ۱) OAuth: POST {base}/oauth/token با Basic(client_id:client_secret) + form {username,password,grant_type}
/// ۲) تیکت: POST {base}/tickets/business?type=11 با هدر Agent/Digipay-Version + basketDetailsDto
/// ۳) تایید: POST {base}/purchases/verify?type=13 با {trackingCode, providerId}
/// apiKey در اینجا JSON حاوی username/password/clientId/clientSecret/environment است.
/// </summary>
public class DigipayBnplGateway : IBnplGateway
{
    public const string Key = "digipay";

    private const string UatBase = "https://uat.mydigipay.info/digipay/api";
    private const string LiveBase = "https://api.mydigipay.com/digipay/api";

    private readonly HttpClient _httpClient;

    public DigipayBnplGateway(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string ProviderKey => Key;

    public async Task<BnplEligibilityResult> CheckEligibilityAsync(
        BnplEligibilityRequest request, string apiKey, CancellationToken cancellationToken = default)
    {
        // دیجی‌پی: Eligibility نهایی هنگام تیکت/verify مشخص می‌شود؛ در این مرحله فقط اعتبار مبلغ بررسی می‌شود.
        if (request.AmountToman <= 0)
            return new BnplEligibilityResult { IsEligible = false, Message = "مبلغ نامعتبر است." };

        return new BnplEligibilityResult { IsEligible = true, Message = "امکان پرداخت اعتباری دیجی‌پی بررسی می‌شود." };
    }

    public async Task<BnplPaymentRequestResult> RequestPaymentAsync(
        BnplPaymentRequest request, string apiKey, CancellationToken cancellationToken = default)
    {
        var config = TryParse(apiKey);
        if (config == null)
            return new BnplPaymentRequestResult { IsSuccess = false, ErrorMessage = "تنظیمات دیجی‌پی نامعتبر است." };

        var baseUrl = config.Environment == "live" ? LiveBase : UatBase;
        var accessToken = await GetTokenAsync(baseUrl, config, cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
            return new BnplPaymentRequestResult { IsSuccess = false, ErrorMessage = "احراز هویت دیجی‌پی ناموفق بود." };

        var client = _httpClient;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.Remove("Agent");
        client.DefaultRequestHeaders.Add("Agent", "WEB");
        client.DefaultRequestHeaders.Remove("Digipay-Version");
        client.DefaultRequestHeaders.Add("Digipay-Version", "2022-02-02");

        var providerId = request.TransactionId;

        var payload = new
        {
            cellNumber = request.CustomerMobile,
            amount = (long)(request.AmountToman * 10),
            providerId,
            callbackUrl = request.CallbackUrl,
            basketDetailsDto = new
            {
                basketId = $"basket-{request.TransactionId}",
                items = new[]
                {
                    new
                    {
                        sellerId = "igbz",
                        supplierId = "igbz",
                        productCode = request.TransactionId,
                        brand = "IGBZ",
                        productType = 3, // سرویس
                        count = 1,
                        categoryId = "General"
                    }
                }
            }
        };

        try
        {
            var response = await client.PostAsJsonAsync($"{baseUrl}/tickets/business?type=11", payload, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            var ticket = System.Text.Json.JsonSerializer.Deserialize<DigipayTicketResponse>(
                body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (ticket?.Result?.Status == 0 && !string.IsNullOrWhiteSpace(ticket.RedirectUrl))
            {
                return new BnplPaymentRequestResult
                {
                    IsSuccess = true,
                    RedirectUrl = ticket.RedirectUrl,
                    PaymentToken = ticket.Ticket
                };
            }

            return new BnplPaymentRequestResult
            {
                IsSuccess = false,
                ErrorMessage = ticket?.Result?.Message ?? "دیجی‌پی تیکت را رد کرد."
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new BnplPaymentRequestResult { IsSuccess = false, ErrorMessage = $"ارتباط با دیجی‌پی برقرار نشد: {ex.Message}" };
        }
    }

    public async Task<BnplVerifyResult> VerifyPaymentAsync(
        BnplVerifyRequest request, string apiKey, CancellationToken cancellationToken = default)
    {
        var config = TryParse(apiKey);
        if (config == null)
            return new BnplVerifyResult { IsSuccess = false, ErrorMessage = "تنظیمات دیجی‌پی نامعتبر است." };

        var baseUrl = config.Environment == "live" ? LiveBase : UatBase;
        var accessToken = await GetTokenAsync(baseUrl, config, cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
            return new BnplVerifyResult { IsSuccess = false, ErrorMessage = "احراز هویت دیجی‌پی ناموفق بود." };

        try
        {
            var client = _httpClient;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.PostAsJsonAsync(
                $"{baseUrl}/purchases/verify?type=13",
                new { trackingCode = request.PaymentToken, providerId = request.TransactionId },
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var verify = System.Text.Json.JsonSerializer.Deserialize<DigipayVerifyResponse>(
                body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (verify?.Result?.Status == 0)
            {
                return new BnplVerifyResult
                {
                    IsSuccess = true,
                    TrackingCode = verify.TrackingCode
                };
            }

            return new BnplVerifyResult { IsSuccess = false, ErrorMessage = verify?.Result?.Message ?? "تایید دیجی‌پی ناموفق بود." };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new BnplVerifyResult { IsSuccess = false, ErrorMessage = $"ارتباط با دیجی‌پی برقرار نشد: {ex.Message}" };
        }
    }

    private async Task<string?> GetTokenAsync(string baseUrl, DigipayConfig config, CancellationToken cancellationToken)
    {
        var client = _httpClient;
        var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{config.ClientId}:{config.ClientSecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "username", config.Username },
            { "password", config.Password }
        });

        var response = await client.PostAsync($"{baseUrl}/oauth/token", form, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var token = await response.Content.ReadFromJsonAsync<DigipayTokenResponse>(cancellationToken: cancellationToken);
        return token?.AccessToken;
    }

    private static DigipayConfig? TryParse(string apiKey)
    {
        try
        {
            var config = System.Text.Json.JsonSerializer.Deserialize<DigipayConfig>(apiKey);
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

    private class DigipayConfig
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Environment { get; set; } = "uat";
    }

    private class DigipayTokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    }

    private class DigipayTicketResponse
    {
        [JsonPropertyName("result")] public DigipayResult? Result { get; set; }
        [JsonPropertyName("ticket")] public string? Ticket { get; set; }
        [JsonPropertyName("redirectUrl")] public string? RedirectUrl { get; set; }
    }

    private class DigipayVerifyResponse
    {
        [JsonPropertyName("result")] public DigipayResult? Result { get; set; }
        [JsonPropertyName("trackingCode")] public string? TrackingCode { get; set; }
    }

    private class DigipayResult
    {
        [JsonPropertyName("status")] public int Status { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
    }
}
