using System.Linq.Expressions;
using Assignment.Api.Models;

namespace Assignment.Api.Dtos;

/// <summary>
/// Projections from entities to DTOs. Written as expressions so EF Core translates them to SQL
/// (which also lets OData apply $filter / $orderby on the projected shape).
/// </summary>
public static class Mappings
{
    public static readonly Expression<Func<Customer, CustomerDto>> CustomerToDto = c => new CustomerDto
    {
        Id = c.Id,
        Name = c.Name,
        Email = c.Email,
    };

    public static readonly Expression<Func<Product, ProductDto>> ProductToDto = p => new ProductDto
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
    };

    public static readonly Expression<Func<PurchaseOrder, PurchaseOrderSummaryDto>> PurchaseOrderToSummary = po => new PurchaseOrderSummaryDto
    {
        Id = po.Id,
        ClientReference = po.ClientReference,
        CustomerId = po.CustomerId,
        CustomerName = po.Customer.Name,
        Timestamp = po.Timestamp,
        Status = po.Status,
        ItemCount = po.Items.Count,
        Total = po.Total,
        IsArchived = po.IsArchived,
    };

    private static readonly Func<Customer, CustomerDto> CustomerToDtoCompiled = CustomerToDto.Compile();
    private static readonly Func<Product, ProductDto> ProductToDtoCompiled = ProductToDto.Compile();

    public static CustomerDto ToDto(this Customer customer) => CustomerToDtoCompiled(customer);

    public static ProductDto ToDto(this Product product) => ProductToDtoCompiled(product);

    /// <summary>
    /// Requires Customer and Items.Product to be loaded.
    /// </summary>
    public static PurchaseOrderDetailsDto ToDetailsDto(this PurchaseOrder order) => new()
    {
        Id = order.Id,
        ClientReference = order.ClientReference,
        CustomerId = order.CustomerId,
        CustomerName = order.Customer.Name,
        CustomerEmail = order.Customer.Email,
        Timestamp = order.Timestamp,
        Status = order.Status,
        IsArchived = order.IsArchived,
        ArchivedAt = order.ArchivedAt,
        Items = order.Items
            .OrderBy(i => i.Id)
            .Select(i => new PurchaseOrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal,
            })
            .ToList(),
        // Calculated from the lines on every read, so the details view always shows the real sum.
        Total = order.Items.Sum(i => i.LineTotal),
    };
}
