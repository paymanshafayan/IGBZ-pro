namespace IGBZ.Application.Abstractions;

public enum TenantResolutionSource
{
    Platform = 0,
    Header = 1,
    Claim = 2,
    Host = 3,
    System = 4
}

public sealed record TenantContext(string TenantId, TenantResolutionSource Source)
{
    public const string PlatformTenantId = "platform";
    public bool IsPlatform => string.Equals(TenantId, PlatformTenantId, StringComparison.OrdinalIgnoreCase);
}
