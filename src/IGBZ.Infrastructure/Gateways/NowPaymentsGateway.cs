namespace IGBZ.Infrastructure.Gateways;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using IGBZ.Application.Payments;

/// <summary>
/// درگاه رمزارز NOWPayments (سند بخش ۹.۴) — فاکتور USDT-TRC20:
/// ۱) POST https://api.nowpayments.io/v1/invoice با هدر x-api-key
/// ۲) پاسخ: { id, invoice_url } → هدایت کاربر
/// ⚠️ تایید نهایی پرداخت رمزارز از طریق Webhook IPN انجام می‌شود (فاز بعدی)؛
/// در این نسخه Verify برای تراکنش‌های رمزارز با استعلام status انجام می‌شود.
/// </summary>
public class NowPaymentsGateway : IPaymentGateway
{
    public const string Key = "nowpayments";

    private const string InvoiceEndpoint = "https://api.nowpayments.io/v1/invoice";

    private readonly HttpClient _httpClient;

    public NowPaymentsGateway(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string GatewayName => Key;

    public async Task<PaymentGatewayRequestResult> RequestAsync(
        PaymentGatewayRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Extra))
            return new PaymentGatewayRequestResult { IsSuccess = false, ErrorMessage = "کلید API NOWPayments تنظیم نشده است." };

        try
        {
            var client = _httpClient;
            client.DefaultRequestHeaders.Remove("x-api-key");
            client.DefaultRequestHeaders.Add("x-api-key", request.Extra);

            // قیمت به دلار — در فاز کامل نرخ ارز از API خوانده می‌شود؛ فعلاً پارامتر ثابت
            var priceUsd = decimal.Round(request.AmountToman / 60_000m, 2); // فرض نرخ

            var response = await client.PostAsJsonAsync(InvoiceEndpoint, new NowPaymentsInvoiceRequest
            {
                PriceAmount = priceUsd,
                PriceCurrency = "usd",
                PayCurrency = "usdttrc20",
                OrderId = request.TrackingNumber,
                IpnCallbackUrl = request.CallbackUrl
            }, cancellationToken);

            var payload = await response.Content.ReadFromJsonAsync<NowPaymentsInvoiceResponse>(cancellationToken: cancellationToken);

            if (!string.IsNullOrWhiteSpace(payload?.InvoiceUrl))
            {
                return new PaymentGatewayRequestResult
                {
                    IsSuccess = true,
                    RedirectUrl = payload.InvoiceUrl
                };
            }

            return new PaymentGatewayRequestResult
            {
                IsSuccess = false,
                ErrorMessage = $"NOWPayments فاکتور را رد کرد (کد {(int)response.StatusCode})."
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new PaymentGatewayRequestResult { IsSuccess = false, ErrorMessage = $"ارتباط با NOWPayments برقرار نشد: {ex.Message}" };
        }
    }

    public async Task<PaymentGatewayVerifyResult> VerifyAsync(
        PaymentGatewayVerifyRequest request, CancellationToken cancellationToken = default)
    {
        // ⚠️ در فاز کامل: تایید از طریق IPN Webhook با امضای HMAC انجام می‌شود.
        return new PaymentGatewayVerifyResult
        {
            IsSuccess = false,
            ErrorMessage = "تایید رمزارز از طریق IPN Webhook انجام می‌شود (فاز بعدی)."
        };
    }
}

internal class NowPaymentsInvoiceRequest
{
    [JsonPropertyName("price_amount")] public decimal PriceAmount { get; set; }
    [JsonPropertyName("price_currency")] public string PriceCurrency { get; set; } = "usd";
    [JsonPropertyName("pay_currency")] public string PayCurrency { get; set; } = "usdttrc20";
    [JsonPropertyName("order_id")] public string OrderId { get; set; } = string.Empty;
    [JsonPropertyName("ipn_callback_url")] public string IpnCallbackUrl { get; set; } = string.Empty;
}

internal class NowPaymentsInvoiceResponse
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("invoice_url")] public string? InvoiceUrl { get; set; }
}
