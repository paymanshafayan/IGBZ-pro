using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Orders;

public sealed class OrderItem
{
    private OrderItem()
    {
    }

    public OrderItem(
        string productId,
        string variantId,
        string productName,
        string variantName,
        string sku,
        int quantity,
        decimal unitPrice,
        string currency)
    {
        ProductId = Guard.AgainstEmpty(productId, nameof(productId));
        VariantId = Guard.AgainstEmpty(variantId, nameof(variantId));
        ProductName = Guard.AgainstEmpty(productName, nameof(productName));
        VariantName = Guard.AgainstEmpty(variantName, nameof(variantName));
        Sku = Guard.AgainstEmpty(sku, nameof(sku));
        Quantity = Guard.AgainstNonPositive(quantity, nameof(quantity));
        UnitPrice = Guard.AgainstNegative(unitPrice, nameof(unitPrice));
        Currency = Guard.AgainstEmpty(currency, nameof(currency)).ToUpperInvariant();
    }

    public string ProductId { get; private set; } = string.Empty;
    public string VariantId { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public string VariantName { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string Currency { get; private set; } = "IRR";
    public decimal LineTotal => UnitPrice * Quantity;
}
