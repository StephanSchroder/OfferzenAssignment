using Assignment.Api.Controllers;
using Assignment.Api.Dtos;
using Assignment.Api.Models;
using Assignment.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Api.Tests.Controllers;

public sealed class PurchaseOrderControllerTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private PurchaseOrderController CreateController(Data.AppDbContext context) =>
        new PurchaseOrderController(context).WithTestContext();

    private static PurchaseOrderRequest Request(
        int customerId,
        string clientReference,
        PurchaseOrderStatus? status = null,
        params PurchaseOrderItemRequest[] items) => new()
        {
            CustomerId = customerId,
            ClientReference = clientReference,
            Status = status,
            Items = items.ToList(),
        };

    private async Task<PurchaseOrderDetailsDto> CreateOrderAsync(PurchaseOrderRequest request)
    {
        using var context = _database.CreateContext();
        var result = await CreateController(context).Create(request, CancellationToken.None);
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        return Assert.IsType<PurchaseOrderDetailsDto>(created.Value);
    }

    [Fact]
    public async Task Create_captures_the_current_product_price_and_calculates_the_total()
    {
        var customer = _database.AddCustomer();
        var desk = _database.AddProduct("Desk", 449.00m);
        var chair = _database.AddProduct("Chair", 289.50m);

        var order = await CreateOrderAsync(Request(
            customer.Id,
            "PO-001",
            items:
            [
                new PurchaseOrderItemRequest { ProductId = desk.Id, Quantity = 2 },
                new PurchaseOrderItemRequest { ProductId = chair.Id, Quantity = 3 },
            ]));

        Assert.Equal(PurchaseOrderStatus.Pending, order.Status);
        Assert.Equal("Acme", order.CustomerName);
        Assert.Equal(449.00m, order.Items.Single(i => i.ProductId == desk.Id).UnitPrice);
        Assert.Equal(868.50m, order.Items.Single(i => i.ProductId == chair.Id).LineTotal);
        Assert.Equal(1766.50m, order.Total);

        using var context = _database.CreateContext();
        var stored = await context.PurchaseOrders.SingleAsync();
        Assert.Equal(1766.50m, stored.Total);
        Assert.Equal(TimeSpan.Zero, stored.Timestamp.Offset);
    }

    [Fact]
    public async Task Create_rejects_a_duplicate_client_reference()
    {
        var customer = _database.AddCustomer();
        var product = _database.AddProduct();
        await CreateOrderAsync(Request(customer.Id, "PO-001", items: new PurchaseOrderItemRequest { ProductId = product.Id, Quantity = 1 }));

        using var context = _database.CreateContext();
        var result = await CreateController(context).Create(
            Request(customer.Id, "PO-001", items: new PurchaseOrderItemRequest { ProductId = product.Id, Quantity = 1 }),
            CancellationToken.None);

        var conflict = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task Database_unique_index_rejects_a_duplicate_client_reference()
    {
        var customer = _database.AddCustomer();
        using var context = _database.CreateContext();
        context.PurchaseOrders.Add(new PurchaseOrder { CustomerId = customer.Id, ClientReference = "PO-001" });
        context.PurchaseOrders.Add(new PurchaseOrder { CustomerId = customer.Id, ClientReference = "PO-001" });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Create_rejects_a_deleted_customer()
    {
        var customer = _database.AddCustomer();
        var product = _database.AddProduct();
        using (var context = _database.CreateContext())
        {
            (await context.Customers.SingleAsync()).SoftDelete();
            await context.SaveChangesAsync();
        }

        using var requestContext = _database.CreateContext();
        var result = await CreateController(requestContext).Create(
            Request(customer.Id, "PO-001", items: new PurchaseOrderItemRequest { ProductId = product.Id, Quantity = 1 }),
            CancellationToken.None);

        var problem = AssertValidationProblem(result.Result);
        Assert.True(problem.Errors.ContainsKey(nameof(PurchaseOrderRequest.CustomerId)));
    }

    [Fact]
    public async Task Create_rejects_starting_as_fulfilled()
    {
        var customer = _database.AddCustomer();
        var product = _database.AddProduct();

        using var context = _database.CreateContext();
        var result = await CreateController(context).Create(
            Request(customer.Id, "PO-001", PurchaseOrderStatus.Fulfilled, new PurchaseOrderItemRequest { ProductId = product.Id, Quantity = 1 }),
            CancellationToken.None);

        var problem = AssertValidationProblem(result.Result);
        Assert.Contains("can't start as Fulfilled", problem.Errors[nameof(PurchaseOrderRequest.Status)].Single());
    }

    [Fact]
    public async Task Update_keeps_the_price_existing_lines_were_ordered_at()
    {
        var customer = _database.AddCustomer();
        var product = _database.AddProduct("Desk", 100m);
        var order = await CreateOrderAsync(Request(customer.Id, "PO-001", items: new PurchaseOrderItemRequest { ProductId = product.Id, Quantity = 1 }));

        // The product gets more expensive after the order was placed.
        using (var context = _database.CreateContext())
        {
            (await context.Products.SingleAsync()).Price = 150m;
            await context.SaveChangesAsync();
        }

        var existingLine = order.Items.Single();
        using var updateContext = _database.CreateContext();
        var result = await CreateController(updateContext).Update(
            order.Id,
            Request(
                customer.Id,
                "PO-001",
                items:
                [
                    new PurchaseOrderItemRequest { Id = existingLine.Id, ProductId = product.Id, Quantity = 2 },
                    new PurchaseOrderItemRequest { ProductId = product.Id, Quantity = 1 },
                ]),
            CancellationToken.None);

        var updated = Assert.IsType<PurchaseOrderDetailsDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(100m, updated.Items.Single(i => i.Id == existingLine.Id).UnitPrice); // price at the time
        Assert.Equal(150m, updated.Items.Single(i => i.Id != existingLine.Id).UnitPrice); // new line, current price
        Assert.Equal(350m, updated.Total);
    }

    [Fact]
    public async Task Update_rejects_a_status_change_that_does_not_make_sense()
    {
        var customer = _database.AddCustomer();
        var product = _database.AddProduct();
        var order = await CreateOrderAsync(Request(customer.Id, "PO-001", items: new PurchaseOrderItemRequest { ProductId = product.Id, Quantity = 1 }));

        using var context = _database.CreateContext();
        var result = await CreateController(context).Update(
            order.Id,
            Request(customer.Id, "PO-001", PurchaseOrderStatus.Fulfilled, new PurchaseOrderItemRequest { Id = order.Items.Single().Id, ProductId = product.Id, Quantity = 1 }),
            CancellationToken.None);

        var problem = AssertValidationProblem(result.Result);
        Assert.Contains("Confirm it first", problem.Errors[nameof(PurchaseOrderRequest.Status)].Single());

        using var checkContext = _database.CreateContext();
        Assert.Equal(PurchaseOrderStatus.Pending, (await checkContext.PurchaseOrders.SingleAsync()).Status);
    }

    [Fact]
    public async Task Update_applies_an_allowed_status_change()
    {
        var customer = _database.AddCustomer();
        var product = _database.AddProduct();
        var order = await CreateOrderAsync(Request(customer.Id, "PO-001", items: new PurchaseOrderItemRequest { ProductId = product.Id, Quantity = 1 }));

        using var context = _database.CreateContext();
        var result = await CreateController(context).Update(
            order.Id,
            Request(customer.Id, "PO-001", PurchaseOrderStatus.Confirmed, new PurchaseOrderItemRequest { Id = order.Items.Single().Id, ProductId = product.Id, Quantity = 1 }),
            CancellationToken.None);

        var updated = Assert.IsType<PurchaseOrderDetailsDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(PurchaseOrderStatus.Confirmed, updated.Status);
    }

    [Fact]
    public async Task Delete_archives_the_order_instead_of_removing_it()
    {
        var customer = _database.AddCustomer();
        var product = _database.AddProduct();
        var order = await CreateOrderAsync(Request(customer.Id, "PO-001", items: new PurchaseOrderItemRequest { ProductId = product.Id, Quantity = 1 }));

        using (var context = _database.CreateContext())
        {
            var result = await CreateController(context).Archive(order.Id, CancellationToken.None);
            Assert.IsType<NoContentResult>(result);
        }

        using var checkContext = _database.CreateContext();
        var stored = await checkContext.PurchaseOrders.SingleAsync();
        Assert.True(stored.IsArchived);
        Assert.NotNull(stored.ArchivedAt);
    }

    [Fact]
    public async Task Archived_orders_cannot_be_updated()
    {
        var customer = _database.AddCustomer();
        var product = _database.AddProduct();
        var order = await CreateOrderAsync(Request(customer.Id, "PO-001", items: new PurchaseOrderItemRequest { ProductId = product.Id, Quantity = 1 }));
        using (var context = _database.CreateContext())
        {
            await CreateController(context).Archive(order.Id, CancellationToken.None);
        }

        using var updateContext = _database.CreateContext();
        var result = await CreateController(updateContext).Update(
            order.Id,
            Request(customer.Id, "PO-001", items: new PurchaseOrderItemRequest { ProductId = product.Id, Quantity = 5 }),
            CancellationToken.None);

        var conflict = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task Timestamps_can_be_filtered_and_sorted_in_the_database()
    {
        var customer = _database.AddCustomer();
        using (var context = _database.CreateContext())
        {
            context.PurchaseOrders.AddRange(
                new PurchaseOrder { CustomerId = customer.Id, ClientReference = "OLD", Timestamp = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero) },
                new PurchaseOrder { CustomerId = customer.Id, ClientReference = "NEW", Timestamp = new DateTimeOffset(2026, 10, 30, 10, 0, 0, TimeSpan.Zero) },
                new PurchaseOrder { CustomerId = customer.Id, ClientReference = "MID", Timestamp = new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.FromHours(2)) });
            await context.SaveChangesAsync();
        }

        using var queryContext = _database.CreateContext();
        var since = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var references = await queryContext.PurchaseOrders
            .Where(po => po.Timestamp >= since)
            .OrderBy(po => po.Timestamp)
            .Select(po => po.ClientReference)
            .ToListAsync();

        Assert.Equal(new[] { "MID", "NEW" }, references);
    }

    private static ValidationProblemDetails AssertValidationProblem(ActionResult? result)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
        return Assert.IsType<ValidationProblemDetails>(objectResult.Value);
    }
}
