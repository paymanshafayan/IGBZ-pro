using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Tenancy;

public enum DomainMappingType
{
    PlatformSubdomain = 0,
    CustomDomain = 1
}

public enum DomainVerificationStatus
{
    Pending = 0,
    Verified = 1,
    Failed = 2
}

public sealed class StoreDomainMapping : Entity
{
    private StoreDomainMapping()
    {
    }

    private StoreDomainMapping(string tenantId, string host, DomainMappingType type, DateTimeOffset now)
        : base(now)
    {
        TenantId = Guard.AgainstEmpty(tenantId, nameof(tenantId));
        Host = NormalizeHost(host);
        Type = type;
        VerificationStatus = type == DomainMappingType.PlatformSubdomain
            ? DomainVerificationStatus.Verified
            : DomainVerificationStatus.Pending;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string Host { get; private set; } = string.Empty;
    public DomainMappingType Type { get; private set; }
    public DomainVerificationStatus VerificationStatus { get; private set; }

    public static StoreDomainMapping CreatePlatformSubdomain(string tenantId, string subdomain, string rootDomain, DateTimeOffset now)
        => new(tenantId, $"{Tenant.NormalizeSubdomain(subdomain)}.{NormalizeHost(rootDomain)}", DomainMappingType.PlatformSubdomain, now);

    public static StoreDomainMapping CreateCustomDomain(string tenantId, string host, DateTimeOffset now)
        => new(tenantId, host, DomainMappingType.CustomDomain, now);

    public void MarkVerified(DateTimeOffset now)
    {
        VerificationStatus = DomainVerificationStatus.Verified;
        Touch(now);
    }

    public static string NormalizeHost(string host)
    {
        var normalized = Guard.AgainstEmpty(host, nameof(host)).Trim().TrimEnd('.').ToLowerInvariant();
        if (normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = new Uri(normalized).Host;
        }

        return normalized;
    }
}
