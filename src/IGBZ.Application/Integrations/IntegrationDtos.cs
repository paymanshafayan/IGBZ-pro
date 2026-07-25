using IGBZ.Domain.Integrations;

namespace IGBZ.Application.Integrations;

public sealed record CreateIntegrationConnectionRequest(
    string ProviderKey,
    string DisplayName,
    IntegrationProviderType Type,
    Dictionary<string, string>? Settings);

public sealed record IntegrationConnectionResponse(
    string Id,
    string ProviderKey,
    string DisplayName,
    IntegrationProviderType Type,
    IntegrationConnectionStatus Status,
    IReadOnlyDictionary<string, string> Settings,
    string? LastError);

public sealed record QueueIntegrationSyncRequest(string JobType);

public sealed record IntegrationSyncJobResponse(
    string Id,
    string ProviderConnectionId,
    string JobType,
    IntegrationSyncJobStatus Status,
    string? Error,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
