namespace Assignment.Api.Models;

/// <summary>
/// A purchase order placed by a customer (one customer has many purchase orders).
/// Purchase orders are never physically removed: deleting one archives it.
/// </summary>
public class PurchaseOrder
{
    private DateTimeOffset _timestamp = DateTimeOffset.UtcNow;

    public int Id { get; set; }

    public int CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    /// <summary>
    /// The customer's own reference for this order. Unique across all purchase orders.
    /// </summary>
    public string ClientReference { get; set; } = string.Empty;

    /// <summary>
    /// Moment the order was placed. Always normalised to UTC.
    /// </summary>
    public DateTimeOffset Timestamp
    {
        get => _timestamp;
        set => _timestamp = value.ToUniversalTime();
    }

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Pending;

    /// <summary>
    /// Order total, calculated by the backend from the items (quantity x price at the time).
    /// Stored so it can be filtered and sorted through OData.
    /// </summary>
    public decimal Total { get; private set; }

    public bool IsArchived { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();

    /// <summary>
    /// Changes the status, enforcing <see cref="PurchaseOrderStatusRules"/>.
    /// </summary>
    public void ChangeStatus(PurchaseOrderStatus newStatus)
    {
        var problem = PurchaseOrderStatusRules.ValidateChange(Status, newStatus);
        if (problem is not null)
        {
            throw new InvalidOperationException(problem);
        }

        Status = newStatus;
    }

    public decimal RecalculateTotal()
    {
        Total = Items.Sum(item => item.LineTotal);
        return Total;
    }

    public void Archive()
    {
        if (IsArchived)
        {
            return;
        }

        IsArchived = true;
        ArchivedAt = DateTimeOffset.UtcNow;
    }
}
