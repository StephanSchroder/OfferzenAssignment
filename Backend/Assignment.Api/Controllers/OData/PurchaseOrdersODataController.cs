using Assignment.Api.Data;
using Assignment.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Assignment.Api.OData;

namespace Assignment.Api.Controllers.OData;

/// <summary>
/// GET /odata/PurchaseOrders — supports $filter, $orderby, $top, $skip, $count and $select.
/// Archived orders are included; filter them out with $filter=isArchived eq false.
/// Example: /odata/PurchaseOrders?$filter=isArchived eq false and status eq 'Pending'&amp;$orderby=timestamp desc
/// </summary>
public class PurchaseOrdersODataController(AppDbContext db) : ODataController
{
    [HttpGet("odata/PurchaseOrders")]
    [EnableQuery(PageSize = ODataQueryDefaults.PageSize, MaxTop = ODataQueryDefaults.MaxTop)]
    public IQueryable<PurchaseOrderSummaryDto> Get() =>
        db.PurchaseOrders.Select(Mappings.PurchaseOrderToSummary);
}
