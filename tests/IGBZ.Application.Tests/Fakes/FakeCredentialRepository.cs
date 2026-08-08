namespace IGBZ.Application.Tests.Fakes;

using IGBZ.Domain.Integration;

/// <summary>اعتبارنامه‌های در-حافظه برای تست پرداخت.</summary>
public class FakeCredentialRepository : FakeTenantScopedRepository<IntegrationCredential>
{
    public FakeCredentialRepository(IGBZ.Application.Tenancy.ITenantContext tenantContext)
        : base(tenantContext)
    {
    }

    /// <summary>افزودن کلید API فعال برای یک درگاه (با رمزگذاری سادهٔ mock).</summary>
    public void AddActiveCredential(string providerKey, string apiKey)
    {
        Store.Add(new IntegrationCredential
        {
            TenantId = "t1",
            ProviderKey = providerKey,
            ApiKeyEncrypted = apiKey, // در تست رمزگشایی لازم نیست
            IsActive = true,
            IsVerified = true
        });
    }
}
