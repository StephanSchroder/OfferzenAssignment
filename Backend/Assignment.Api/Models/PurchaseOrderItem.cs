using System.ComponentModel.DataAnnotations.Schema;

namespace Assignment.Api.Models;

/// <summary>
/// A line on a purchase order. The unit price is copied from the product when the line is
/// created, so later product price changes don't change existing orders.
/// </summary>
public class PurchaseOrderItem
{
    private decimal _unitPrice;

    public int Id { get; set; }

    public int PurchaseOrderId { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    /// <summary>
    /// Ordered quantity. Unsigned, so it can never be negative.
    /// </summary>
    public uint Quantity { get; set; }

    /// <summary>
    /// Price of one unit at the moment the line was added to the order.
    /// </summary>
    public decimal UnitPrice
    {
        get => _unitPrice;
        set => _unitPrice = Math.Abs(value);
    }

    [NotMapped]
    public decimal LineTotal => UnitPrice * Quantity;
}
