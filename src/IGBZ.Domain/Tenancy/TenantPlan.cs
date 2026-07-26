using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Tenancy;

public sealed class TenantPlan : Entity
{
    private TenantPlan()
    {
    }

    private TenantPlan(string code, string title, decimal monthlyPrice, string currency, DateTimeOffset now)
        : base(now)
    {
        Code = Guard.AgainstEmpty(code, nameof(code)).ToLowerInvariant();
        Title = Guard.AgainstEmpty(title, nameof(title));
        MonthlyPrice = Guard.AgainstNegative(monthlyPrice, nameof(monthlyPrice));
        Currency = Guard.AgainstEmpty(currency, nameof(currency)).ToUpperInvariant();
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public decimal MonthlyPrice { get; private set; }
    public string Currency { get; private set; } = "IRR";
    public bool IsActive { get; private set; }
    public IReadOnlyDictionary<string, string> Features => _features;

    private readonly Dictionary<string, string> _features = new(StringComparer.OrdinalIgnoreCase);

    public static TenantPlan Create(string code, string title, decimal monthlyPrice, string currency, DateTimeOffset now)
        => new(code, title, monthlyPrice, currency, now);

    public void AddFeature(string key, string value, DateTimeOffset now)
    {
        _features[Guard.AgainstEmpty(key, nameof(key))] = Guard.AgainstEmpty(value, nameof(value));
        Touch(now);
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        Touch(now);
    }
}
