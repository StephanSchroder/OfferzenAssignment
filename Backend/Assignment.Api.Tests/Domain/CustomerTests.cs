using Assignment.Api.Models;

namespace Assignment.Api.Tests.Domain;

public class CustomerTests
{
    [Fact]
    public void SoftDelete_marks_the_customer_deleted()
    {
        var customer = new Customer { Name = "Acme", Email = "acme@example.com" };

        customer.SoftDelete();

        Assert.True(customer.IsDeleted);
        Assert.NotNull(customer.DeletedAt);
    }

    [Fact]
    public void Deleting_twice_keeps_the_original_delete_date()
    {
        var customer = new Customer();
        customer.SoftDelete();
        var firstDeletedAt = customer.DeletedAt;

        customer.SoftDelete();

        Assert.Equal(firstDeletedAt, customer.DeletedAt);
    }
}
