namespace IGBZ.Application.Inventory;

public sealed record InventoryItemResponse(
    string Id,
    string ProductId,
    string VariantId,
    int OnHand,
    int Reserved,
    int Available);

public sealed record AdjustInventoryRequest(int OnHand);
