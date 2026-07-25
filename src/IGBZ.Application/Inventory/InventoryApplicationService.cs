using IGBZ.Application.Abstractions;
using IGBZ.Domain.Inventory;
using IGBZ.Domain.Shared;

namespace IGBZ.Application.Inventory;

public sealed class InventoryApplicationService(
    ITenantScopedRepository<InventoryItem> inventory,
    IClock clock)
{
    public async Task<IReadOnlyList<InventoryItemResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = await inventory.ListAsync(null, cancellationToken);
        return result.OrderBy(item => item.ProductId).ThenBy(item => item.VariantId).Select(ToResponse).ToList();
    }

    public async Task<InventoryItemResponse> AdjustAsync(string id, AdjustInventoryRequest request, CancellationToken cancellationToken = default)
    {
        var item = await inventory.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Inventory item was not found.");

        item.AdjustOnHand(request.OnHand, clock.UtcNow);
        await inventory.ReplaceAsync(item, cancellationToken);
        return ToResponse(item);
    }

    private static InventoryItemResponse ToResponse(InventoryItem item)
        => new(item.Id, item.ProductId, item.VariantId, item.OnHand, item.Reserved, item.Available);
}
