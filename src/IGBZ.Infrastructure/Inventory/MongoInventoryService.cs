using IGBZ.Application.Abstractions;
using IGBZ.Domain.Inventory;
using IGBZ.Domain.Shared;
using IGBZ.Infrastructure.MongoDb;
using MongoDB.Driver;

namespace IGBZ.Infrastructure.Inventory;

public sealed class MongoInventoryService(
    MongoDbContext dbContext,
    ITenantContextAccessor tenantContextAccessor,
    ITenantScopedRepository<StockReservation> reservations,
    IClock clock) : IInventoryService
{
    private readonly IMongoCollection<InventoryItem> _inventory = dbContext.Collection<InventoryItem>();

    public async Task EnsureInventoryItemAsync(string productId, string variantId, int initialStock, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.RequiredTenantId;
        var filter = VariantFilter(tenantId, productId, variantId);
        var existing = await _inventory.Find(filter).FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return;
        }

        try
        {
            await _inventory.InsertOneAsync(
                InventoryItem.Create(tenantId, productId, variantId, initialStock, clock.UtcNow),
                cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Another concurrent request created the inventory row. The unique index protects us.
        }
    }

    public async Task<StockReservationResult> TryReserveAsync(
        string productId,
        string variantId,
        int quantity,
        string reason,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNonPositive(quantity, nameof(quantity));
        var tenantId = tenantContextAccessor.RequiredTenantId;
        var now = clock.UtcNow;

        var filter = Builders<InventoryItem>.Filter.Where(item =>
            item.TenantId == tenantId &&
            item.ProductId == productId &&
            item.VariantId == variantId &&
            item.OnHand - item.Reserved >= quantity);

        var update = Builders<InventoryItem>.Update
            .Inc(item => item.Reserved, quantity)
            .Set(item => item.UpdatedAtUtc, now);

        var updated = await _inventory.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<InventoryItem> { ReturnDocument = ReturnDocument.After },
            cancellationToken);

        if (updated is null)
        {
            return StockReservationResult.Failure("Insufficient stock for the requested variant.");
        }

        var reservation = StockReservation.Create(
            tenantId,
            productId,
            variantId,
            quantity,
            reason,
            now.Add(ttl),
            now);

        try
        {
            await reservations.AddAsync(reservation, cancellationToken);
            return StockReservationResult.Success(reservation.Id);
        }
        catch
        {
            await ReleaseReservedQuantityAsync(tenantId, productId, variantId, quantity, cancellationToken);
            throw;
        }
    }

    public async Task CommitReservationAsync(string reservationId, CancellationToken cancellationToken = default)
    {
        var reservation = await reservations.GetByIdAsync(reservationId, cancellationToken)
            ?? throw new DomainException("Stock reservation was not found.");

        if (reservation.Status == StockReservationStatus.Committed)
        {
            return;
        }

        if (reservation.Status != StockReservationStatus.Active)
        {
            throw new DomainException("Only active stock reservations can be committed.");
        }

        var tenantId = tenantContextAccessor.RequiredTenantId;
        var filter = VariantFilter(tenantId, reservation.ProductId, reservation.VariantId) &
                     Builders<InventoryItem>.Filter.Gte(item => item.Reserved, reservation.Quantity) &
                     Builders<InventoryItem>.Filter.Gte(item => item.OnHand, reservation.Quantity);

        var update = Builders<InventoryItem>.Update
            .Inc(item => item.Reserved, -reservation.Quantity)
            .Inc(item => item.OnHand, -reservation.Quantity)
            .Set(item => item.UpdatedAtUtc, clock.UtcNow);

        var result = await _inventory.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        if (result.ModifiedCount == 0)
        {
            throw new DomainException("Could not commit stock reservation.");
        }

        reservation.MarkCommitted(clock.UtcNow);
        await reservations.ReplaceAsync(reservation, cancellationToken);
    }

    public async Task ReleaseReservationAsync(string reservationId, CancellationToken cancellationToken = default)
    {
        var reservation = await reservations.GetByIdAsync(reservationId, cancellationToken);
        if (reservation is null || reservation.Status is StockReservationStatus.Released or StockReservationStatus.Committed)
        {
            return;
        }

        var tenantId = tenantContextAccessor.RequiredTenantId;
        await ReleaseReservedQuantityAsync(tenantId, reservation.ProductId, reservation.VariantId, reservation.Quantity, cancellationToken);
        reservation.MarkReleased(clock.UtcNow);
        await reservations.ReplaceAsync(reservation, cancellationToken);
    }

    private async Task ReleaseReservedQuantityAsync(string tenantId, string productId, string variantId, int quantity, CancellationToken cancellationToken)
    {
        var filter = VariantFilter(tenantId, productId, variantId) &
                     Builders<InventoryItem>.Filter.Gte(item => item.Reserved, quantity);
        var update = Builders<InventoryItem>.Update
            .Inc(item => item.Reserved, -quantity)
            .Set(item => item.UpdatedAtUtc, clock.UtcNow);

        await _inventory.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    private static FilterDefinition<InventoryItem> VariantFilter(string tenantId, string productId, string variantId)
        => Builders<InventoryItem>.Filter.Eq(item => item.TenantId, tenantId) &
           Builders<InventoryItem>.Filter.Eq(item => item.ProductId, productId) &
           Builders<InventoryItem>.Filter.Eq(item => item.VariantId, variantId);
}
