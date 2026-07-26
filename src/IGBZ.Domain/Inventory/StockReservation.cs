using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Inventory;

public enum StockReservationStatus
{
    Active = 0,
    Committed = 1,
    Released = 2,
    Expired = 3
}

public sealed class StockReservation : TenantScopedEntity
{
    private StockReservation()
    {
    }

    private StockReservation(string tenantId, string productId, string variantId, int quantity, string reason, DateTimeOffset expiresAtUtc, DateTimeOffset now)
        : base(tenantId, now)
    {
        ProductId = Guard.AgainstEmpty(productId, nameof(productId));
        VariantId = Guard.AgainstEmpty(variantId, nameof(variantId));
        Quantity = Guard.AgainstNonPositive(quantity, nameof(quantity));
        Reason = Guard.AgainstEmpty(reason, nameof(reason));
        ExpiresAtUtc = expiresAtUtc;
        Status = StockReservationStatus.Active;
    }

    public string ProductId { get; private set; } = string.Empty;
    public string VariantId { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public StockReservationStatus Status { get; private set; }

    public static StockReservation Create(string tenantId, string productId, string variantId, int quantity, string reason, DateTimeOffset expiresAtUtc, DateTimeOffset now)
        => new(tenantId, productId, variantId, quantity, reason, expiresAtUtc, now);

    public void MarkCommitted(DateTimeOffset now)
    {
        Status = StockReservationStatus.Committed;
        Touch(now);
    }

    public void MarkReleased(DateTimeOffset now)
    {
        Status = StockReservationStatus.Released;
        Touch(now);
    }
}
