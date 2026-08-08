namespace IGBZ.Application.Admin;

using IGBZ.Application.Abstractions;
using IGBZ.Domain.Catalog;
using IGBZ.Domain.Customers;
using IGBZ.Domain.Orders;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly ITenantScopedRepository<Order> _orderRepository;
    private readonly ITenantScopedRepository<Product> _productRepository;
    private readonly ITenantScopedRepository<Customer> _customerRepository;

    public AdminDashboardService(
        ITenantScopedRepository<Order> orderRepository,
        ITenantScopedRepository<Product> productRepository,
        ITenantScopedRepository<Customer> customerRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
    }

    public async Task<IReadOnlyList<OrderListItemDto>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.FindAsync(_ => true, cancellationToken);

        return orders
            .OrderByDescending(o => o.CreatedOnUtc)
            .Select(o => new OrderListItemDto
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                Status = o.Status,
                GrandTotalToman = o.GrandTotalToman.Toman,
                CreatedOnUtc = o.CreatedOnUtc
            })
            .ToList();
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.FindAsync(_ => true, cancellationToken);
        var products = await _productRepository.FindAsync(p => !p.Deleted, cancellationToken);
        var customers = await _customerRepository.FindAsync(_ => true, cancellationToken);

        var paidOrders = orders.Where(o => o.Status == OrderStatus.Paid
            || o.Status == OrderStatus.Processing
            || o.Status == OrderStatus.Shipped
            || o.Status == OrderStatus.Delivered).ToList();

        return new DashboardSummaryDto
        {
            ProductCount = products.Count,
            OrderCount = orders.Count,
            PaidOrderCount = paidOrders.Count,
            RevenueToman = paidOrders.Sum(o => o.GrandTotalToman.Toman),
            CustomerCount = customers.Count
        };
    }
}
