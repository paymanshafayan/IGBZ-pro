namespace IGBZ.Application.Accounting;

using IGBZ.Domain.Accounting;

public class IssueInvoiceRequest
{
    public string OrderId { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public string? CustomerNationalId { get; init; }
    public decimal SubTotalToman { get; init; }
    public decimal TaxToman { get; init; }
}

/// <summary>
/// حسابداری (فاز ۱۱): صدور فاکتور رسمی از روی سفارش + ارسال به سامانهٔ مؤدیان.
/// </summary>
public interface IAccountingService
{
    Task<Invoice> IssueInvoiceAsync(IssueInvoiceRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Invoice>> GetInvoicesAsync(string orderId, CancellationToken cancellationToken = default);
}
