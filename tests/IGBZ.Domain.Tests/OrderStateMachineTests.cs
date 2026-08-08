namespace IGBZ.Domain.Tests;

using IGBZ.Domain.Common;
using IGBZ.Domain.Orders;
using Xunit;

public class OrderStateMachineTests
{
    private static Order CreatePendingOrder()
    {
        var order = new Order { TenantId = "t1", CustomerId = "c1" };
        order.AddItem("p1", "SKU-1", 1, new Money(100_000));
        order.ApplyPricing(new Money(100_000), Money.Zero, new Money(9_000), Money.Zero, Array.Empty<string>());
        return order;
    }

    [Fact]
    public void HappyPath_TransitionsThroughAllStates()
    {
        var order = CreatePendingOrder();

        order.MarkAsPaid("zarinpal", "trk-1", "ref-1");
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal("trk-1", order.Payment.TrackingNumber);

        order.MarkAsProcessing();
        Assert.Equal(OrderStatus.Processing, order.Status);

        order.Ship("تیپاکس", "tp-123", "1234");
        Assert.Equal(OrderStatus.Shipped, order.Status);

        order.Deliver();
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void CannotShip_BeforePaid()
    {
        var order = CreatePendingOrder();
        Assert.Throws<InvalidOrderStateException>(() => order.Ship("تیپاکس", null, null));
    }

    [Fact]
    public void CannotDeliver_BeforeShipped()
    {
        var order = CreatePendingOrder();
        order.MarkAsPaid("zarinpal", "trk-1", null);
        order.MarkAsProcessing();
        Assert.Throws<InvalidOrderStateException>(() => order.Deliver());
    }

    [Fact]
    public void CanCancel_WhenPendingOrPaid()
    {
        var pending = CreatePendingOrder();
        pending.Cancel("مشتری منصرف شد");
        Assert.Equal(OrderStatus.Cancelled, pending.Status);

        var paid = CreatePendingOrder();
        paid.MarkAsPaid("zarinpal", "trk-2", null);
        paid.Cancel("توسط ادمین");
        Assert.Equal(OrderStatus.Cancelled, paid.Status);
    }

    [Fact]
    public void CannotCancel_WhenShipped()
    {
        var order = CreatePendingOrder();
        order.MarkAsPaid("zarinpal", "trk-1", null);
        order.MarkAsProcessing();
        order.Ship("تیپاکس", null, null);

        Assert.Throws<InvalidOrderStateException>(() => order.Cancel("بعد از ارسال"));
    }

    [Fact]
    public void AddItem_OnlyWhenPending()
    {
        var order = CreatePendingOrder();
        order.MarkAsPaid("zarinpal", "trk-1", null);

        Assert.Throws<InvalidOrderStateException>(() => order.AddItem("p2", "SKU-2", 1, new Money(50_000)));
    }

    [Fact]
    public void ApplyPricing_ComputesGrandTotal()
    {
        var order = new Order { TenantId = "t1", CustomerId = "c1" };
        order.ApplyPricing(
            new Money(100_000),
            new Money(10_000),
            new Money(8_100),
            new Money(25_000),
            new[] { "SAVE10" });

        Assert.Equal(123_100m, order.GrandTotalToman.Toman);
        Assert.Contains("SAVE10", order.AppliedCouponCodes);
    }

    [Fact]
    public void ApplyPricing_Throws_WhenDiscountExceedsSubtotal()
    {
        var order = new Order { TenantId = "t1", CustomerId = "c1" };
        Assert.Throws<InvalidOperationException>(() =>
            order.ApplyPricing(new Money(100_000), new Money(150_000), Money.Zero, Money.Zero, Array.Empty<string>()));
    }

    [Fact]
    public void StatusHistory_TracksTransitions()
    {
        var order = CreatePendingOrder();
        order.MarkAsPaid("zarinpal", "trk-1", null);
        order.MarkAsProcessing();

        Assert.Equal(3, order.StatusHistory.Count); // Pending(ایجاد) + Paid + Processing
        Assert.Equal(OrderStatus.Processing, order.StatusHistory[^1].Status);
    }
}
