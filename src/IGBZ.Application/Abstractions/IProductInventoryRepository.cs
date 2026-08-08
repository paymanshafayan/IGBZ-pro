namespace IGBZ.Application.Abstractions;

using IGBZ.Domain.Catalog;

/// <summary>
/// عملیات اتمیک موجودی — رزرو با <c>findOneAndUpdate</c> روی شرط
/// <c>stock - reserved &gt;= qty</c> انجام می‌شود (سند بخش ۷.۳). بدون بافت تننت خطا می‌دهد.
/// </summary>
public interface IProductInventoryRepository : ITenantScopedRepository<Product>
{
    /// <summary>رزرو اتمیک — در صورت موفقیت true، در صورت کمبود موجودی false.</summary>
    Task<bool> TryReserveAsync(string productId, string sku, int quantity, CancellationToken cancellationToken = default);

    /// <summary>آزادسازی رزرو (مثلاً انقضای سبد خرید).</summary>
    Task ReleaseReservationAsync(string productId, string sku, int quantity, CancellationToken cancellationToken = default);
}
