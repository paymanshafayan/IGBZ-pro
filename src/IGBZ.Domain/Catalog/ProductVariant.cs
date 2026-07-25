using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Catalog;

public sealed class ProductVariant
{
    private ProductVariant()
    {
    }

    public ProductVariant(
        string sku,
        string name,
        decimal price,
        string currency,
        IReadOnlyDictionary<string, string>? attributes = null,
        bool isDefault = false)
    {
        Id = Guid.NewGuid().ToString("N");
        Sku = Guard.AgainstEmpty(sku, nameof(sku));
        Name = Guard.AgainstEmpty(name, nameof(name));
        Price = Guard.AgainstNegative(price, nameof(price));
        Currency = Guard.AgainstEmpty(currency, nameof(currency)).ToUpperInvariant();
        Attributes = attributes is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(attributes, StringComparer.OrdinalIgnoreCase);
        IsDefault = isDefault;
        IsActive = true;
    }

    public string Id { get; private set; } = Guid.NewGuid().ToString("N");
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "IRR";
    public Dictionary<string, string> Attributes { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }

    public void SetDefault() => IsDefault = true;
    public void ClearDefault() => IsDefault = false;
    public void Deactivate() => IsActive = false;
}
