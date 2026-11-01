using Assignment.Api.Models;

namespace Assignment.Api.Tests.Domain;

public class ProductTests
{
    [Theory]
    [InlineData(19.99, 19.99)]
    [InlineData(-19.99, 19.99)]
    [InlineData(0, 0)]
    public void Price_is_always_positive(double input, double expected)
    {
        var product = new Product { Price = (decimal)input };

        Assert.Equal((decimal)expected, product.Price);
    }

    [Fact]
    public void SoftDelete_keeps_the_product_but_marks_it_deleted()
    {
        var product = new Product { Name = "Desk", Price = 100m };

        product.SoftDelete();

        Assert.True(product.IsDeleted);
        Assert.NotNull(product.DeletedAt);
        Assert.Equal("Desk", product.Name);
    }
}
