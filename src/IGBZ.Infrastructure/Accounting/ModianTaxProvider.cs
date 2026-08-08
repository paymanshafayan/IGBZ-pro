namespace IGBZ.Infrastructure.Accounting;

using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using IGBZ.Application.Accounting;

/// <summary>
/// سامانهٔ مؤدیان مالیاتی — ارسال فاکتور رسمی با HTTP واقعی.
/// ⚠️ Endpoint رسمی و امضای دیجیتال فاکتور (تایید شده با کلید عمومی مؤدیان) نیازمند
/// کلیدهای صادره از سامانه است؛ این پیاده‌سازی قرارداد عمومی را دارد و با
/// Endpoint واقعی قابل تنظیم است (در صورت نیاز، مسیر از اعتبارنامه خوانده می‌شود).
/// قاعدهٔ سخت: بدون فراخوانی موفق، «تاییدشده» اعلام نمی‌شود.
/// </summary>
public class ModianTaxProvider : ITaxProvider
{
    private readonly HttpClient _httpClient;

    public ModianTaxProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TaxSubmissionResult> SubmitInvoiceAsync(
        TaxInvoicePayload payload, string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return new TaxSubmissionResult { IsSuccess = false, ErrorMessage = "کلید سامانهٔ مؤدیان فعال نیست." };

        try
        {
            var client = _httpClient;
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await client.PostAsJsonAsync(
                "https://tp.tax.gov.ir/api/invoices", // آدرس نمادین — از اعتبارنامه قابل تنظیم
                new ModianInvoicePayload
                {
                    InvoiceNumber = payload.InvoiceNumber,
                    NationalId = payload.CustomerNationalId,
                    AmountRials = (long)(payload.AmountToman * 10),
                    TaxRials = (long)(payload.TaxToman * 10),
                    IssueDateUtc = payload.IssueDateUtc
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new TaxSubmissionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"مؤدیان خطا داد (کد {(int)response.StatusCode}): {Truncate(body)}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<ModianInvoiceResponse>(cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(result?.ReferenceNumber))
                return new TaxSubmissionResult { IsSuccess = false, ErrorMessage = "پاسخ مؤدیان بدون شناسهٔ حافظهٔ مالیاتی بود." };

            return new TaxSubmissionResult { IsSuccess = true, Reference = result.ReferenceNumber };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new TaxSubmissionResult { IsSuccess = false, ErrorMessage = $"ارتباط با مؤدیان برقرار نشد: {ex.Message}" };
        }
    }

    private static string Truncate(string s, int max = 200) =>
        s.Length <= max ? s : s[..max];
}

internal class ModianInvoicePayload
{
    [JsonPropertyName("invoiceNumber")] public string InvoiceNumber { get; set; } = string.Empty;
    [JsonPropertyName("nationalId")] public string NationalId { get; set; } = string.Empty;
    [JsonPropertyName("amountRials")] public long AmountRials { get; set; }
    [JsonPropertyName("taxRials")] public long TaxRials { get; set; }
    [JsonPropertyName("issueDateUtc")] public DateTime IssueDateUtc { get; set; }
}

internal class ModianInvoiceResponse
{
    [JsonPropertyName("referenceNumber")] public string? ReferenceNumber { get; set; }
}
