namespace IGBZ.Domain.Orders;

public enum OrderStatus
{
    Pending = 0,
    Paid = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5,
    Returned = 6,
    Refunded = 7
}
