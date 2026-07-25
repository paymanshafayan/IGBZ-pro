using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Tenancy;

public sealed class Tenant : Entity
{
    private Tenant()
    {
    }

    private Tenant(string storeName, string subdomain, string adminEmail, string adminPhone, DateTimeOffset now)
        : base(now)
    {
        StoreName = Guard.AgainstEmpty(storeName, nameof(storeName));
        Subdomain = NormalizeSubdomain(subdomain);
        AdminEmail = Guard.AgainstEmpty(adminEmail, nameof(adminEmail)).ToLowerInvariant();
        AdminPhone = Guard.AgainstEmpty(adminPhone, nameof(adminPhone));
        Status = TenantStatus.Provisioning;
    }

    public string StoreName { get; private set; } = string.Empty;
    public string Subdomain { get; private set; } = string.Empty;
    public string AdminEmail { get; private set; } = string.Empty;
    public string AdminPhone { get; private set; } = string.Empty;
    public TenantStatus Status { get; private set; }

    public static Tenant Create(string storeName, string subdomain, string adminEmail, string adminPhone, DateTimeOffset now)
        => new(storeName, subdomain, adminEmail, adminPhone, now);

    public void Activate(DateTimeOffset now)
    {
        if (Status is TenantStatus.Deleted)
        {
            throw new DomainException("Deleted tenant cannot be activated.");
        }

        Status = TenantStatus.Active;
        Touch(now);
    }

    public void Suspend(DateTimeOffset now)
    {
        if (Status is TenantStatus.Deleted)
        {
            throw new DomainException("Deleted tenant cannot be suspended.");
        }

        Status = TenantStatus.Suspended;
        Touch(now);
    }

    public static string NormalizeSubdomain(string subdomain)
    {
        var value = Guard.AgainstEmpty(subdomain, nameof(subdomain)).ToLowerInvariant();
        if (value.Length < 3)
        {
            throw new DomainException("Subdomain must contain at least 3 characters.");
        }

        if (value.Any(ch => !char.IsAsciiLetterOrDigit(ch) && ch != '-'))
        {
            throw new DomainException("Subdomain can contain only ASCII letters, digits and dash.");
        }

        if (value.StartsWith('-') || value.EndsWith('-'))
        {
            throw new DomainException("Subdomain cannot start or end with dash.");
        }

        return value;
    }
}
