using Assignment.Api.Controllers;
using Assignment.Api.Dtos;
using Assignment.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Api.Tests.Controllers;

public sealed class CustomerAndProductControllerTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Deleting_a_customer_is_a_soft_delete()
    {
        var customer = _database.AddCustomer();

        using (var context = _database.CreateContext())
        {
            var result = await new CustomerController(context).Delete(customer.Id, CancellationToken.None);
            Assert.IsType<NoContentResult>(result);
        }

        using var checkContext = _database.CreateContext();
        var stored = await checkContext.Customers.SingleAsync(); // still in the database
        Assert.True(stored.IsDeleted);

        var list = await new CustomerController(checkContext).GetAll(CancellationToken.None);
        var customers = Assert.IsAssignableFrom<IEnumerable<CustomerDto>>(Assert.IsType<OkObjectResult>(list.Result).Value);
        Assert.Empty(customers); // but hidden from the list
    }

    [Fact]
    public async Task Deleted_customers_cannot_be_updated()
    {
        var customer = _database.AddCustomer();
        using (var context = _database.CreateContext())
        {
            await new CustomerController(context).Delete(customer.Id, CancellationToken.None);
        }

        using var updateContext = _database.CreateContext();
        var result = await new CustomerController(updateContext).Update(
            customer.Id,
            new CustomerRequest { Name = "New name", Email = "new@example.com" },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Creating_a_product_with_a_negative_price_stores_it_as_positive()
    {
        using var context = _database.CreateContext();

        var result = await new ProductController(context).Create(
            new ProductRequest { Name = "Desk", Price = -249.99m },
            CancellationToken.None);

        var created = Assert.IsType<ProductDto>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
        Assert.Equal(249.99m, created.Price);
    }

    [Fact]
    public async Task Prices_can_be_sorted_in_the_database()
    {
        _database.AddProduct("Cheap", 9.99m);
        _database.AddProduct("Expensive", 1200m);
        _database.AddProduct("Middle", 100.50m);

        using var context = _database.CreateContext();
        var names = await context.Products
            .OrderByDescending(p => p.Price)
            .Select(p => p.Name)
            .ToListAsync();

        Assert.Equal(new[] { "Expensive", "Middle", "Cheap" }, names);
    }

    [Fact]
    public async Task Prices_can_be_filtered_in_the_database()
    {
        _database.AddProduct("Cheap", 9.99m);
        _database.AddProduct("Expensive", 1200m);

        using var context = _database.CreateContext();
        var names = await context.Products.Where(p => p.Price > 100m).Select(p => p.Name).ToListAsync();

        Assert.Equal(new[] { "Expensive" }, names);
    }
}
