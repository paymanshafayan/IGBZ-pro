using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Inventory;

public sealed class InventoryItem : TenantScopedEntity
{
    private InventoryItem()
    {
    }

    private InventoryItem(string tenantId, string productId, string variantId, int onHand, DateTimeOffset now)
        : base(tenantId, now)
    {
        ProductId = Guard.AgainstEmpty(productId, nameof(productId));
        VariantId = Guard.AgainstEmpty(variantId, nameof(variantId));
        OnHand = onHand < 0 ? throw new DomainException("Initial stock cannot be negative.") : onHand;
        Reserved = 0;
    }

    public string ProductId { get; private set; } = string.Empty;
    public string VariantId { get; private set; } = string.Empty;
    public int OnHand { get; private set; }
    public int Reserved { get; private set; }
    public int Available => OnHand - Reserved;

    public static InventoryItem Create(string tenantId, string productId, string variantId, int initialStock, DateTimeOffset now)
        => new(tenantId, productId, variantId, initialStock, now);

    public void AdjustOnHand(int newOnHand, DateTimeOffset now)
    {
        if (newOnHand < Reserved)
        {
            throw new DomainException("On-hand stock cannot be lower than reserved stock.");
        }

        OnHand = newOnHand;
        Touch(now);
    }
}
