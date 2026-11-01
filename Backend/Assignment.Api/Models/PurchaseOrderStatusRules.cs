namespace Assignment.Api.Models;

/// <summary>
/// Which purchase order status changes make sense, with a helpful message for the ones that don't.
/// <code>
///   Pending ──► Confirmed ──► Fulfilled
///      │            │
///      └────────────┴──► Cancelled
/// </code>
/// Fulfilled and Cancelled are final. The frontend mirrors these rules in
/// src/app/core/models/purchase-order-status-rules.ts so it can explain them before saving.
/// </summary>
public static class PurchaseOrderStatusRules
{
    private static readonly Dictionary<PurchaseOrderStatus, PurchaseOrderStatus[]> NextStatuses = new()
    {
        [PurchaseOrderStatus.Pending] = [PurchaseOrderStatus.Confirmed, PurchaseOrderStatus.Cancelled],
        [PurchaseOrderStatus.Confirmed] = [PurchaseOrderStatus.Fulfilled, PurchaseOrderStatus.Cancelled],
        [PurchaseOrderStatus.Fulfilled] = [],
        [PurchaseOrderStatus.Cancelled] = [],
    };

    /// <summary>Statuses a new purchase order may start in.</summary>
    public static IReadOnlyList<PurchaseOrderStatus> InitialStatuses { get; } =
        [PurchaseOrderStatus.Pending, PurchaseOrderStatus.Confirmed];

    public static IReadOnlyList<PurchaseOrderStatus> AllowedNextStatuses(PurchaseOrderStatus current) => NextStatuses[current];

    public static bool CanChange(PurchaseOrderStatus from, PurchaseOrderStatus to) =>
        from == to || NextStatuses[from].Contains(to);

    /// <summary>Returns null when the change is allowed, otherwise a message for the user.</summary>
    public static string? ValidateChange(PurchaseOrderStatus from, PurchaseOrderStatus to)
    {
        if (CanChange(from, to))
        {
            return null;
        }

        return (from, to) switch
        {
            (PurchaseOrderStatus.Pending, PurchaseOrderStatus.Fulfilled) =>
                "A pending order can't be fulfilled yet. Confirm it first, then mark it as Fulfilled.",
            (PurchaseOrderStatus.Confirmed, PurchaseOrderStatus.Pending) =>
                "A confirmed order can't go back to Pending. Cancel it instead if it shouldn't go ahead.",
            (PurchaseOrderStatus.Fulfilled, PurchaseOrderStatus.Cancelled) =>
                "A fulfilled order can't be cancelled because it has already been delivered.",
            (PurchaseOrderStatus.Fulfilled, _) =>
                $"This order has already been fulfilled, so it can't move back to {to}.",
            (PurchaseOrderStatus.Cancelled, _) =>
                "A cancelled order can't be reopened. Create a new purchase order instead.",
            _ => $"An order can't change from {from} to {to}.",
        };
    }

    /// <summary>Returns null when a new order may start in this status, otherwise a message for the user.</summary>
    public static string? ValidateInitial(PurchaseOrderStatus status) =>
        InitialStatuses.Contains(status)
            ? null
            : $"A new purchase order can't start as {status}. Create it as Pending or Confirmed, then update its status.";
}
