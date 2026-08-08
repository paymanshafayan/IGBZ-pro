namespace IGBZ.Api.Controllers.Admin;

using IGBZ.Application.Abstractions;
using IGBZ.Domain.Integration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>مدیریت اعتبارنامهٔ سرویس‌های بیرونی (درگاه‌ها، BNPL، پیامک و…) — فقط مالک تننت.</summary>
[ApiController]
[Route("api/admin/integrations")]
[Authorize(Policy = "TenantOwner")]
public class IntegrationCredentialsController : ControllerBase
{
    private readonly ITenantScopedRepository<IntegrationCredential> _credentialRepository;

    public IntegrationCredentialsController(ITenantScopedRepository<IntegrationCredential> credentialRepository)
    {
        _credentialRepository = credentialRepository;
    }

    /// <summary>لیست اعتبارنامه‌های تننت جاری.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var credentials = await _credentialRepository.FindAsync(_ => true, cancellationToken);

        // کلیدها به‌صورت ماسک‌شده برمی‌گردند (هرگز خام)
        return Ok(new
        {
            success = true,
            credentials = credentials.Select(c => new
            {
                c.Id,
                c.ProviderKey,
                c.IsActive,
                c.IsVerified,
                hasApiKey = !string.IsNullOrWhiteSpace(c.ApiKeyEncrypted),
                c.EndpointOverrideUrl
            })
        });
    }

    /// <summary>ثبت/به‌روزرسانی اعتبارنامه (کلید به‌صورت رمزنگاری‌شده ذخیره می‌شود).</summary>
    [HttpPut("{providerKey}")]
    public async Task<IActionResult> Upsert(string providerKey, [FromBody] UpsertCredentialDto dto)
    {
        if (string.IsNullOrWhiteSpace(providerKey))
            return BadRequest(new { success = false, message = "شناسهٔ Provider الزامی است." });

        var existing = await _credentialRepository.FirstOrDefaultAsync(
            c => c.ProviderKey == providerKey, CancellationToken.None);

        if (existing == null)
        {
            await _credentialRepository.InsertAsync(new IntegrationCredential
            {
                ProviderKey = providerKey,
                ApiKeyEncrypted = dto.ApiKey,
                ApiSecretEncrypted = dto.ApiSecret,
                EndpointOverrideUrl = dto.EndpointOverrideUrl,
                IsActive = dto.IsActive,
                IsVerified = dto.IsVerified,
                CreatedOnUtc = DateTime.UtcNow
            }, CancellationToken.None);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(dto.ApiKey))
                existing.ApiKeyEncrypted = dto.ApiKey;
            if (!string.IsNullOrWhiteSpace(dto.ApiSecret))
                existing.ApiSecretEncrypted = dto.ApiSecret;

            existing.EndpointOverrideUrl = dto.EndpointOverrideUrl;
            existing.IsActive = dto.IsActive;
            existing.IsVerified = dto.IsVerified;
            existing.UpdatedOnUtc = DateTime.UtcNow;

            await _credentialRepository.UpdateAsync(existing, CancellationToken.None);
        }

        return Ok(new { success = true });
    }

    /// <summary>حذف اعتبارنامه.</summary>
    [HttpDelete("{providerKey}")]
    public async Task<IActionResult> Delete(string providerKey)
    {
        var existing = await _credentialRepository.FirstOrDefaultAsync(
            c => c.ProviderKey == providerKey, CancellationToken.None);

        if (existing != null)
            await _credentialRepository.DeleteAsync(existing.Id, CancellationToken.None);

        return Ok(new { success = true });
    }
}

public class UpsertCredentialDto
{
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? EndpointOverrideUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; }
}
