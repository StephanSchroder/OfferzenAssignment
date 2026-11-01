using System.ComponentModel.DataAnnotations;
using Assignment.Api.Models;

namespace Assignment.Api.Dtos;

/// <summary>
/// Row shape exposed through the OData endpoint (/odata/PurchaseOrders).
/// </summary>
public class PurchaseOrderSummaryDto
{
    public int Id { get; set; }

    public string ClientReference { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    public PurchaseOrderStatus Status { get; set; }

    public int ItemCount { get; set; }

    public decimal Total { get; set; }

    public bool IsArchived { get; set; }
}

public class PurchaseOrderDetailsDto
{
    public int Id { get; set; }

    public string ClientReference { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    public PurchaseOrderStatus Status { get; set; }

    public bool IsArchived { get; set; }

    public DateTimeOffset? ArchivedAt { get; set; }

    public List<PurchaseOrderItemDto> Items { get; set; } = [];

    /// <summary>
    /// Sum of all line totals, calculated by the backend.
    /// </summary>
    public decimal Total { get; set; }
}

public class PurchaseOrderItemDto
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public uint Quantity { get; set; }

    /// <summary>
    /// Price of one unit at the time the line was added to the order.
    /// </summary>
    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }
}

public class PurchaseOrderRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A customer is required.")]
    public int CustomerId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string ClientReference { get; set; } = string.Empty;

    /// <summary>
    /// Optional on create (defaults to Pending).
    /// </summary>
    public PurchaseOrderStatus? Status { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "A purchase order needs at least one item.")]
    public List<PurchaseOrderItemRequest> Items { get; set; } = [];
}

public class PurchaseOrderItemRequest
{
    /// <summary>
    /// Id of an existing line when updating. Existing lines keep the price they were ordered at.
    /// Leave empty for new lines; they get the product's current price.
    /// </summary>
    public int? Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A product is required.")]
    public int ProductId { get; set; }

    /// <summary>
    /// Unsigned, so negative quantities are rejected when the request is read.
    /// </summary>
    [Range(typeof(uint), "1", "4294967295", ErrorMessage = "Quantity must be at least 1.")]
    public uint Quantity { get; set; }
}
