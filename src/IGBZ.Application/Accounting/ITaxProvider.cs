namespace IGBZ.Application.Accounting;

/// <summary>فاکتور برای ارسال به سامانهٔ مالیاتی.</summary>
public class TaxInvoicePayload
{
    public string InvoiceNumber { get; init; } = string.Empty;
    public string CustomerNationalId { get; init; } = string.Empty;
    public decimal AmountToman { get; init; }
    public decimal TaxToman { get; init; }
    public DateTime IssueDateUtc { get; init; }
}

public class TaxSubmissionResult
{
    public bool IsSuccess { get; init; }
    public string? Reference { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// ارائه‌دهندهٔ سامانهٔ مؤدیان مالیاتی (فاز ۱۱).
/// قاعدهٔ سخت: بدون فراخوانی واقعی HTTP، موفق اعلام نمی‌شود.
/// </summary>
public interface ITaxProvider
{
    Task<TaxSubmissionResult> SubmitInvoiceAsync(TaxInvoicePayload payload, string apiKey, CancellationToken cancellationToken = default);
}
