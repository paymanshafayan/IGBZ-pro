using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Integrations;

public sealed class IntegrationProviderConnection : TenantScopedEntity
{
    private IntegrationProviderConnection()
    {
    }

    private IntegrationProviderConnection(string tenantId, string providerKey, string displayName, IntegrationProviderType type, DateTimeOffset now)
        : base(tenantId, now)
    {
        ProviderKey = Guard.AgainstEmpty(providerKey, nameof(providerKey)).ToLowerInvariant();
        DisplayName = Guard.AgainstEmpty(displayName, nameof(displayName));
        Type = type;
        Status = IntegrationConnectionStatus.Disconnected;
    }

    public string ProviderKey { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public IntegrationProviderType Type { get; private set; }
    public IntegrationConnectionStatus Status { get; private set; }
    public Dictionary<string, string> Settings { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? LastError { get; private set; }

    public static IntegrationProviderConnection Create(string tenantId, string providerKey, string displayName, IntegrationProviderType type, DateTimeOffset now)
        => new(tenantId, providerKey, displayName, type, now);

    public void SetSecretPlaceholder(string key, DateTimeOffset now)
    {
        Settings[Guard.AgainstEmpty(key, nameof(key))] = "***";
        Touch(now);
    }

    public void MarkConnected(DateTimeOffset now)
    {
        Status = IntegrationConnectionStatus.Connected;
        LastError = null;
        Touch(now);
    }

    public void MarkError(string error, DateTimeOffset now)
    {
        Status = IntegrationConnectionStatus.Error;
        LastError = Guard.AgainstEmpty(error, nameof(error));
        Touch(now);
    }
}
