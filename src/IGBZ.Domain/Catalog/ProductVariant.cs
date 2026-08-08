namespace IGBZ.Domain.Catalog;

public class ProductVariant
{
    public string Sku { get; set; } = string.Empty;

    public Dictionary<string, string> Attributes { get; set; } = new();

    public decimal PriceToman { get; set; }

    public decimal? OldPriceToman { get; set; }

    public int StockQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    /// <summary>موجودی قابل‌فروش = کل منهای رزروشده.</summary>
    public int AvailableQuantity => StockQuantity - ReservedQuantity;
}
