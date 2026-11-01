using Assignment.Api.Models;

namespace Assignment.Api.Tests.Domain;

public class PurchaseOrderItemTests
{
    [Fact]
    public void Line_total_is_quantity_times_unit_price()
    {
        var item = new PurchaseOrderItem { Quantity = 4, UnitPrice = 12.25m };

        Assert.Equal(49.00m, item.LineTotal);
    }

    [Fact]
    public void Unit_price_is_always_positive()
    {
        var item = new PurchaseOrderItem { UnitPrice = -8.40m };

        Assert.Equal(8.40m, item.UnitPrice);
    }
}
