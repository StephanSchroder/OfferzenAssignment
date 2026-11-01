using Assignment.Api.Models;

namespace Assignment.Api.Tests.Domain;

public class PurchaseOrderTests
{
    [Fact]
    public void Total_is_the_sum_of_quantity_times_price_at_the_time()
    {
        var order = new PurchaseOrder();
        order.Items.Add(new PurchaseOrderItem { Quantity = 3, UnitPrice = 19.99m });
        order.Items.Add(new PurchaseOrderItem { Quantity = 2, UnitPrice = 5.50m });

        var total = order.RecalculateTotal();

        Assert.Equal(70.97m, total);
        Assert.Equal(70.97m, order.Total);
    }

    [Fact]
    public void Total_is_zero_without_items()
    {
        var order = new PurchaseOrder();

        Assert.Equal(0m, order.RecalculateTotal());
    }

    [Fact]
    public void Timestamp_is_always_stored_in_utc()
    {
        var amsterdamTime = new DateTimeOffset(2026, 10, 31, 17, 30, 0, TimeSpan.FromHours(1));

        var order = new PurchaseOrder { Timestamp = amsterdamTime };

        Assert.Equal(TimeSpan.Zero, order.Timestamp.Offset);
        Assert.Equal(new DateTimeOffset(2026, 10, 31, 16, 30, 0, TimeSpan.Zero), order.Timestamp);
    }

    [Fact]
    public void New_orders_are_pending()
    {
        Assert.Equal(PurchaseOrderStatus.Pending, new PurchaseOrder().Status);
    }

    [Fact]
    public void ChangeStatus_applies_an_allowed_change()
    {
        var order = new PurchaseOrder { Status = PurchaseOrderStatus.Pending };

        order.ChangeStatus(PurchaseOrderStatus.Confirmed);

        Assert.Equal(PurchaseOrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void ChangeStatus_rejects_a_change_that_does_not_make_sense()
    {
        var order = new PurchaseOrder { Status = PurchaseOrderStatus.Cancelled };

        var exception = Assert.Throws<InvalidOperationException>(() => order.ChangeStatus(PurchaseOrderStatus.Confirmed));

        Assert.Contains("can't be reopened", exception.Message);
        Assert.Equal(PurchaseOrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Archive_marks_the_order_as_archived_instead_of_deleting_it()
    {
        var order = new PurchaseOrder();

        order.Archive();

        Assert.True(order.IsArchived);
        Assert.NotNull(order.ArchivedAt);
    }

    [Fact]
    public void Archiving_twice_keeps_the_original_archive_date()
    {
        var order = new PurchaseOrder();
        order.Archive();
        var firstArchivedAt = order.ArchivedAt;

        order.Archive();

        Assert.Equal(firstArchivedAt, order.ArchivedAt);
    }
}
