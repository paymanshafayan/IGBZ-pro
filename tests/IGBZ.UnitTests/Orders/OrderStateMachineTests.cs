using IGBZ.Domain.Orders;
using IGBZ.Domain.Shared;
using Xunit;

namespace IGBZ.UnitTests.Orders;

public sealed class OrderStateMachineTests
{
    [Fact]
    public void MarkAsPaid_WhenOrderIsPending_ChangesStatusToPaid()
    {
        var order = CreatePendingOrder();

        order.MarkAsPaid("tx-1", DateTimeOffset.UtcNow);

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal("tx-1", order.PaymentTransactionId);
    }

    [Fact]
    public void Cancel_WhenOrderIsDelivered_ThrowsDomainException()
    {
        var order = CreatePendingOrder();
        var now = DateTimeOffset.UtcNow;
        order.MarkAsPaid("tx-1", now);
        order.StartProcessing(now);
        order.MarkAsShipped(now);
        order.MarkAsDelivered(now);

        Assert.Throws<DomainException>(() => order.Cancel("too late", now));
    }

    [Fact]
    public void MarkAsPaid_WhenOrderIsCancelled_ThrowsDomainException()
    {
        var order = CreatePendingOrder();
        var now = DateTimeOffset.UtcNow;
        order.Cancel("customer request", now);

        Assert.Throws<DomainException>(() => order.MarkAsPaid("tx-1", now));
    }

    private static Order CreatePendingOrder()
    {
        var now = DateTimeOffset.UtcNow;
        return Order.Create(
            "tenant-a",
            "ORD-1",
            new OrderCustomerSnapshot(null, "Ali", "09120000000", null),
            new Address("Ali", "09120000000", "Tehran", "Tehran", "Street 1"),
            [new OrderItem("p1", "v1", "Product", "Default", "SKU-1", 1, 100_000, "IRR")],
            ["reservation-1"],
            100_000,
            0,
            9_000,
            20_000,
            129_000,
            "IRR",
            null,
            now);
    }
}
