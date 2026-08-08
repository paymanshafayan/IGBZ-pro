namespace IGBZ.Api.Controllers.Admin;

using IGBZ.Application.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>حسابداری/فاکتور رسمی — فقط مالک تننت.</summary>
[ApiController]
[Route("api/admin/accounting")]
[Authorize(Policy = "TenantOwner")]
public class AccountingController : ControllerBase
{
    private readonly IAccountingService _accountingService;

    public AccountingController(IAccountingService accountingService)
    {
        _accountingService = accountingService;
    }

    /// <summary>صدور فاکتور رسمی برای یک سفارش + ارسال به مؤدیان (در صورت تنظیم).</summary>
    [HttpPost("issue-invoice")]
    public async Task<IActionResult> IssueInvoice([FromBody] IssueInvoiceDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _accountingService.IssueInvoiceAsync(new IssueInvoiceRequest
            {
                OrderId = dto.OrderId,
                CustomerId = dto.CustomerId,
                CustomerNationalId = dto.CustomerNationalId,
                SubTotalToman = dto.SubTotalToman,
                TaxToman = dto.TaxToman
            }, cancellationToken);

            return Ok(new
            {
                success = true,
                invoiceNumber = invoice.InvoiceNumber,
                status = invoice.Status.ToString(),
                taxAuthorityReference = invoice.TaxAuthorityReference,
                totalToman = invoice.TotalToman
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>فاکتورهای یک سفارش.</summary>
    [HttpGet("invoices/{orderId}")]
    public async Task<IActionResult> GetInvoices(string orderId, CancellationToken cancellationToken)
    {
        var invoices = await _accountingService.GetInvoicesAsync(orderId, cancellationToken);
        return Ok(new { success = true, invoices });
    }
}

public class IssueInvoiceDto
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerNationalId { get; set; }
    public decimal SubTotalToman { get; set; }
    public decimal TaxToman { get; set; }
}
