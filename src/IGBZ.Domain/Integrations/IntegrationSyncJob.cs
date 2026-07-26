using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Integrations;

public enum IntegrationSyncJobStatus
{
    Queued = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3
}

public sealed class IntegrationSyncJob : TenantScopedEntity
{
    private IntegrationSyncJob()
    {
    }

    private IntegrationSyncJob(string tenantId, string providerConnectionId, string jobType, DateTimeOffset now)
        : base(tenantId, now)
    {
        ProviderConnectionId = Guard.AgainstEmpty(providerConnectionId, nameof(providerConnectionId));
        JobType = Guard.AgainstEmpty(jobType, nameof(jobType));
        Status = IntegrationSyncJobStatus.Queued;
    }

    public string ProviderConnectionId { get; private set; } = string.Empty;
    public string JobType { get; private set; } = string.Empty;
    public IntegrationSyncJobStatus Status { get; private set; }
    public string? Error { get; private set; }

    public static IntegrationSyncJob Queue(string tenantId, string providerConnectionId, string jobType, DateTimeOffset now)
        => new(tenantId, providerConnectionId, jobType, now);

    public void MarkRunning(DateTimeOffset now)
    {
        Status = IntegrationSyncJobStatus.Running;
        Touch(now);
    }

    public void MarkSucceeded(DateTimeOffset now)
    {
        Status = IntegrationSyncJobStatus.Succeeded;
        Error = null;
        Touch(now);
    }

    public void MarkFailed(string error, DateTimeOffset now)
    {
        Status = IntegrationSyncJobStatus.Failed;
        Error = Guard.AgainstEmpty(error, nameof(error));
        Touch(now);
    }
}
