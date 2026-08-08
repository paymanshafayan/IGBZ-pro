namespace IGBZ.Application.Logistics;

/// <summary>دستهٔ مسیر ارسال — محاسبهٔ محلی بر اساس وزن/شهر (منطق قطعی، نه جعلی).</summary>
public class RouteCategoryResult
{
    public string RouteType { get; init; } = string.Empty;
    public string CarrierName { get; init; } = string.Empty;
    public decimal EstimatedCostToman { get; init; }
    public bool DeliveryPinRequired { get; init; }
}

/// <summary>ثبت مرسوله در سامانهٔ لجستیک (تاپین/پستکس) — با HTTP واقعی.</summary>
public class ShipmentRegistrationResult
{
    public bool IsSuccess { get; init; }
    public string? TrackingCode { get; init; }
    public string? DeliveryPin { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// لجستیک (سند بخش ۱۰.۴): دسته‌بندی خودکار مسیر، PIN تحویل امن (RandomNumberGenerator)،
/// ثبت مرسوله در تاپین. قاعدهٔ سخت: بدون فراخوانی واقعی، موفق اعلام نمی‌شود.
/// </summary>
public interface ILogisticsService
{
    /// <summary>دسته‌بندی مسیر بر اساس وزن و شهر مقصد.</summary>
    RouteCategoryResult CategorizeRoute(decimal weightKg, string destinationCity, bool isExpressNeeded);

    /// <summary>PIN تحویل ۴ رقمی با RandomNumberGenerator (سند بخش ۱۸.۴).</summary>
    string GenerateDeliveryPin();

    /// <summary>ثبت مرسوله در تاپین با توکن از اعتبارنامهٔ تننت.</summary>
    Task<ShipmentRegistrationResult> RegisterShipmentAsync(
        string orderId, decimal weightKg, string destinationCity, string recipientAddress, string recipientPhone,
        bool isCod, CancellationToken cancellationToken = default);
}
