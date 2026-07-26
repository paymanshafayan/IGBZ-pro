using IGBZ.Domain.Integrations;
using IGBZ.Infrastructure.MongoDb;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IGBZ.Infrastructure.Integrations;

public sealed class MongoIntegrationJobWorker(
    MongoDbContext dbContext,
    ILogger<MongoIntegrationJobWorker> logger) : BackgroundService
{
    private readonly IMongoCollection<IntegrationSyncJob> _jobs = dbContext.Collection<IntegrationSyncJob>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await TakeNextJobAsync(stoppingToken);
                if (job is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    continue;
                }

                logger.LogInformation("Processing integration job {JobId} for tenant {TenantId}", job.Id, job.TenantId);
                await CompleteJobAsync(job.Id, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown.
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Integration job worker failed during polling cycle.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task<IntegrationSyncJob?> TakeNextJobAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var filter = Builders<IntegrationSyncJob>.Filter.Eq(job => job.Status, IntegrationSyncJobStatus.Queued);
        var update = Builders<IntegrationSyncJob>.Update
            .Set(job => job.Status, IntegrationSyncJobStatus.Running)
            .Set(job => job.UpdatedAtUtc, now);

        return await _jobs.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<IntegrationSyncJob>
            {
                Sort = Builders<IntegrationSyncJob>.Sort.Ascending(job => job.CreatedAtUtc),
                ReturnDocument = ReturnDocument.After
            },
            cancellationToken);
    }

    private async Task CompleteJobAsync(string jobId, CancellationToken cancellationToken)
    {
        // Initial no-op provider runner. Real providers will replace this with type-specific Sync/Test/Webhook handlers.
        var now = DateTimeOffset.UtcNow;
        var update = Builders<IntegrationSyncJob>.Update
            .Set(job => job.Status, IntegrationSyncJobStatus.Succeeded)
            .Set(job => job.Error, null)
            .Set(job => job.UpdatedAtUtc, now);

        await _jobs.UpdateOneAsync(job => job.Id == jobId, update, cancellationToken: cancellationToken);
    }
}
