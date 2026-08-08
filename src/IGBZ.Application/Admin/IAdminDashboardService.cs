namespace IGBZ.Application.Admin;

using IGBZ.Domain.Orders;

public class OrderListItemDto
{
    public string Id { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public decimal GrandTotalToman { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

public class DashboardSummaryDto
{
    public int ProductCount { get; init; }
    public int OrderCount { get; init; }
    public int PaidOrderCount { get; init; }
    public decimal RevenueToman { get; init; }
    public int CustomerCount { get; init; }
}

/// <summary>داده‌های ادمین تننت — سفارش‌ها و خلاصهٔ داشبورد (همیشه از منبع واقعی).</summary>
public interface IAdminDashboardService
{
    Task<IReadOnlyList<OrderListItemDto>> GetOrdersAsync(CancellationToken cancellationToken = default);

    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
