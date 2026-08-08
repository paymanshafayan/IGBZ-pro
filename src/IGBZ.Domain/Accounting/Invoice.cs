namespace IGBZ.Domain.Accounting;

/// <summary>
/// فاکتور رسمی (برای سامانهٔ مؤدیان مالیاتی و حسابداری).
/// فاکتور از روی سفارش پرداخت‌شده صادر می‌شود؛ وضعیت ارسال به مؤدیان پیگیری می‌شود.
/// </summary>
public class Invoice : Entity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;

    public string InvoiceNumber { get; set; } = string.Empty;

    public decimal SubTotalToman { get; set; }
    public decimal TaxToman { get; set; }
    public decimal TotalToman { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    /// <summary>کد حافظهٔ مالیاتی / شناسهٔ فاکتور در سامانهٔ مؤدیان (پس از ارسال موفق).</summary>
    public string? TaxAuthorityReference { get; set; }

    public string? RawTaxResponse { get; set; }
    public DateTime IssuedOnUtc { get; set; } = DateTime.UtcNow;
}

public enum InvoiceStatus
{
    Draft = 0,
    SubmittedToTax = 10,
    ConfirmedByTax = 20,
    Failed = 30
}
