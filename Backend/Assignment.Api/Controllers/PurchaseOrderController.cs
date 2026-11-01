using Assignment.Api.Data;
using Assignment.Api.Dtos;
using Assignment.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Api.Controllers;

/// <summary>
/// Add / update / archive purchase orders, plus a details endpoint with the backend-calculated
/// total. Deleting a purchase order archives it. For lists with filtering / sorting use the
/// OData endpoint at /odata/PurchaseOrders.
/// </summary>
[ApiController]
[Route("api/purchaseorders")]
[Produces("application/json")]
public class PurchaseOrderController(AppDbContext db) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseOrderDetailsDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(id, tracking: false, cancellationToken);
        return order is null ? NotFound() : Ok(order.ToDetailsDto());
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PurchaseOrderDetailsDto>> Create(PurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var clientReference = request.ClientReference.Trim();

        if (request.Status is { } initialStatus && PurchaseOrderStatusRules.ValidateInitial(initialStatus) is { } statusProblem)
        {
            ModelState.AddModelError(nameof(request.Status), statusProblem);
        }

        var problem = await ValidateAsync(request, clientReference, currentOrderId: null, cancellationToken);
        if (problem is not null)
        {
            return problem;
        }

        var products = await LoadProductsAsync(request, cancellationToken);

        var order = new PurchaseOrder
        {
            CustomerId = request.CustomerId,
            ClientReference = clientReference,
            Status = request.Status ?? PurchaseOrderStatus.Pending,
            Timestamp = DateTimeOffset.UtcNow,
        };

        foreach (var line in request.Items)
        {
            var product = products[line.ProductId];
            order.Items.Add(new PurchaseOrderItem
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                UnitPrice = product.Price, // price at the time of ordering
            });
        }

        order.RecalculateTotal();
        db.PurchaseOrders.Add(order);

        var saveProblem = await TrySaveAsync(clientReference, cancellationToken);
        if (saveProblem is not null)
        {
            return saveProblem;
        }

        var created = await LoadOrderAsync(order.Id, tracking: false, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, created!.ToDetailsDto());
    }

    /// <summary>
    /// Replaces the order's header fields and items. Status changes must follow
    /// <see cref="PurchaseOrderStatusRules"/>; a change that doesn't make sense returns 400 with a
    /// message under <c>errors.Status</c>. Items sent with an existing <c>id</c> keep
    /// the price they were ordered at (unless the product changes); new items get the current
    /// product price. Existing items that are not sent are removed.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PurchaseOrderDetailsDto>> Update(int id, PurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(id, tracking: true, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        if (order.IsArchived)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Purchase order is archived",
                detail: "Archived purchase orders can't be changed.");
        }

        var clientReference = request.ClientReference.Trim();

        if (request.Status is { } requestedStatus
            && PurchaseOrderStatusRules.ValidateChange(order.Status, requestedStatus) is { } statusProblem)
        {
            ModelState.AddModelError(nameof(request.Status), statusProblem);
        }

        var problem = await ValidateAsync(request, clientReference, currentOrderId: id, cancellationToken);
        if (problem is not null)
        {
            return problem;
        }

        var products = await LoadProductsAsync(request, cancellationToken);

        order.CustomerId = request.CustomerId;
        order.ClientReference = clientReference;
        if (request.Status is { } status)
        {
            order.ChangeStatus(status); // already validated above
        }

        var existingItems = order.Items.ToDictionary(i => i.Id);
        var keptItemIds = new HashSet<int>();

        foreach (var line in request.Items)
        {
            if (line.Id is { } itemId && existingItems.TryGetValue(itemId, out var existing) && keptItemIds.Add(itemId))
            {
                if (existing.ProductId != line.ProductId)
                {
                    existing.ProductId = line.ProductId;
                    existing.UnitPrice = products[line.ProductId].Price;
                }

                existing.Quantity = line.Quantity;
                continue;
            }

            order.Items.Add(new PurchaseOrderItem
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = products[line.ProductId].Price,
            });
        }

        foreach (var removed in existingItems.Values.Where(i => !keptItemIds.Contains(i.Id)).ToList())
        {
            order.Items.Remove(removed);
            db.PurchaseOrderItems.Remove(removed);
        }

        order.RecalculateTotal();

        var saveProblem = await TrySaveAsync(clientReference, cancellationToken);
        if (saveProblem is not null)
        {
            return saveProblem;
        }

        var updated = await LoadOrderAsync(id, tracking: false, cancellationToken);
        return Ok(updated!.ToDetailsDto());
    }

    /// <summary>
    /// Archives the purchase order instead of deleting it.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken)
    {
        var order = await db.PurchaseOrders.FirstOrDefaultAsync(po => po.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        order.Archive();
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private Task<PurchaseOrder?> LoadOrderAsync(int id, bool tracking, CancellationToken cancellationToken)
    {
        IQueryable<PurchaseOrder> query = db.PurchaseOrders
            .Include(po => po.Customer)
            .Include(po => po.Items)
                .ThenInclude(i => i.Product);

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(po => po.Id == id, cancellationToken);
    }

    private async Task<Dictionary<int, Product>> LoadProductsAsync(PurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        return await db.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);
    }

    private async Task<ActionResult?> ValidateAsync(
        PurchaseOrderRequest request,
        string clientReference,
        int? currentOrderId,
        CancellationToken cancellationToken)
    {
        var customerExists = await db.Customers.AnyAsync(c => c.Id == request.CustomerId && !c.IsDeleted, cancellationToken);
        if (!customerExists)
        {
            ModelState.AddModelError(nameof(request.CustomerId), $"Customer {request.CustomerId} does not exist.");
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var activeProductIds = await db.Products
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        // Lines that already exist on the order may keep pointing at a product that was deleted later.
        var existingProductIds = currentOrderId is null
            ? []
            : await db.PurchaseOrderItems
                .Where(i => i.PurchaseOrderId == currentOrderId)
                .Select(i => i.ProductId)
                .Distinct()
                .ToListAsync(cancellationToken);

        var missing = productIds.Except(activeProductIds).Except(existingProductIds).ToList();
        if (missing.Count > 0)
        {
            ModelState.AddModelError(nameof(request.Items), $"Unknown or deleted product(s): {string.Join(", ", missing)}.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // Checked up front for a friendly message; the unique index on ClientReference is the real guarantee.
        var referenceTaken = await db.PurchaseOrders.AnyAsync(
            po => po.ClientReference == clientReference && po.Id != (currentOrderId ?? 0),
            cancellationToken);

        if (referenceTaken)
        {
            return ClientReferenceConflict(clientReference);
        }

        return null;
    }

    private async Task<ActionResult?> TrySaveAsync(string clientReference, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteErrorCode: 19 } sqlite
            && sqlite.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
        {
            // Another request saved the same client reference between the check above and this save;
            // the unique index rejected it (SQLite error 19 = constraint violation).
            return ClientReferenceConflict(clientReference);
        }
    }

    private ObjectResult ClientReferenceConflict(string clientReference) => Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Client reference already in use",
        detail: $"Another purchase order already uses client reference '{clientReference}'.");
}
