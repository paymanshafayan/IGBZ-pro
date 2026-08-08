namespace IGBZ.Application.Tests;

using IGBZ.Application.Accounting;
using IGBZ.Application.Tenancy;
using IGBZ.Application.Tests.Fakes;
using IGBZ.Domain.Accounting;
using Xunit;

public class AccountingServiceTests
{
    private readonly TenantContext _tenantContext = new();

    private sealed class FakeTaxProvider : ITaxProvider
    {
        public bool ShouldFail { get; set; }
        public string? LastApiKey { get; private set; }
        public int CallCount { get; private set; }

        public Task<TaxSubmissionResult> SubmitInvoiceAsync(
            TaxInvoicePayload payload, string apiKey, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastApiKey = apiKey;
            if (ShouldFail)
                return Task.FromResult(new TaxSubmissionResult { IsSuccess = false, ErrorMessage = "مؤدیان در دسترس نیست." });
            return Task.FromResult(new TaxSubmissionResult { IsSuccess = true, Reference = $"MEM-{payload.InvoiceNumber}" });
        }
    }

    private (AccountingService service, FakeTenantScopedRepository<Invoice> invoices, FakeCredentialRepository credentials, FakeTaxProvider tax) Build()
    {
        _tenantContext.Set("t1");
        var invoices = new FakeTenantScopedRepository<Invoice>(_tenantContext);
        var credentials = new FakeCredentialRepository(_tenantContext);
        var tax = new FakeTaxProvider();
        return (new AccountingService(invoices, credentials, FakeEncryptionService.Instance, tax), invoices, credentials, tax);
    }

    [Fact]
    public async Task IssueInvoice_WithoutTaxCredential_CreatesDraft()
    {
        var (service, invoices, _, tax) = Build();

        var invoice = await service.IssueInvoiceAsync(new IssueInvoiceRequest
        {
            OrderId = "o1",
            CustomerId = "c1",
            SubTotalToman = 100_000,
            TaxToman = 9_000
        });

        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        Assert.Equal(109_000m, invoice.TotalToman);
        Assert.Single(invoices.Store);
        Assert.Equal(0, tax.CallCount); // بدون اعتبارنامه، به مؤدیان ارسال نشد
    }

    [Fact]
    public async Task IssueInvoice_WithTaxCredential_SubmitsAndConfirms()
    {
        var (service, invoices, credentials, tax) = Build();
        credentials.AddActiveCredential("modian", "tax-key");

        var invoice = await service.IssueInvoiceAsync(new IssueInvoiceRequest
        {
            OrderId = "o1",
            CustomerId = "c1",
            CustomerNationalId = "1234567890",
            SubTotalToman = 100_000,
            TaxToman = 9_000
        });

        Assert.Equal(InvoiceStatus.ConfirmedByTax, invoice.Status);
        Assert.NotNull(invoice.TaxAuthorityReference);
        Assert.Equal(1, tax.CallCount);
        Assert.Equal("tax-key", tax.LastApiKey);
    }

    [Fact]
    public async Task IssueInvoice_TaxFails_MarksFailed()
    {
        var (service, _, credentials, tax) = Build();
        credentials.AddActiveCredential("modian", "tax-key");
        tax.ShouldFail = true;

        var invoice = await service.IssueInvoiceAsync(new IssueInvoiceRequest
        {
            OrderId = "o1",
            CustomerId = "c1",
            CustomerNationalId = "1234567890",
            SubTotalToman = 100_000,
            TaxToman = 9_000
        });

        Assert.Equal(InvoiceStatus.Failed, invoice.Status);
        Assert.NotNull(invoice.RawTaxResponse);
    }

    [Fact]
    public async Task GetInvoices_FiltersByOrder()
    {
        var (service, invoices, _, _) = Build();
        invoices.Store.Add(new Invoice { Id = "i1", OrderId = "o1", TenantId = "t1", InvoiceNumber = "INV-1" });
        invoices.Store.Add(new Invoice { Id = "i2", OrderId = "o2", TenantId = "t1", InvoiceNumber = "INV-2" });

        var result = await service.GetInvoicesAsync("o1");

        Assert.Single(result);
        Assert.Equal("INV-1", result[0].InvoiceNumber);
    }
}
