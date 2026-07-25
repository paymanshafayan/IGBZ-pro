using IGBZ.Application.Abstractions;
using IGBZ.Domain.Integrations;
using IGBZ.Domain.Shared;

namespace IGBZ.Application.Integrations;

public sealed class IntegrationService(
    ITenantContextAccessor tenantContextAccessor,
    ITenantScopedRepository<IntegrationProviderConnection> connections,
    ITenantScopedRepository<IntegrationSyncJob> jobs,
    IClock clock)
{
    public async Task<IntegrationConnectionResponse> CreateConnectionAsync(CreateIntegrationConnectionRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.RequiredTenantId;
        var existing = await connections.FirstOrDefaultAsync(connection => connection.ProviderKey == request.ProviderKey.ToLowerInvariant(), cancellationToken);
        if (existing is not null)
        {
            throw new DomainException("Integration provider connection already exists for this tenant.");
        }

        var now = clock.UtcNow;
        var connection = IntegrationProviderConnection.Create(tenantId, request.ProviderKey, request.DisplayName, request.Type, now);
        if (request.Settings is not null)
        {
            foreach (var key in request.Settings.Keys)
            {
                connection.SetSecretPlaceholder(key, now);
            }
        }

        connection.MarkConnected(now);
        await connections.AddAsync(connection, cancellationToken);
        return ToConnectionResponse(connection);
    }

    public async Task<IReadOnlyList<IntegrationConnectionResponse>> ListConnectionsAsync(CancellationToken cancellationToken = default)
    {
        var result = await connections.ListAsync(null, cancellationToken);
        return result.OrderBy(connection => connection.Type).ThenBy(connection => connection.DisplayName).Select(ToConnectionResponse).ToList();
    }

    public async Task<IntegrationSyncJobResponse> QueueSyncAsync(string connectionId, QueueIntegrationSyncRequest request, CancellationToken cancellationToken = default)
    {
        var connection = await connections.GetByIdAsync(connectionId, cancellationToken)
            ?? throw new DomainException("Integration connection was not found.");

        var job = IntegrationSyncJob.Queue(tenantContextAccessor.RequiredTenantId, connection.Id, request.JobType, clock.UtcNow);
        await jobs.AddAsync(job, cancellationToken);
        return ToJobResponse(job);
    }

    public async Task<IReadOnlyList<IntegrationSyncJobResponse>> ListJobsAsync(CancellationToken cancellationToken = default)
    {
        var result = await jobs.ListAsync(null, cancellationToken);
        return result.OrderByDescending(job => job.CreatedAtUtc).Select(ToJobResponse).ToList();
    }

    public static IntegrationConnectionResponse ToConnectionResponse(IntegrationProviderConnection connection)
        => new(
            connection.Id,
            connection.ProviderKey,
            connection.DisplayName,
            connection.Type,
            connection.Status,
            connection.Settings,
            connection.LastError);

    public static IntegrationSyncJobResponse ToJobResponse(IntegrationSyncJob job)
        => new(job.Id, job.ProviderConnectionId, job.JobType, job.Status, job.Error, job.CreatedAtUtc, job.UpdatedAtUtc);
}
