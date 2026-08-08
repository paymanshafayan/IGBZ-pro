namespace IGBZ.Infrastructure.Repositories;

using IGBZ.Application.Abstractions;
using IGBZ.Application.Tenancy;
using IGBZ.Domain.Catalog;
using IGBZ.Infrastructure.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;

/// <summary>
/// رزرو اتمیک موجودی با <c>findOneAndUpdate</c>-مانند (UpdateOne با فیلتر شرطی):
/// فقط وقتی موجودی کافی است (stock - reserved &gt;= qty) رزرو انجام می‌شود (سند بخش ۷.۳).
/// </summary>
public class ProductInventoryRepository : TenantScopedRepository<Product>, IProductInventoryRepository
{
    public ProductInventoryRepository(MongoDbContext dbContext, ITenantContext tenantContext)
        : base(dbContext, tenantContext)
    {
    }

    public async Task<bool> TryReserveAsync(string productId, string sku, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "تعداد رزرو باید مثبت باشد.");

        var tenantId = RequireTenant();

        // فیلتر: محصول همین تننت + واریانت با sku + موجودی کافی (با $expr داخل $elemMatch)
        var expr = new BsonDocument("$expr",
            new BsonDocument("$gte", new BsonArray
            {
                new BsonDocument("$subtract", new BsonArray { "$stockQuantity", "$reservedQuantity" }),
                quantity
            }));

        var filter = Builders<Product>.Filter.And(
            Builders<Product>.Filter.Eq(p => p.Id, productId),
            TenantFilter(tenantId),
            Builders<Product>.Filter.ElemMatch(p => p.Variants,
                new BsonDocumentFilterDefinition<ProductVariant>(new BsonDocument
                {
                    { "sku", sku },
                    { "$expr", expr }
                })));

        var update = Builders<Product>.Update.Inc("variants.$.reservedQuantity", quantity);

        var result = await Collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task ReleaseReservationAsync(string productId, string sku, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "تعداد آزادسازی باید مثبت باشد.");

        var tenantId = RequireTenant();

        var filter = Builders<Product>.Filter.And(
            Builders<Product>.Filter.Eq(p => p.Id, productId),
            TenantFilter(tenantId),
            Builders<Product>.Filter.ElemMatch(p => p.Variants, Builders<ProductVariant>.Filter.Eq(v => v.Sku, sku)));

        var update = Builders<Product>.Update.Inc("variants.$.reservedQuantity", -quantity);

        await Collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }
}
