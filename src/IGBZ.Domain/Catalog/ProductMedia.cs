using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Catalog;

public sealed class ProductMedia
{
    private ProductMedia()
    {
    }

    public ProductMedia(string url, string? altText, int sortOrder)
    {
        Url = Guard.AgainstEmpty(url, nameof(url));
        AltText = altText;
        SortOrder = sortOrder;
    }

    public string Url { get; private set; } = string.Empty;
    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
}
