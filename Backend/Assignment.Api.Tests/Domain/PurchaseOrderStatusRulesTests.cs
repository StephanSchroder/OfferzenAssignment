using Assignment.Api.Models;

namespace Assignment.Api.Tests.Domain;

public class PurchaseOrderStatusRulesTests
{
    [Theory]
    [InlineData(PurchaseOrderStatus.Pending, PurchaseOrderStatus.Confirmed)]
    [InlineData(PurchaseOrderStatus.Pending, PurchaseOrderStatus.Cancelled)]
    [InlineData(PurchaseOrderStatus.Confirmed, PurchaseOrderStatus.Fulfilled)]
    [InlineData(PurchaseOrderStatus.Confirmed, PurchaseOrderStatus.Cancelled)]
    public void Allows_the_normal_order_flow(PurchaseOrderStatus from, PurchaseOrderStatus to)
    {
        Assert.True(PurchaseOrderStatusRules.CanChange(from, to));
        Assert.Null(PurchaseOrderStatusRules.ValidateChange(from, to));
    }

    [Theory]
    [InlineData(PurchaseOrderStatus.Pending)]
    [InlineData(PurchaseOrderStatus.Confirmed)]
    [InlineData(PurchaseOrderStatus.Fulfilled)]
    [InlineData(PurchaseOrderStatus.Cancelled)]
    public void Keeping_the_same_status_is_always_allowed(PurchaseOrderStatus status)
    {
        Assert.Null(PurchaseOrderStatusRules.ValidateChange(status, status));
    }

    [Theory]
    [InlineData(PurchaseOrderStatus.Pending, PurchaseOrderStatus.Fulfilled, "Confirm it first")]
    [InlineData(PurchaseOrderStatus.Confirmed, PurchaseOrderStatus.Pending, "can't go back to Pending")]
    [InlineData(PurchaseOrderStatus.Fulfilled, PurchaseOrderStatus.Cancelled, "already been delivered")]
    [InlineData(PurchaseOrderStatus.Fulfilled, PurchaseOrderStatus.Pending, "already been fulfilled")]
    [InlineData(PurchaseOrderStatus.Fulfilled, PurchaseOrderStatus.Confirmed, "already been fulfilled")]
    [InlineData(PurchaseOrderStatus.Cancelled, PurchaseOrderStatus.Pending, "can't be reopened")]
    [InlineData(PurchaseOrderStatus.Cancelled, PurchaseOrderStatus.Confirmed, "can't be reopened")]
    [InlineData(PurchaseOrderStatus.Cancelled, PurchaseOrderStatus.Fulfilled, "can't be reopened")]
    public void Rejects_changes_that_do_not_make_sense_with_a_helpful_message(
        PurchaseOrderStatus from,
        PurchaseOrderStatus to,
        string expectedMessagePart)
    {
        Assert.False(PurchaseOrderStatusRules.CanChange(from, to));

        var message = PurchaseOrderStatusRules.ValidateChange(from, to);

        Assert.NotNull(message);
        Assert.Contains(expectedMessagePart, message);
    }

    [Theory]
    [InlineData(PurchaseOrderStatus.Fulfilled)]
    [InlineData(PurchaseOrderStatus.Cancelled)]
    public void Fulfilled_and_cancelled_are_final(PurchaseOrderStatus status)
    {
        Assert.Empty(PurchaseOrderStatusRules.AllowedNextStatuses(status));
    }

    [Theory]
    [InlineData(PurchaseOrderStatus.Pending)]
    [InlineData(PurchaseOrderStatus.Confirmed)]
    public void New_orders_may_start_as_pending_or_confirmed(PurchaseOrderStatus status)
    {
        Assert.Null(PurchaseOrderStatusRules.ValidateInitial(status));
    }

    [Theory]
    [InlineData(PurchaseOrderStatus.Fulfilled)]
    [InlineData(PurchaseOrderStatus.Cancelled)]
    public void New_orders_cannot_start_fulfilled_or_cancelled(PurchaseOrderStatus status)
    {
        var message = PurchaseOrderStatusRules.ValidateInitial(status);

        Assert.NotNull(message);
        Assert.Contains("can't start as", message);
    }
}
