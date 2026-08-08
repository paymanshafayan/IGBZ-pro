namespace IGBZ.Application.Accounting;

using IGBZ.Application.Abstractions;
using IGBZ.Domain.Accounting;
using IGBZ.Domain.Integration;

public class AccountingService : IAccountingService
{
    private const string ModianProviderKey = "modian";

    private readonly ITenantScopedRepository<Invoice> _invoiceRepository;
    private readonly ITenantScopedRepository<IntegrationCredential> _credentialRepository;
    private readonly ITaxProvider _taxProvider;

    public AccountingService(
        ITenantScopedRepository<Invoice> invoiceRepository,
        ITenantScopedRepository<IntegrationCredential> credentialRepository,
        ITaxProvider taxProvider)
    {
        _invoiceRepository = invoiceRepository;
        _credentialRepository = credentialRepository;
        _taxProvider = taxProvider;
    }

    public async Task<Invoice> IssueInvoiceAsync(IssueInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId) || string.IsNullOrWhiteSpace(request.CustomerId))
            throw new ArgumentException("شناسهٔ سفارش و مشتری الزامی است.");

        var total = request.SubTotalToman + request.TaxToman;

        var invoice = new Invoice
        {
            CustomerId = request.CustomerId,
            OrderId = request.OrderId,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..Math.Min(24, $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}".Length)],
            SubTotalToman = request.SubTotalToman,
            TaxToman = request.TaxToman,
            TotalToman = total,
            Status = InvoiceStatus.Draft,
            IssuedOnUtc = DateTime.UtcNow
        };

        // ارسال به سامانهٔ مؤدیان (اگر اعتبارنامه فعال باشد)
        var credential = await _credentialRepository.FirstOrDefaultAsync(
            c => c.ProviderKey == ModianProviderKey && c.IsActive, cancellationToken);

        if (credential != null && !string.IsNullOrWhiteSpace(request.CustomerNationalId))
        {
            var taxResult = await _taxProvider.SubmitInvoiceAsync(new TaxInvoicePayload
            {
                InvoiceNumber = invoice.InvoiceNumber,
                CustomerNationalId = request.CustomerNationalId,
                AmountToman = request.SubTotalToman,
                TaxToman = request.TaxToman,
                IssueDateUtc = invoice.IssuedOnUtc
            }, credential.ApiKeyEncrypted ?? string.Empty, cancellationToken);

            if (taxResult.IsSuccess)
            {
                invoice.Status = InvoiceStatus.ConfirmedByTax;
                invoice.TaxAuthorityReference = taxResult.Reference;
            }
            else
            {
                invoice.Status = InvoiceStatus.Failed;
                invoice.RawTaxResponse = taxResult.ErrorMessage;
            }
        }

        await _invoiceRepository.InsertAsync(invoice, cancellationToken);
        return invoice;
    }

    public async Task<IReadOnlyList<Invoice>> GetInvoicesAsync(string orderId, CancellationToken cancellationToken = default)
    {
        return await _invoiceRepository.FindAsync(i => i.OrderId == orderId, cancellationToken);
    }
}
