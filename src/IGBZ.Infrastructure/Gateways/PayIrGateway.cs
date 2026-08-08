namespace IGBZ.Infrastructure.Gateways;

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using IGBZ.Application.Payments;

/// <summary>
/// درگاه pay.ir — طبق مستندات رسمی https://docs.pay.ir/gateway:
/// ۱) POST https://pay.ir/pg/send ← { api, amount(ریال), redirect, factorNumber, description } → { status, token }
/// ۲) هدایت کاربر به https://pay.ir/pg/{token}
/// ۳) POST https://pay.ir/pg/verify ← { api, token } → { status, amount, transId }
/// موفقیت فقط با status==1 و تطابق مبلغ (در PaymentService بررسی می‌شود).
/// </summary>
public class PayIrGateway : IPaymentGateway
{
    public const string Key = "payir";

    private const string SendEndpoint = "https://pay.ir/pg/send";
    private const string VerifyEndpoint = "https://pay.ir/pg/verify";
    private const string GatewayPage = "https://pay.ir/pg";

    private readonly HttpClient _httpClient;

    public PayIrGateway(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string GatewayName => Key;

    public async Task<PaymentGatewayRequestResult> RequestAsync(
        PaymentGatewayRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Extra))
            return new PaymentGatewayRequestResult { IsSuccess = false, ErrorMessage = "کلید API pay.ir (Extra) تنظیم نشده است." };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(SendEndpoint, new PayIrSendPayload
            {
                Api = request.Extra,
                Amount = (long)(request.AmountToman * 10), // تومان → ریال
                Redirect = request.CallbackUrl,
                FactorNumber = request.TrackingNumber,
                Description = $"پرداخت {request.TrackingNumber}"
            }, cancellationToken);

            var payload = await response.Content.ReadFromJsonAsync<PayIrSendResponse>(cancellationToken: cancellationToken);

            if (payload?.Status == 1 && !string.IsNullOrWhiteSpace(payload.Token))
            {
                return new PaymentGatewayRequestResult
                {
                    IsSuccess = true,
                    RedirectUrl = $"{GatewayPage}/{payload.Token}"
                };
            }

            return new PaymentGatewayRequestResult
            {
                IsSuccess = false,
                ErrorMessage = payload?.ErrorMessage ?? payload?.Message ?? $"pay.ir خطا داد (کد {(int)response.StatusCode})"
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new PaymentGatewayRequestResult { IsSuccess = false, ErrorMessage = $"ارتباط با pay.ir برقرار نشد: {ex.Message}" };
        }
    }

    public async Task<PaymentGatewayVerifyResult> VerifyAsync(
        PaymentGatewayVerifyRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(VerifyEndpoint, new PayIrVerifyPayload
            {
                Api = request.Extra ?? string.Empty,
                Token = request.TrackingNumber
            }, cancellationToken);

            var payload = await response.Content.ReadFromJsonAsync<PayIrVerifyResponse>(cancellationToken: cancellationToken);

            if (payload?.Status == 1)
            {
                return new PaymentGatewayVerifyResult
                {
                    IsSuccess = true,
                    BankRefId = payload.TransId
                };
            }

            return new PaymentGatewayVerifyResult
            {
                IsSuccess = false,
                ErrorMessage = payload?.Message ?? "تایید pay.ir ناموفق بود."
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new PaymentGatewayVerifyResult { IsSuccess = false, ErrorMessage = $"ارتباط با pay.ir برقرار نشد: {ex.Message}" };
        }
    }
}

internal class PayIrSendPayload
{
    [JsonPropertyName("api")] public string Api { get; set; } = string.Empty;
    [JsonPropertyName("amount")] public long Amount { get; set; }
    [JsonPropertyName("redirect")] public string Redirect { get; set; } = string.Empty;
    [JsonPropertyName("factorNumber")] public string FactorNumber { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
}

internal class PayIrSendResponse
{
    [JsonPropertyName("status")] public int Status { get; set; }
    [JsonPropertyName("token")] public string? Token { get; set; }
    [JsonPropertyName("errorMessage")] public string? ErrorMessage { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}

internal class PayIrVerifyPayload
{
    [JsonPropertyName("api")] public string Api { get; set; } = string.Empty;
    [JsonPropertyName("token")] public string Token { get; set; } = string.Empty;
}

internal class PayIrVerifyResponse
{
    [JsonPropertyName("status")] public int Status { get; set; }
    [JsonPropertyName("amount")] public long Amount { get; set; }
    [JsonPropertyName("transId")] public string? TransId { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}
